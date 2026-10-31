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

    public List<SheetEntry> Entries { get; set; } = new();

    public List<SheetOvertime> Overtime { get; set; } = new();
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
