using System.Globalization;
using Company.Api.Contracts;
using Company.Api.Data;
using Company.Api.Endpoints;
using Microsoft.EntityFrameworkCore;
using Timesheet.Domain;

namespace Company.Api.Services;

/// <summary>
/// What is due for one person today. The rules are the domain library's <see cref="ReminderRules"/>; this service only
/// gathers the facts (the employee's months, the sheets waiting for them) so the banner in the app and the scheduled task
/// show the same thing. It says what is due, not how to word it.
/// </summary>
public sealed class ReminderService
{
    private readonly CompanyDbContext _db;
    private readonly WorkflowService _workflow;
    private readonly TimeProvider _time;
    private readonly DateOnly? _startFrom;

    public ReminderService(CompanyDbContext db, WorkflowService workflow, TimeProvider time, IConfiguration configuration)
    {
        _db = db;
        _workflow = workflow;
        _time = time;

        // Months before the go-live month are ignored, so nobody is told off for a month that was never meant to be entered
        var start = configuration["Reminders:StartFrom"];
        if (DateOnly.TryParseExact(start + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) _startFrom = parsed;
    }

    public async Task<RemindersDto> ForAsync(string employeeCode)
    {
        var today = DateOnly.FromDateTime(_time.GetLocalNow().DateTime);
        var items = new List<ReminderDto>();

        var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Code == employeeCode);
        if (employee == null || !employee.IsActive) return new RemindersDto(today, items);

        // Returned sheets wait for the employee whatever the month
        var returned = await _db.Sheets.AsNoTracking().Where(s => s.EmployeeCode == employeeCode && s.Status == TimesheetStatus.Returned)
            .OrderBy(s => s.Year).ThenBy(s => s.Month).Select(s => new { s.Year, s.Month }).ToListAsync();
        foreach (var sheet in returned)
        {
            var deadline = await SubmissionDeadlineAsync(sheet.Year, sheet.Month);
            items.Add(new ReminderDto(nameof(ReminderKind.FixReturned), sheet.Year, sheet.Month, 1, deadline));
        }

        // This month and the one before it (a month that was not submitted stays overdue until it is)
        var thisMonth = new DateOnly(today.Year, today.Month, 1);
        foreach (var first in new[] { thisMonth.AddMonths(-1), thisMonth })
        {
            var last = new DateOnly(first.Year, first.Month, DateTime.DaysInMonth(first.Year, first.Month));
            if (_startFrom is { } start && last < start) continue;
            if (employee.ActiveFrom is { } from && from > last) continue;
            if (employee.ActiveTo is { } to && to < first) continue;

            var status = await _db.Sheets.AsNoTracking().Where(s => s.EmployeeCode == employeeCode && s.Year == first.Year && s.Month == first.Month)
                .Select(s => (TimesheetStatus?)s.Status).FirstOrDefaultAsync() ?? TimesheetStatus.Draft;
            if (status != TimesheetStatus.Draft) continue;

            var deadline = await SubmissionDeadlineAsync(first.Year, first.Month);
            var kind = ReminderRules.ForEmployee(today, status, deadline);
            if (kind != null) items.Add(new ReminderDto(kind.Value.ToString(), first.Year, first.Month, 1, deadline));
        }

        // Timesheets that wait for this person as an approver
        var waiting = await _workflow.PendingAsync(employeeCode);
        if (waiting.Count > 0)
        {
            var oldest = waiting.OrderBy(w => w.Year).ThenBy(w => w.Month).First();
            var deadline = Deadlines.Approval(oldest.Year, oldest.Month);
            var kind = ReminderRules.ForApprover(today, deadline, waiting.Count);
            if (kind != null) items.Add(new ReminderDto(kind.Value.ToString(), oldest.Year, oldest.Month, waiting.Count, deadline));
        }

        return new RemindersDto(today, items);
    }

    private async Task<DateOnly> SubmissionDeadlineAsync(int year, int month) =>
        Deadlines.Submission(year, month, await Reference.CalendarAsync(_db, year));
}
