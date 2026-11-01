using Company.Api.Contracts;
using Company.Api.Services;

namespace Company.Api.Endpoints;

/// <summary>The one way an endpoint answers: the result, or a refusal in the form the desktop app shows as it is</summary>
public static class EndpointResults
{
    public static async Task<IResult> Run<T>(Func<Task<T>> action)
    {
        try
        {
            return Results.Ok(await action());
        }
        catch (SheetException ex)
        {
            return Refusal(ex);
        }
    }

    public static async Task<IResult> File(Func<Task<PrintedFile>> action)
    {
        try
        {
            var file = await action();
            return Results.File(file.Bytes, file.ContentType, file.FileName);
        }
        catch (SheetException ex)
        {
            return Refusal(ex);
        }
    }

    private static IResult Refusal(SheetException ex) => Results.Json(new ApiErrorDto(ex.Code, ex.Message), statusCode: ex.Status);
}
