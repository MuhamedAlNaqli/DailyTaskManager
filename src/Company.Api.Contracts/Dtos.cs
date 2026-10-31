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
