using Company.Api.Data;
using Company.Api.Printing;
using Microsoft.EntityFrameworkCore;
using Timesheet.Domain;

namespace Company.Api.Services;

public sealed record PrintedFile(byte[] Bytes, string ContentType, string FileName);

/// <summary>
/// The printed timesheet in the layout of the current Excel sheet: one employee's month (for the owner and the people of the
/// chain), or the approved months of everybody (for finance and the administrator). Opening the sheets of others is audited.
/// </summary>
public sealed class PrintService
{
    private const string ExcelType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string PdfType = "application/pdf";

    private readonly CompanyDbContext _db;
    private readonly TimesheetService _sheets;
    private readonly WorkflowService _workflow;
    private readonly AuditService _audit;

    public PrintService(CompanyDbContext db, TimesheetService sheets, WorkflowService workflow, AuditService audit)
    {
        _db = db;
        _sheets = sheets;
        _workflow = workflow;
        _audit = audit;
    }

    /// <summary>The caller's own month, in any status (the status is printed on it)</summary>
    public async Task<PrintedFile> OwnAsync(string employeeCode, int year, int month, string format)
    {
        CheckFormat(format);
        var sheet = await _sheets.LoadOrCreateAsync(employeeCode, year, month);
        return await RenderAsync(new[] { sheet }, year, month, format, single: true);
    }

    /// <summary>Somebody else's month, for the people of its approval chain</summary>
    public async Task<PrintedFile> ReviewAsync(string viewerCode, string ownerCode, int year, int month, string format)
    {
        CheckFormat(format);
        var sheet = await _workflow.FindForReviewAsync(viewerCode, ownerCode, year, month);
        return await RenderAsync(new[] { sheet }, year, month, format, single: true);
    }

    /// <summary>Every approved timesheet of the month, for finance</summary>
    public async Task<PrintedFile> ApprovedAsync(int year, int month, string format)
    {
        CheckFormat(format);
        var sheets = await _db.Sheets.Include(s => s.Entries).Include(s => s.Overtime).Include(s => s.Messages).Include(s => s.Events)
            .Where(s => s.Year == year && s.Month == month && s.Status == TimesheetStatus.Approved).OrderBy(s => s.EmployeeCode).ToListAsync();
        if (sheets.Count == 0) throw new SheetException(404, "no-approved-sheets", "No timesheet has been approved for this month yet.");

        _audit.Record("ApprovedSheetsPrinted", "MonthSheet", $"{year}-{month:00}", $"{sheets.Count} timesheets, {format}");
        await _db.SaveChangesAsync();
        return await RenderAsync(sheets, year, month, format, single: false);
    }

    private async Task<PrintedFile> RenderAsync(IReadOnlyList<MonthSheet> sheets, int year, int month, string format, bool single)
    {
        var codes = sheets.Select(s => s.EmployeeCode).Distinct().ToList();
        var names = await _db.Employees.AsNoTracking().Where(e => codes.Contains(e.Code)).ToDictionaryAsync(e => e.Code, e => e.Name);

        var employees = new List<PrintedEmployee>();
        foreach (var sheet in sheets)
            employees.Add(new PrintedEmployee(sheet.EmployeeCode, names.GetValueOrDefault(sheet.EmployeeCode, sheet.EmployeeCode), await _sheets.BuildAsync(sheet)));

        var statusLine = single
            ? "Status: " + LegacySheet.StatusText(employees[0].Sheet.Status) + (employees[0].Sheet.Status == "Approved" ? "" : " (nije odobreno)")
            : "Odobrene šihtarice";
        var isExcel = format.Equals("xlsx", StringComparison.OrdinalIgnoreCase);
        var stem = single ? $"Sihtarica_{employees[0].Code}_{year}-{month:00}" : $"Sihtarice_{year}-{month:00}";

        return isExcel
            ? new PrintedFile(LegacySheet.ToExcel(year, month, employees, statusLine), ExcelType, stem + ".xlsx")
            : new PrintedFile(LegacySheet.ToPdf(year, month, employees, statusLine), PdfType, stem + ".pdf");
    }

    private static void CheckFormat(string format)
    {
        if (!format.Equals("xlsx", StringComparison.OrdinalIgnoreCase) && !format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            throw new SheetException(400, "invalid-format", "Choose xlsx or pdf.");
    }
}
