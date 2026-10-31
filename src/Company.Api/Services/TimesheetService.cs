using Company.Api.Contracts;
using Company.Api.Data;
using Company.Api.Endpoints;
using Microsoft.EntityFrameworkCore;
using Timesheet.Domain;

namespace Company.Api.Services;

/// <summary>A refusal the desktop app can show as it is</summary>
public sealed class SheetException : Exception
{
    public SheetException(int status, string code, string message) : base(message)
    {
        Status = status;
        Code = code;
    }

    public int Status { get; }

    public string Code { get; }
}

/// <summary>
/// An employee's own month: entries, absence ranges and overtime, with everything derived (overtime split, totals, the fund
/// and the findings of the rules) computed by the domain library. The employee edits only while the sheet is a draft or
/// returned; every change is audited.
/// </summary>
public sealed class TimesheetService
{
    private readonly CompanyDbContext _db;
    private readonly AuditService _audit;

    public TimesheetService(CompanyDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    // ---- Reading ---------------------------------------------------------------------------------------------------

    public async Task<SheetDto> GetAsync(string employeeCode, int year, int month)
    {
        CheckMonth(year, month);
        var sheet = await LoadOrCreateAsync(employeeCode, year, month);
        return await BuildAsync(sheet);
    }

    // ---- Entries ---------------------------------------------------------------------------------------------------

    public async Task<SheetDto> SetEntryAsync(string employeeCode, int year, int month, DateOnly date, SetEntryRequest request)
    {
        var sheet = await EditableAsync(employeeCode, year, month);
        CheckDate(date, year, month);

        if (!TimeCodes.TryGet(request.Code, out var code)) throw new SheetException(400, "unknown-code", $"The code {request.Code} does not exist.");
        if (code.IsOvertime) throw new SheetException(400, "overtime-derived", "Overtime is entered as an interval, not as a code on a day.");

        var minutes = request.Minutes;
        if (code.Unit == CodeUnit.Hours)
        {
            if (minutes <= 0 || minutes > OvertimeInterval.MinutesPerDay)
                throw new SheetException(400, "invalid-hours", "Enter between 1 minute and 24 hours.");
        }
        else
        {
            minutes = 0;
        }

        // A day has one base entry (regular hours or an absence); other hour codes are replaced per code
        var existing = sheet.Entries.Where(e => e.Date == date && (code.IsBaseEntry ? TimeCodes.Get(e.Code).IsBaseEntry : e.Code == code.Code)).ToList();
        foreach (var old in existing) _db.SheetEntries.Remove(old);
        sheet.Entries.RemoveAll(existing.Contains);
        sheet.Entries.Add(new SheetEntry { Date = date, Code = code.Code, Minutes = minutes });

        Touch(sheet, "EntrySet", $"{date:yyyy-MM-dd} {code.Code} {minutes}");
        await _db.SaveChangesAsync();
        return await BuildAsync(sheet);
    }

    public async Task<SheetDto> ClearEntryAsync(string employeeCode, int year, int month, DateOnly date, int code)
    {
        var sheet = await EditableAsync(employeeCode, year, month);
        var entry = sheet.Entries.FirstOrDefault(e => e.Date == date && e.Code == code);
        if (entry == null) throw new SheetException(404, "not-found", "There is no such entry.");

        _db.SheetEntries.Remove(entry);
        sheet.Entries.Remove(entry);
        Touch(sheet, "EntryCleared", $"{date:yyyy-MM-dd} {code}");
        await _db.SaveChangesAsync();
        return await BuildAsync(sheet);
    }

    /// <summary>Regular hours (the daily norm) on every working day that has no base entry. The employee triggers it explicitly.</summary>
    public async Task<SheetDto> FillStandardDaysAsync(string employeeCode, int year, int month)
    {
        var sheet = await EditableAsync(employeeCode, year, month);
        var context = await ContextAsync(employeeCode, year);
        var added = 0;

        foreach (var date in WorkCalendar.DaysOf(year, month))
        {
            if (!context.Employee.IsActiveOn(date)) continue;
            if (context.Calendar.Resolve(date, context.Religious).Type != DayType.Working) continue;
            if (sheet.Entries.Any(e => e.Date == date && TimeCodes.Get(e.Code).IsBaseEntry)) continue;

            sheet.Entries.Add(new SheetEntry { Date = date, Code = Codes.RegularWork, Minutes = WorkingTimeSettings.Default.DailyNormMinutes });
            added++;
        }

        Touch(sheet, "StandardDaysFilled", $"{year}-{month:00}: {added} days");
        await _db.SaveChangesAsync();
        return await BuildAsync(sheet);
    }

    // ---- Absence ranges ----------------------------------------------------------------------------------------------

    public async Task<AbsencePreviewDto> PreviewAbsenceAsync(string employeeCode, int year, int month, AbsenceRangeRequest request)
    {
        CheckMonth(year, month);
        var expansion = await ExpandAsync(employeeCode, year, month, request);
        return new AbsencePreviewDto(
            request.Code,
            expansion.Days,
            expansion.Skipped.Select(s => new SkippedDayDto(s.Date, s.Reason.ToString())).ToList());
    }

    public async Task<SheetDto> ApplyAbsenceAsync(string employeeCode, int year, int month, AbsenceRangeRequest request)
    {
        var sheet = await EditableAsync(employeeCode, year, month);
        var expansion = await ExpandAsync(employeeCode, year, month, request);

        foreach (var date in expansion.Days)
        {
            var existing = sheet.Entries.Where(e => e.Date == date && TimeCodes.Get(e.Code).IsBaseEntry).ToList();
            foreach (var old in existing) _db.SheetEntries.Remove(old);
            sheet.Entries.RemoveAll(existing.Contains);
            sheet.Entries.Add(new SheetEntry { Date = date, Code = request.Code, Minutes = 0 });
        }

        Touch(sheet, "AbsenceRangeApplied", $"{request.Code} {request.From:yyyy-MM-dd}..{request.To:yyyy-MM-dd}: {expansion.Days.Count} days");
        await _db.SaveChangesAsync();
        return await BuildAsync(sheet);
    }

    private async Task<AbsenceExpansion> ExpandAsync(string employeeCode, int year, int month, AbsenceRangeRequest request)
    {
        // The range must lie inside the month being filled in
        CheckDate(request.From, year, month);
        CheckDate(request.To, year, month);

        var context = await ContextAsync(employeeCode, year);
        try
        {
            return AbsenceRanges.Expand(request.Code, request.From, request.To, context.Calendar, context.Religious);
        }
        catch (ArgumentException ex)
        {
            throw new SheetException(400, "invalid-absence", ex.Message);
        }
    }

    // ---- Overtime --------------------------------------------------------------------------------------------------

    public async Task<SheetDto> AddOvertimeAsync(string employeeCode, int year, int month, OvertimeRequest request)
    {
        var sheet = await EditableAsync(employeeCode, year, month);
        Check(request, year, month);

        sheet.Overtime.Add(new SheetOvertime { Date = request.Date, Start = request.Start, End = request.End, Description = request.Description?.Trim() ?? "" });
        Touch(sheet, "OvertimeAdded", $"{request.Date:yyyy-MM-dd} {request.Start:HH\\:mm}-{request.End:HH\\:mm}");
        await _db.SaveChangesAsync();
        return await BuildAsync(sheet);
    }

    public async Task<SheetDto> UpdateOvertimeAsync(string employeeCode, int year, int month, int id, OvertimeRequest request)
    {
        var sheet = await EditableAsync(employeeCode, year, month);
        var record = sheet.Overtime.FirstOrDefault(o => o.Id == id) ?? throw new SheetException(404, "not-found", "There is no such overtime record.");
        Check(request, year, month);

        record.Date = request.Date;
        record.Start = request.Start;
        record.End = request.End;
        record.Description = request.Description?.Trim() ?? "";
        Touch(sheet, "OvertimeChanged", $"{id}: {request.Date:yyyy-MM-dd} {request.Start:HH\\:mm}-{request.End:HH\\:mm}");
        await _db.SaveChangesAsync();
        return await BuildAsync(sheet);
    }

    public async Task<SheetDto> DeleteOvertimeAsync(string employeeCode, int year, int month, int id)
    {
        var sheet = await EditableAsync(employeeCode, year, month);
        var record = sheet.Overtime.FirstOrDefault(o => o.Id == id) ?? throw new SheetException(404, "not-found", "There is no such overtime record.");

        _db.SheetOvertime.Remove(record);
        sheet.Overtime.Remove(record);
        Touch(sheet, "OvertimeDeleted", $"{id}: {record.Date:yyyy-MM-dd}");
        await _db.SaveChangesAsync();
        return await BuildAsync(sheet);
    }

    private static void Check(OvertimeRequest request, int year, int month)
    {
        CheckDate(request.Date, year, month);
        if (new OvertimeInterval(request.Date, request.Start, request.End).TotalMinutes == 0)
            throw new SheetException(400, "invalid-interval", "The start and the end must differ.");
    }

    // ---- Building the answer -------------------------------------------------------------------------------------------

    private sealed record Context(WorkCalendar Calendar, HashSet<DateOnly> Religious, EmployeeStatus Employee);

    private async Task<Context> ContextAsync(string employeeCode, int year)
    {
        var calendar = await Reference.CalendarAsync(_db, year);
        var religious = (await _db.ReligiousHolidays.AsNoTracking().Where(h => h.EmployeeCode == employeeCode).Select(h => h.Date).ToListAsync()).ToHashSet();
        var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Code == employeeCode)
                       ?? throw new SheetException(404, "not-found", "The employee was not found.");
        return new Context(calendar, religious, new EmployeeStatus(true, employee.ActiveFrom, employee.ActiveTo));
    }

