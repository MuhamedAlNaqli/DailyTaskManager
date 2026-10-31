namespace Company.Api.Contracts;

/// <summary>Who the caller is, after sign-in</summary>
public sealed record MeDto(
    string Code,
    string Name,
    string Account,
    bool IsAdmin,
    bool IsApprover,
    bool IsFinalApprover,
    string? SupervisorCode,
    IReadOnlyList<string> ApprovalChain,
    string AuthenticationMode);

public sealed record EmployeeDto(
    string Code,
    string Name,
    bool IsActive,
    string? SupervisorCode,
    bool IsFinalApprover,
    string? DesignatedApproverCode,
    bool IsAdmin,
    DateOnly? ActiveFrom,
    DateOnly? ActiveTo,
    IReadOnlyList<string> Accounts);

/// <summary>Changes to one employee made by the administrator. Null leaves a value as it is; an empty string clears a code.</summary>
public sealed record UpdateEmployeeRequest(
    string? Name,
    bool? IsActive,
    string? SupervisorCode,
    bool? IsFinalApprover,
    string? DesignatedApproverCode,
    bool? IsAdmin,
    DateOnly? ActiveFrom,
    DateOnly? ActiveTo);

public sealed record CsvRequest(string Csv);

public sealed record ImportRowDto(int Line, string Code, string Name, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings);

public sealed record ImportPreviewDto(IReadOnlyList<ImportRowDto> Rows, int ErrorCount, int WarningCount);

public sealed record ImportResultDto(int Created, int Updated, int Skipped);

public sealed record AccountRequest(string Account);

public sealed record DirectoryUserDto(string Account, string DisplayName);

public sealed record AccountMatchDto(string Account, string DisplayName, string? EmployeeCode, string Status);

public sealed record HolidayDto(DateOnly Date, string Name);

public sealed record TimeCodeDto(int Code, string NameBs, string NameEn, string Colour, string Unit, bool InMonthlyFund, bool IsOvertime);

public sealed record CalendarDayDto(DateOnly Date, string Weekday, string Type, string? HolidayName);

public sealed record AuditEventDto(long Id, DateTime AtUtc, string Actor, string Action, string EntityType, string EntityId, string? Details);

/// <summary>For the administrator and IT: what the server sees of the caller, to test Windows or Entra sign-in</summary>
public sealed record AuthDiagnosticsDto(
    string Mode,
    string? IdentityName,
    string? AuthenticationType,
    bool IsAuthenticated,
    IReadOnlyList<string> Candidates,
    string? EmployeeCode,
    IReadOnlyList<ClaimDto> Claims);

public sealed record ClaimDto(string Type, string Value);

/// <summary>An error the desktop app can show as it is</summary>
public sealed record ApiErrorDto(string Code, string Message);

// ---- Timesheet capture ------------------------------------------------------------------------------------------------

public sealed record EntryDto(int Code, int Minutes);

public sealed record AllocationDto(DateOnly Date, int Code, int Minutes);

public sealed record DayDto(
    DateOnly Date,
    string Weekday,
    string Type,
    string? HolidayName,
    IReadOnlyList<EntryDto> Entries,
    IReadOnlyList<AllocationDto> OvertimeAllocations);

public sealed record OvertimeDto(
    int Id,
    DateOnly Date,
    TimeOnly Start,
    TimeOnly End,
    string Description,
    int TotalMinutes,
    IReadOnlyList<AllocationDto> Allocations,
    bool HasRegularWindowConflict);

public sealed record SummaryDto(
    IReadOnlyDictionary<int, int> MinutesByCode,
    IReadOnlyDictionary<int, int> DaysByCode,
    int FundMinutes,
    int ExpectedFundMinutes,
    int OvertimeMinutes);

public sealed record IssueDto(string RuleId, string Severity, DateOnly? Date, string Message);

/// <summary>One employee's month as the employee sees it while filling it in</summary>
public sealed record SheetDto(
    int Year,
    int Month,
    string Status,
    int Revision,
    bool Editable,
    IReadOnlyList<DayDto> Days,
    IReadOnlyList<OvertimeDto> Overtime,
    SummaryDto Summary,
    IReadOnlyList<IssueDto> Issues);

/// <summary>Sets the day's entry. A whole-day absence code needs no minutes; an hours code needs them.</summary>
public sealed record SetEntryRequest(int Code, int Minutes);

public sealed record AbsenceRangeRequest(int Code, DateOnly From, DateOnly To);

public sealed record SkippedDayDto(DateOnly Date, string Reason);

public sealed record AbsencePreviewDto(int Code, IReadOnlyList<DateOnly> Days, IReadOnlyList<SkippedDayDto> Skipped);

public sealed record OvertimeRequest(DateOnly Date, TimeOnly Start, TimeOnly End, string Description);
