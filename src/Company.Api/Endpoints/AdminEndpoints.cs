using Company.Api.Auth;
using Company.Api.Contracts;
using Company.Api.Data;
using Company.Api.Services;
using Microsoft.EntityFrameworkCore;
using Timesheet.Domain;

namespace Company.Api.Endpoints;

/// <summary>Master data and settings for the administrator. Every change is audited in the same save.</summary>
public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/admin").RequireAuthorization(Identities.AdminPolicy);

        // ---- Employees ---------------------------------------------------------------------------------------------

        admin.MapGet("/employees", async (CompanyDbContext db) =>
            Results.Ok((await db.Employees.AsNoTracking().Include(e => e.Accounts).OrderBy(e => e.Code).ToListAsync()).Select(ToDto)));

        admin.MapGet("/employees/{code}", async (string code, CompanyDbContext db) =>
        {
            var employee = await db.Employees.AsNoTracking().Include(e => e.Accounts).FirstOrDefaultAsync(e => e.Code == code);
            return employee == null ? NotFound("employee") : Results.Ok(ToDto(employee));
        });

        admin.MapPut("/employees/{code}", async (string code, UpdateEmployeeRequest request, CompanyDbContext db, ChartService chart, AuditService audit) =>
        {
            var employee = await db.Employees.Include(e => e.Accounts).FirstOrDefaultAsync(e => e.Code == code);
            if (employee == null) return NotFound("employee");

            var before = Describe(employee);
            if (request.Name != null)
            {
                var name = request.Name.Trim();
                if (name.Length == 0) return Results.BadRequest(new ApiErrorDto("name-required", "The name cannot be empty."));
                employee.Name = name;
            }

            if (request.IsActive.HasValue) employee.IsActive = request.IsActive.Value;
            if (request.IsFinalApprover.HasValue) employee.IsFinalApprover = request.IsFinalApprover.Value;
            if (request.IsAdmin.HasValue) employee.IsAdmin = request.IsAdmin.Value;
            if (request.ActiveFrom.HasValue) employee.ActiveFrom = request.ActiveFrom;
            if (request.ActiveTo.HasValue) employee.ActiveTo = request.ActiveTo;
            if (request.SupervisorCode != null) employee.SupervisorCode = Clear(request.SupervisorCode);
            if (request.DesignatedApproverCode != null) employee.DesignatedApproverCode = Clear(request.DesignatedApproverCode);

            if (!await db.Employees.AnyAsync(e => e.Code == employee.SupervisorCode) && employee.SupervisorCode != null
                || employee.DesignatedApproverCode != null && !await db.Employees.AnyAsync(e => e.Code == employee.DesignatedApproverCode))
                return Results.BadRequest(new ApiErrorDto("unknown-person", "The supervisor or the designated approver is not an employee."));

            var problem = await chart.CheckWithAsync(ChartService.ToPerson(employee));
            if (problem != ChainProblem.None)
                return Results.Conflict(new ApiErrorDto(problem == ChainProblem.Cycle ? "chart-cycle" : "chart-invalid",
                    problem == ChainProblem.Cycle ? "This would make someone their own supervisor." : "The chart would be invalid."));

            audit.Record("EmployeeUpdated", "Employee", employee.Code, $"{before} -> {Describe(employee)}");
            await db.SaveChangesAsync();
            return Results.Ok(ToDto(employee));
        });

        admin.MapPost("/employees/import/preview", async (CsvRequest request, CompanyDbContext db) =>
        {
            var existing = await db.Employees.AsNoTracking().ToDictionaryAsync(e => e.Code, e => e.Name);
            var preview = EmployeeListImporter.Preview(request.Csv ?? "", existing);
            return Results.Ok(ToDto(preview));
        });

        admin.MapPost("/employees/import", async (CsvRequest request, CompanyDbContext db, AuditService audit) =>
        {
            var existing = await db.Employees.ToDictionaryAsync(e => e.Code);
            var preview = EmployeeListImporter.Preview(request.Csv ?? "", existing.ToDictionary(p => p.Key, p => p.Value.Name));

            int created = 0, updated = 0;
            foreach (var row in preview.Valid)
            {
                if (existing.TryGetValue(row.Code, out var employee))
                {
                    if (employee.Name == row.Name) continue;
                    audit.Record("EmployeeRenamed", "Employee", row.Code, $"{employee.Name} -> {row.Name}");
                    employee.Name = row.Name;
                    updated++;
                }
                else
                {
                    var added = new Employee { Code = row.Code, Name = row.Name };
                    db.Employees.Add(added);
                    existing[row.Code] = added;
                    audit.Record("EmployeeCreated", "Employee", row.Code, row.Name);
                    created++;
                }
            }

            audit.Record("EmployeesImported", "Employee", "*", $"created {created}, updated {updated}, skipped {preview.ErrorCount}");
            await db.SaveChangesAsync();
            return Results.Ok(new ImportResultDto(created, updated, preview.ErrorCount));
        });

        // ---- Accounts ----------------------------------------------------------------------------------------------

        admin.MapPost("/employees/{code}/accounts", async (string code, AccountRequest request, CompanyDbContext db, AuditService audit) =>
        {
            var account = AccountNormalizer.Normalize(request.Account);
            if (account.Length == 0) return Results.BadRequest(new ApiErrorDto("account-required", "The account cannot be empty."));
            if (!await db.Employees.AnyAsync(e => e.Code == code)) return NotFound("employee");

            var owner = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Account == account);
            if (owner != null)
                return owner.EmployeeCode == code
                    ? Results.Ok()
                    : Results.Conflict(new ApiErrorDto("account-taken", $"The account is already linked to {owner.EmployeeCode}."));

            db.Accounts.Add(new EmployeeAccount { EmployeeCode = code, Account = account });
            audit.Record("AccountLinked", "Employee", code, account);
            await db.SaveChangesAsync();
            return Results.Ok();
        });

        admin.MapDelete("/employees/{code}/accounts", async (string code, string account, CompanyDbContext db, AuditService audit) =>
        {
            var normalized = AccountNormalizer.Normalize(account);
            var link = await db.Accounts.FirstOrDefaultAsync(a => a.EmployeeCode == code && a.Account == normalized);
            if (link == null) return NotFound("account");

            db.Accounts.Remove(link);
            audit.Record("AccountUnlinked", "Employee", code, normalized);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // Proposes links between directory users and employees by name; nothing is saved
        admin.MapPost("/accounts/match", async (List<DirectoryUserDto> users, CompanyDbContext db) =>
        {
            var employees = (await db.Employees.AsNoTracking().Where(e => e.IsActive).ToListAsync()).Select(e => (e.Code, e.Name)).ToList();
            var linked = (await db.Accounts.AsNoTracking().Select(a => a.Account).ToListAsync()).ToHashSet();
            return Results.Ok(AccountMatcher.Match(users ?? new List<DirectoryUserDto>(), employees, linked));
        });

        // ---- Holidays ----------------------------------------------------------------------------------------------

        admin.MapGet("/holidays/{year:int}", async (int year, CompanyDbContext db) =>
            Results.Ok((await db.StateHolidays.AsNoTracking().Where(h => h.Date.Year == year).OrderBy(h => h.Date).ToListAsync())
                .Select(h => new HolidayDto(h.Date, h.Name))));

        admin.MapPost("/holidays", async (HolidayDto holiday, CompanyDbContext db, AuditService audit) =>
        {
            var name = holiday.Name?.Trim() ?? "";
            if (name.Length == 0) return Results.BadRequest(new ApiErrorDto("name-required", "The holiday needs a name."));

            var existing = await db.StateHolidays.FirstOrDefaultAsync(h => h.Date == holiday.Date);
            if (existing != null) existing.Name = name;
            else db.StateHolidays.Add(new StateHoliday { Date = holiday.Date, Name = name });

            audit.Record(existing == null ? "HolidayAdded" : "HolidayRenamed", "StateHoliday", holiday.Date.ToString("yyyy-MM-dd"), name);
            await db.SaveChangesAsync();
            return Results.Ok(new HolidayDto(holiday.Date, name));
        });

        admin.MapDelete("/holidays/{date}", async (DateOnly date, CompanyDbContext db, AuditService audit) =>
        {
            var holiday = await db.StateHolidays.FirstOrDefaultAsync(h => h.Date == date);
            if (holiday == null) return NotFound("holiday");

            db.StateHolidays.Remove(holiday);
            audit.Record("HolidayRemoved", "StateHoliday", date.ToString("yyyy-MM-dd"), holiday.Name);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        admin.MapGet("/employees/{code}/religious-holidays", async (string code, CompanyDbContext db) =>
            Results.Ok((await db.ReligiousHolidays.AsNoTracking().Where(h => h.EmployeeCode == code).OrderBy(h => h.Date).ToListAsync())
                .Select(h => new HolidayDto(h.Date, h.Name))));

        // Replaces the religious holidays of one employee with the given list
        admin.MapPut("/employees/{code}/religious-holidays", async (string code, List<HolidayDto> holidays, CompanyDbContext db, AuditService audit) =>
        {
            if (!await db.Employees.AnyAsync(e => e.Code == code)) return NotFound("employee");

            var list = (holidays ?? new List<HolidayDto>()).GroupBy(h => h.Date).Select(g => g.First()).ToList();
            db.ReligiousHolidays.RemoveRange(db.ReligiousHolidays.Where(h => h.EmployeeCode == code));
            db.ReligiousHolidays.AddRange(list.Select(h => new ReligiousHoliday { EmployeeCode = code, Date = h.Date, Name = h.Name?.Trim() ?? "" }));
            audit.Record("ReligiousHolidaysSet", "Employee", code, string.Join(", ", list.Select(h => h.Date.ToString("yyyy-MM-dd"))));
            await db.SaveChangesAsync();
            return Results.Ok(list);
        });

        // ---- Audit -------------------------------------------------------------------------------------------------

        admin.MapGet("/audit", async (CompanyDbContext db, int? take) =>
        {
            var events = await db.AuditEvents.AsNoTracking().OrderByDescending(e => e.Id).Take(Math.Clamp(take ?? 100, 1, 1000)).ToListAsync();
            return Results.Ok(events.Select(e => new AuditEventDto(e.Id, e.AtUtc, e.Actor, e.Action, e.EntityType, e.EntityId, e.Details)));
        });
    }

    private static IResult NotFound(string what) => Results.NotFound(new ApiErrorDto("not-found", $"The {what} was not found."));

    /// <summary>An empty string clears a code</summary>
    private static string? Clear(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Describe(Employee e) =>
        $"name={e.Name}, active={e.IsActive}, supervisor={e.SupervisorCode}, final={e.IsFinalApprover}, designated={e.DesignatedApproverCode}, admin={e.IsAdmin}";

    private static EmployeeDto ToDto(Employee e) => new(
        e.Code, e.Name, e.IsActive, e.SupervisorCode, e.IsFinalApprover, e.DesignatedApproverCode, e.IsAdmin, e.ActiveFrom, e.ActiveTo,
        e.Accounts.Select(a => a.Account).OrderBy(a => a).ToList());

    private static ImportPreviewDto ToDto(EmployeeImportPreview preview) => new(
        preview.Rows.Select(r => new ImportRowDto(
            r.Line, r.Code, r.Name,
            r.Issues.Where(i => i.Severity == ImportSeverity.Error).Select(i => i.Message).ToList(),
            r.Issues.Where(i => i.Severity == ImportSeverity.Warning).Select(i => i.Message).ToList())).ToList(),
        preview.ErrorCount, preview.WarningCount);
}