    private async Task<SheetDto> BuildAsync(MonthSheet sheet)
    {
        var context = await ContextAsync(sheet.EmployeeCode, sheet.Year);
        var settings = WorkingTimeSettings.Default;
        var inMonth = (DateOnly d) => d.Year == sheet.Year && d.Month == sheet.Month;

        var entries = sheet.Entries.Select(e => new DayEntry(e.Date, e.Code, e.Minutes)).ToList();
        var records = sheet.Overtime.OrderBy(o => o.Date).ThenBy(o => o.Start).ToList();

        var classified = records.ToDictionary(
            r => r.Id,
            r => OvertimeClassifier.Classify(new OvertimeInterval(r.Date, r.Start, r.End), d => context.Calendar.DayTypeOf(d, context.Religious), settings));
        var allocations = classified.Values.SelectMany(c => c.Allocations).ToList();
        var monthAllocations = allocations.Where(a => inMonth(a.Date)).ToList();

        var summary = MonthlySummaryCalculator.Compute(entries, monthAllocations, settings, context.Calendar);
        var expected = MonthlySummaryCalculator.ExpectedFundMinutes(sheet.Year, sheet.Month, settings, context.Employee.ActiveFrom, context.Employee.ActiveTo);

        var data = new TimesheetData(
            sheet.EmployeeCode, sheet.Year, sheet.Month, entries,
            records.Select(r => new OvertimeRecord(r.Date, r.Start, r.End, r.Description)).ToList(),
            context.Calendar, context.Religious, settings, context.Employee);
        var issues = TimesheetValidator.Validate(data);

        var days = WorkCalendar.DaysOf(sheet.Year, sheet.Month).Select(date =>
        {
            var info = context.Calendar.Resolve(date, context.Religious);
            return new DayDto(
                date,
                WorkCalendar.WeekdayInitial(date),
                info.Type.ToString(),
                info.HolidayName,
                sheet.Entries.Where(e => e.Date == date).OrderBy(e => e.Code).Select(e => new EntryDto(e.Code, e.Minutes)).ToList(),
                monthAllocations.Where(a => a.Date == date).Select(a => new AllocationDto(a.Date, a.Code, a.Minutes)).ToList());
        }).ToList();

        var overtime = records.Select(r =>
        {
            var c = classified[r.Id];
            return new OvertimeDto(
                r.Id, r.Date, r.Start, r.End, r.Description, c.TotalMinutes,
                c.Allocations.Select(a => new AllocationDto(a.Date, a.Code, a.Minutes)).ToList(),
                c.Conflicts.Count > 0);
        }).ToList();

        return new SheetDto(
            sheet.Year, sheet.Month, sheet.Status.ToString(), sheet.Revision, IsEditable(sheet), days, overtime,
            new SummaryDto(summary.MinutesByCode, summary.DaysByCode, summary.FundMinutes, expected, summary.OvertimeMinutes),
            issues.Select(i => new IssueDto(i.RuleId, i.Severity.ToString(), i.Date, i.Message)).ToList());
    }

