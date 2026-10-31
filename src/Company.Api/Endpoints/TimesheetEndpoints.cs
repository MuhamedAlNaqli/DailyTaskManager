using System.Security.Claims;
using Company.Api.Auth;
using Company.Api.Contracts;
using Company.Api.Services;

namespace Company.Api.Endpoints;

/// <summary>The caller's own timesheet. Other people's timesheets (approvers, legal) come with the approval workflow.</summary>
public static class TimesheetEndpoints
{
    public static void MapTimesheetEndpoints(this IEndpointRouteBuilder app)
    {
        var sheet = app.MapGroup("/api/timesheet/{year:int}/{month:int}").RequireAuthorization(Identities.RegisteredPolicy);

        sheet.MapGet("", (int year, int month, ClaimsPrincipal user, TimesheetService service) =>
            Run(() => service.GetAsync(Identities.EmployeeCode(user)!, year, month)));

        sheet.MapPut("/days/{date}", (int year, int month, DateOnly date, SetEntryRequest request, ClaimsPrincipal user, TimesheetService service) =>
            Run(() => service.SetEntryAsync(Identities.EmployeeCode(user)!, year, month, date, request)));

        sheet.MapDelete("/days/{date}/{code:int}", (int year, int month, DateOnly date, int code, ClaimsPrincipal user, TimesheetService service) =>
            Run(() => service.ClearEntryAsync(Identities.EmployeeCode(user)!, year, month, date, code)));

        sheet.MapPost("/fill-standard-days", (int year, int month, ClaimsPrincipal user, TimesheetService service) =>
            Run(() => service.FillStandardDaysAsync(Identities.EmployeeCode(user)!, year, month)));

        sheet.MapPost("/absences/preview", (int year, int month, AbsenceRangeRequest request, ClaimsPrincipal user, TimesheetService service) =>
            Run(() => service.PreviewAbsenceAsync(Identities.EmployeeCode(user)!, year, month, request)));

        sheet.MapPost("/absences", (int year, int month, AbsenceRangeRequest request, ClaimsPrincipal user, TimesheetService service) =>
            Run(() => service.ApplyAbsenceAsync(Identities.EmployeeCode(user)!, year, month, request)));

        sheet.MapPost("/overtime", (int year, int month, OvertimeRequest request, ClaimsPrincipal user, TimesheetService service) =>
            Run(() => service.AddOvertimeAsync(Identities.EmployeeCode(user)!, year, month, request)));

        sheet.MapPut("/overtime/{id:int}", (int year, int month, int id, OvertimeRequest request, ClaimsPrincipal user, TimesheetService service) =>
            Run(() => service.UpdateOvertimeAsync(Identities.EmployeeCode(user)!, year, month, id, request)));

        sheet.MapDelete("/overtime/{id:int}", (int year, int month, int id, ClaimsPrincipal user, TimesheetService service) =>
            Run(() => service.DeleteOvertimeAsync(Identities.EmployeeCode(user)!, year, month, id)));
    }

    /// <summary>Turns a refusal of the service into the error body the desktop app shows</summary>
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
