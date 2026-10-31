namespace Company.Api.Data;

/// <summary>A person of the organisation: master data, place in the chart and the roles that follow from it</summary>
public class Employee
{
    public int Id { get; set; }

    /// <summary>Z-NNN, unique</summary>
    public string Code { get; set; } = "";

    public string Name { get; set; } = "";

    public bool IsActive { get; set; } = true;

    /// <summary>The person this employee reports to; the timesheet climbs this chain</summary>
    public string? SupervisorCode { get; set; }

    /// <summary>The lawyer: the approval chain of employees below stops here</summary>
    public bool IsFinalApprover { get; set; }

    /// <summary>Approver for the person at the top of the chart, who has no supervisor</summary>
    public string? DesignatedApproverCode { get; set; }

    public bool IsAdmin { get; set; }

    public DateOnly? ActiveFrom { get; set; }

    public DateOnly? ActiveTo { get; set; }

    public List<EmployeeAccount> Accounts { get; set; } = new();
}

/// <summary>A sign-in name (DOMAIN\user, user@domain or an Entra name) linked to an employee</summary>
public class EmployeeAccount
{
    public int Id { get; set; }

    public string EmployeeCode { get; set; } = "";

    /// <summary>Lower case, unique</summary>
    public string Account { get; set; } = "";
}

public class StateHoliday
{
    public int Id { get; set; }

    public DateOnly Date { get; set; }

    public string Name { get; set; } = "";
}

/// <summary>A religious holiday marked for one employee (Q-14: holidays are per employee)</summary>
public class ReligiousHoliday
{
    public int Id { get; set; }

    public string EmployeeCode { get; set; } = "";

    public DateOnly Date { get; set; }

    public string Name { get; set; } = "";
}

/// <summary>An entry of the append-only audit log. Nothing in the application updates or deletes these rows.</summary>
public class AuditEvent
{
    public long Id { get; set; }

    public DateTime AtUtc { get; set; }

    public string Actor { get; set; } = "";

    public string Action { get; set; } = "";

    public string EntityType { get; set; } = "";

    public string EntityId { get; set; } = "";

    public string? Details { get; set; }
}

/// <summary>One employee's timesheet for one month</summary>
public class MonthSheet
{
    public int Id { get; set; }

    public string EmployeeCode { get; set; } = "";

    public int Year { get; set; }

    public int Month { get; set; }

    public Timesheet.Domain.TimesheetStatus Status { get; set; } = Timesheet.Domain.TimesheetStatus.Draft;

    public int Revision { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    /// <summary>Changes with every save of the sheet, so two people acting at the same moment cannot both win</summary>
    public int Version { get; set; }

    /// <summary>The approvers frozen at the last submission, in order, separated by commas</summary>
    public string Chain { get; set; } = "";

    /// <summary>Position in <see cref="Chain"/> of the person whose turn it is</summary>
    public int Step { get; set; }

    public List<SheetEntry> Entries { get; set; } = new();

    public List<SheetOvertime> Overtime { get; set; } = new();

    public List<SheetMessage> Messages { get; set; } = new();

    public List<SheetEvent> Events { get; set; } = new();

    public List<string> ChainCodes => Chain.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}

/// <summary>A message with which an approver returned the sheet, and the employee's answer</summary>
public class SheetMessage
{
    public int Id { get; set; }

    public int SheetId { get; set; }

    /// <summary>Order of the messages of the sheet, starting at 0</summary>
    public int Position { get; set; }

    public string FromCode { get; set; } = "";

    public string Text { get; set; } = "";

    public int Revision { get; set; }

    public string? Answer { get; set; }

    public DateTime CreatedUtc { get; set; }
}

/// <summary>What happened to a sheet (submitted, approved, returned, answered), shown as its history</summary>
public class SheetEvent
{
    public int Id { get; set; }

    public int SheetId { get; set; }

    public DateTime AtUtc { get; set; }

    public string Kind { get; set; } = "";

    public string ActorCode { get; set; } = "";

    public string? OnBehalfOf { get; set; }

    public int Revision { get; set; }

    public int Step { get; set; }

    public string? Text { get; set; }
}

/// <summary>What the sheet looked like when it was submitted, so a later correction can be compared with it</summary>
public class SheetRevision
{
    public int Id { get; set; }

    public int SheetId { get; set; }

    public int Revision { get; set; }

    public DateTime SubmittedUtc { get; set; }

    public string Json { get; set; } = "";
}

/// <summary>A regular-hours, absence or hour-based code on a date. Overtime is not stored here; it is derived from the intervals.</summary>
public class SheetEntry
{
    public int Id { get; set; }

    public int SheetId { get; set; }

    public DateOnly Date { get; set; }

    public int Code { get; set; }

    /// <summary>Zero for whole-day absence codes</summary>
    public int Minutes { get; set; }
}

public class SheetOvertime
{
    public int Id { get; set; }

    public int SheetId { get; set; }

    public DateOnly Date { get; set; }

    public TimeOnly Start { get; set; }

    public TimeOnly End { get; set; }

    public string Description { get; set; } = "";
}