    // ---- Helpers ---------------------------------------------------------------------------------------------------

    private static bool IsEditable(MonthSheet sheet) => sheet.Status is TimesheetStatus.Draft or TimesheetStatus.Returned;

    private async Task<MonthSheet> LoadOrCreateAsync(string employeeCode, int year, int month)
    {
        var sheet = await _db.Sheets.Include(s => s.Entries).Include(s => s.Overtime)
            .FirstOrDefaultAsync(s => s.EmployeeCode == employeeCode && s.Year == year && s.Month == month);
        if (sheet != null) return sheet;

        sheet = new MonthSheet { EmployeeCode = employeeCode, Year = year, Month = month, CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow };
        _db.Sheets.Add(sheet);
        await _db.SaveChangesAsync();
        return sheet;
    }

    private async Task<MonthSheet> EditableAsync(string employeeCode, int year, int month)
    {
        CheckMonth(year, month);
        var sheet = await LoadOrCreateAsync(employeeCode, year, month);
        if (!IsEditable(sheet))
            throw new SheetException(409, "locked", "The timesheet is submitted or approved and cannot be changed.");
        return sheet;
    }

    private void Touch(MonthSheet sheet, string action, string details)
    {
        sheet.UpdatedUtc = DateTime.UtcNow;
        _audit.Record(action, "MonthSheet", $"{sheet.EmployeeCode}/{sheet.Year}-{sheet.Month:00}", details);
    }

    private static void CheckMonth(int year, int month)
    {
        if (year < 2000 || year > 2100 || month < 1 || month > 12)
            throw new SheetException(400, "invalid-month", "Choose a month between 2000 and 2100.");
    }

    private static void CheckDate(DateOnly date, int year, int month)
    {
        if (date.Year != year || date.Month != month)
            throw new SheetException(400, "outside-month", "The date is not in this month.");
    }
}
