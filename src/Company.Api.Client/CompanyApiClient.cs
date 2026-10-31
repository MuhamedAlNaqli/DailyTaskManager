using System.Net;
using System.Net.Http.Json;
using Company.Api.Contracts;

namespace Company.Api.Client;

/// <summary>An answer of the API that the desktop app shows as it is. <see cref="Code"/> is the stable code (for example "locked").</summary>
public sealed class ApiException : Exception
{
    public ApiException(HttpStatusCode status, string code, string message) : base(message)
    {
        Status = status;
        Code = code;
    }

    public HttpStatusCode Status { get; }

    public string Code { get; }
}

/// <summary>
/// The company API for the desktop app. It carries no state: sign-in is whatever the <see cref="HttpClient"/> does (for
/// Windows sign-in, the default credentials of the user).
/// </summary>
public sealed class CompanyApiClient
{
    private readonly HttpClient _http;

    public CompanyApiClient(HttpClient http) => _http = http;

    /// <summary>
    /// An HTTP client that signs in with the Windows account of the user (Kerberos on a domain, NTLM otherwise).
    /// Plain HTTP is only accepted for the local machine; anywhere else the address must be HTTPS.
    /// </summary>
    public static HttpClient CreateWindowsHttpClient(Uri baseAddress)
    {
        EnsureSecure(baseAddress);
        var handler = new HttpClientHandler { UseDefaultCredentials = true, AllowAutoRedirect = false };
        return new HttpClient(handler) { BaseAddress = baseAddress, Timeout = TimeSpan.FromSeconds(30) };
    }

    public static void EnsureSecure(Uri address)
    {
        if (address.Scheme == Uri.UriSchemeHttps) return;
        if (address.Scheme == Uri.UriSchemeHttp && address.IsLoopback) return;
        throw new InvalidOperationException("The company server address must start with https:// (plain http is only allowed for this computer).");
    }

    // ---- Identity and reference data ------------------------------------------------------------------------------------

    public Task<MeDto> GetMeAsync(CancellationToken ct = default) => GetAsync<MeDto>("/api/me", ct);

    public Task<AuthDiagnosticsDto> GetDiagnosticsAsync(CancellationToken ct = default) => GetAsync<AuthDiagnosticsDto>("/api/auth/diagnostics", ct);

    public Task<List<TimeCodeDto>> GetTimeCodesAsync(CancellationToken ct = default) => GetAsync<List<TimeCodeDto>>("/api/timecodes", ct);

    // ---- My timesheet -----------------------------------------------------------------------------------------------------

    private static string Sheet(int year, int month) => $"/api/timesheet/{year}/{month}";

    public Task<SheetDto> GetSheetAsync(int year, int month, CancellationToken ct = default) => GetAsync<SheetDto>(Sheet(year, month), ct);

    public Task<SheetDto> SetEntryAsync(int year, int month, DateOnly date, SetEntryRequest request, CancellationToken ct = default) =>
        SendAsync<SheetDto>(HttpMethod.Put, $"{Sheet(year, month)}/days/{date:yyyy-MM-dd}", request, ct);

    public Task<SheetDto> ClearEntryAsync(int year, int month, DateOnly date, int code, CancellationToken ct = default) =>
        SendAsync<SheetDto>(HttpMethod.Delete, $"{Sheet(year, month)}/days/{date:yyyy-MM-dd}/{code}", null, ct);

    public Task<SheetDto> FillStandardDaysAsync(int year, int month, CancellationToken ct = default) =>
        SendAsync<SheetDto>(HttpMethod.Post, $"{Sheet(year, month)}/fill-standard-days", null, ct);

    public Task<AbsencePreviewDto> PreviewAbsenceAsync(int year, int month, AbsenceRangeRequest request, CancellationToken ct = default) =>
        SendAsync<AbsencePreviewDto>(HttpMethod.Post, $"{Sheet(year, month)}/absences/preview", request, ct);

    public Task<SheetDto> ApplyAbsenceAsync(int year, int month, AbsenceRangeRequest request, CancellationToken ct = default) =>
        SendAsync<SheetDto>(HttpMethod.Post, $"{Sheet(year, month)}/absences", request, ct);

    public Task<SheetDto> AddOvertimeAsync(int year, int month, OvertimeRequest request, CancellationToken ct = default) =>
        SendAsync<SheetDto>(HttpMethod.Post, $"{Sheet(year, month)}/overtime", request, ct);

    public Task<SheetDto> UpdateOvertimeAsync(int year, int month, int id, OvertimeRequest request, CancellationToken ct = default) =>
        SendAsync<SheetDto>(HttpMethod.Put, $"{Sheet(year, month)}/overtime/{id}", request, ct);

    public Task<SheetDto> DeleteOvertimeAsync(int year, int month, int id, CancellationToken ct = default) =>
        SendAsync<SheetDto>(HttpMethod.Delete, $"{Sheet(year, month)}/overtime/{id}", null, ct);

    // ---- Plumbing --------------------------------------------------------------------------------------------------------

    private Task<T> GetAsync<T>(string url, CancellationToken ct) => SendAsync<T>(HttpMethod.Get, url, null, ct);

    private async Task<T> SendAsync<T>(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body != null) request.Content = JsonContent.Create(body, body.GetType());

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(HttpStatusCode.ServiceUnavailable, "unreachable", "The company server cannot be reached. Check the network connection. " + ex.Message);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new ApiException(HttpStatusCode.RequestTimeout, "timeout", "The company server did not answer in time.");
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
                return result ?? throw new ApiException(response.StatusCode, "empty", "The server sent an empty answer.");
            }

            throw await ToExceptionAsync(response, ct);
        }
    }

    private static async Task<ApiException> ToExceptionAsync(HttpResponseMessage response, CancellationToken ct)
    {
        ApiErrorDto? error = null;
        try
        {
            error = await response.Content.ReadFromJsonAsync<ApiErrorDto>(cancellationToken: ct);
        }
        catch (Exception)
        {
            // Not our error body (for example a proxy page): fall back to the status
        }

        if (error != null && !string.IsNullOrEmpty(error.Code)) return new ApiException(response.StatusCode, error.Code, error.Message);

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new ApiException(response.StatusCode, "unauthorized", "The server did not accept your Windows sign-in."),
            HttpStatusCode.Forbidden => new ApiException(response.StatusCode, "forbidden", "You are not allowed to do this."),
            HttpStatusCode.NotFound => new ApiException(response.StatusCode, "not-found", "The server does not know this request."),
            _ => new ApiException(response.StatusCode, "server-error", $"The server answered {(int)response.StatusCode}.")
        };
    }
}
