using System.Security.Claims;
using Company.Api.Auth;
using Company.Api.Services;

using static Company.Api.Endpoints.EndpointResults;

namespace Company.Api.Endpoints;

/// <summary>Sheets of more than one person on one printed sheet: the supervisor's team, and (for finance and the administrator) everything that was approved</summary>
public static class ExportEndpoints
{
    public static void MapExportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/team/{year:int}/{month:int}/print/{format}", (int year, int month, string format, ClaimsPrincipal user, PrintService service) =>
            File(() => service.TeamAsync(Identities.EmployeeCode(user)!, year, month, format))).RequireAuthorization(Identities.RegisteredPolicy);

        app.MapGet("/api/export/{year:int}/{month:int}/{format}", (int year, int month, string format, PrintService service) =>
            File(() => service.ApprovedAsync(year, month, format))).RequireAuthorization(Identities.FinancePolicy);
    }
}
