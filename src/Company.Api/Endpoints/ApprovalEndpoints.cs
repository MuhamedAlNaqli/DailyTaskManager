using System.Security.Claims;
using Company.Api.Auth;
using Company.Api.Contracts;
using Company.Api.Services;

namespace Company.Api.Endpoints;

/// <summary>Submitting the caller's own month, and the approvers' work on other people's months</summary>
public static class ApprovalEndpoints
{
    public static void MapApprovalEndpoints(this IEndpointRouteBuilder app)
    {
        var own = app.MapGroup("/api/timesheet/{year:int}/{month:int}").RequireAuthorization(Identities.RegisteredPolicy);

        own.MapPost("/submit", (int year, int month, ClaimsPrincipal user, WorkflowService service) =>
            Run(() => service.SubmitAsync(Identities.EmployeeCode(user)!, year, month)));

        own.MapPost("/messages/{index:int}/answer", (int year, int month, int index, AnswerRequest request, ClaimsPrincipal user, WorkflowService service) =>
            Run(() => service.AnswerAsync(Identities.EmployeeCode(user)!, year, month, index, request.Answer)));

        own.MapGet("/revisions/{revision:int}", (int year, int month, int revision, ClaimsPrincipal user, WorkflowService service) =>
            Run(() => service.RevisionAsync(Identities.EmployeeCode(user)!, Identities.EmployeeCode(user)!, year, month, revision)));

        own.MapGet("/print/{format}", (int year, int month, string format, ClaimsPrincipal user, PrintService service) =>
            File(() => service.OwnAsync(Identities.EmployeeCode(user)!, year, month, format)));

        app.MapGet("/api/export/{year:int}/{month:int}/{format}", (int year, int month, string format, PrintService service) =>
            File(() => service.ApprovedAsync(year, month, format))).RequireAuthorization(Identities.FinancePolicy);

        app.MapGet("/api/reminders", (ClaimsPrincipal user, ReminderService service) => service.ForAsync(Identities.EmployeeCode(user)!))
            .RequireAuthorization(Identities.RegisteredPolicy);

        var approvals = app.MapGroup("/api/approvals").RequireAuthorization(Identities.RegisteredPolicy);

        approvals.MapGet("", (ClaimsPrincipal user, WorkflowService service) =>
            Run(() => service.PendingAsync(Identities.EmployeeCode(user)!)));

        approvals.MapGet("/{code}/{year:int}/{month:int}", (string code, int year, int month, ClaimsPrincipal user, WorkflowService service) =>
            Run(() => service.ReviewAsync(Identities.EmployeeCode(user)!, code, year, month)));

        approvals.MapGet("/{code}/{year:int}/{month:int}/revisions/{revision:int}", (string code, int year, int month, int revision, ClaimsPrincipal user, WorkflowService service) =>
            Run(() => service.RevisionAsync(Identities.EmployeeCode(user)!, code, year, month, revision)));

        approvals.MapGet("/{code}/{year:int}/{month:int}/print/{format}", (string code, int year, int month, string format, ClaimsPrincipal user, PrintService service) =>
            File(() => service.ReviewAsync(Identities.EmployeeCode(user)!, code, year, month, format)));

        approvals.MapPost("/{code}/{year:int}/{month:int}/approve", (string code, int year, int month, ClaimsPrincipal user, WorkflowService service) =>
            Run(() => service.ApproveAsync(Identities.EmployeeCode(user)!, code, year, month)));

        approvals.MapPost("/{code}/{year:int}/{month:int}/return", (string code, int year, int month, ReturnRequest request, ClaimsPrincipal user, WorkflowService service) =>
            Run(() => service.ReturnAsync(Identities.EmployeeCode(user)!, code, year, month, request.Message)));
    }

    private static async Task<IResult> File(Func<Task<PrintedFile>> action)
    {
        try
        {
            var file = await action();
            return Results.File(file.Bytes, file.ContentType, file.FileName);
        }
        catch (SheetException ex)
        {
            return Results.Json(new ApiErrorDto(ex.Code, ex.Message), statusCode: ex.Status);
        }
    }

    private static async Task<IResult> Run<T>(Func<Task<T>> action)
    {
        try
        {
            return Results.Ok(await action());
        }
        catch (SheetException ex)
        {
            return Results.Json(new ApiErrorDto(ex.Code, ex.Message), statusCode: ex.Status);
        }
    }
}
