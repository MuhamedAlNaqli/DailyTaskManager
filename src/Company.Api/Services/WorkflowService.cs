using System.Text.Json;
using Company.Api.Contracts;
using Company.Api.Data;
using Microsoft.EntityFrameworkCore;
using Timesheet.Domain;

namespace Company.Api.Services;

/// <summary>
/// Submitting a month and walking it up the approval chain. The status machine is the domain library's
/// <see cref="TimesheetWorkflow"/>; this service loads the state, applies the transition, keeps the history and the
/// submitted revision, and saves it all in one step. Only an explicit action of the right person moves a sheet on.
/// </summary>
public sealed class WorkflowService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly CompanyDbContext _db;
    private readonly TimesheetService _sheets;
    private readonly ChartService _chart;
    private readonly AuditService _audit;

    public WorkflowService(CompanyDbContext db, TimesheetService sheets, ChartService chart, AuditService audit)
    {
        _db = db;
        _sheets = sheets;
        _chart = chart;
        _audit = audit;
    }

    // ---- The employee ----------------------------------------------------------------------------------------------

    public async Task<SheetDto> SubmitAsync(string employeeCode, int year, int month)
    {
        var sheet = await _sheets.LoadOrCreateAsync(employeeCode, year, month);
        var current = await _sheets.BuildAsync(sheet);

        var outcome = await _chart.ChainOfAsync(employeeCode);
        if (outcome.Chain == null)
            throw new SheetException(409, "no-chain", "The administrator has not set who approves your timesheet yet.");

        var result = TimesheetWorkflow.Submit(ToState(sheet), employeeCode, outcome.Chain, current.Issues.Any(i => i.Severity == nameof(Severity.Blocking)));
        Apply(sheet, result);

        // What was submitted is kept, so a later correction can be compared with it
        var frozen = await _sheets.BuildAsync(sheet);
        _db.SheetRevisions.Add(new SheetRevision
        {
            SheetId = sheet.Id,
            Revision = sheet.Revision,
            SubmittedUtc = DateTime.UtcNow,
            Json = JsonSerializer.Serialize(frozen with { Workflow = null }, Json)
        });

        await SaveAsync();
        return await _sheets.BuildAsync(sheet);
    }

    public async Task<SheetDto> AnswerAsync(string employeeCode, int year, int month, int index, string answer)
    {
        var sheet = await _sheets.LoadOrCreateAsync(employeeCode, year, month);
        Apply(sheet, TimesheetWorkflow.Answer(ToState(sheet), employeeCode, index, answer));
        await SaveAsync();
        return await _sheets.BuildAsync(sheet);
    }

    // ---- The approvers ---------------------------------------------------------------------------------------------

    /// <summary>The submitted timesheets that are waiting for this person right now</summary>
    public async Task<List<PendingApprovalDto>> PendingAsync(string approverCode)
    {
        var submitted = await _db.Sheets.Include(s => s.Entries).Include(s => s.Overtime).Include(s => s.Messages).Include(s => s.Events)
            .Where(s => s.Status == TimesheetStatus.Submitted).ToListAsync();

        var result = new List<PendingApprovalDto>();
        foreach (var sheet in submitted.Where(s => s.Step < s.ChainCodes.Count && TimesheetService.Same(s.ChainCodes[s.Step], approverCode)))
        {
            var dto = await _sheets.BuildAsync(sheet, approverCode);
            var sent = sheet.Events.Where(e => e.Kind == "Submitted" && e.Revision == sheet.Revision).Select(e => e.AtUtc).DefaultIfEmpty(sheet.UpdatedUtc).Max();
            result.Add(new PendingApprovalDto(
                sheet.EmployeeCode, dto.Workflow!.OwnerName, sheet.Year, sheet.Month, sheet.Revision, sheet.Step,
                DateTime.SpecifyKind(sent, DateTimeKind.Utc),
                dto.Issues.Count(i => i.Severity == nameof(Severity.Blocking)),
                dto.Issues.Count(i => i.Severity != nameof(Severity.Blocking))));
        }

        return result.OrderBy(p => p.SubmittedUtc).ThenBy(p => p.EmployeeName).ToList();
    }

    /// <summary>Someone else's sheet, read only. Only the owner and the people of its approval chain may look at it.</summary>
    public async Task<SheetDto> ReviewAsync(string viewerCode, string ownerCode, int year, int month)
    {
        var sheet = await FindForReviewAsync(viewerCode, ownerCode, year, month);
        return await _sheets.BuildAsync(sheet, viewerCode);
    }

    public async Task<SheetDto> ApproveAsync(string approverCode, string ownerCode, int year, int month)
    {
        var sheet = await FindForReviewAsync(approverCode, ownerCode, year, month);
        Apply(sheet, TimesheetWorkflow.Approve(ToState(sheet), approverCode));
        await SaveAsync();
        return await _sheets.BuildAsync(sheet, approverCode);
    }

    public async Task<SheetDto> ReturnAsync(string approverCode, string ownerCode, int year, int month, string message)
    {
        var sheet = await FindForReviewAsync(approverCode, ownerCode, year, month);
        Apply(sheet, TimesheetWorkflow.Return(ToState(sheet), approverCode, message));
        await SaveAsync();
        return await _sheets.BuildAsync(sheet, approverCode);
    }

    /// <summary>The sheet exactly as it was submitted in the given revision</summary>
    public async Task<SheetDto> RevisionAsync(string viewerCode, string ownerCode, int year, int month, int revision)
    {
        var sheet = await FindForReviewAsync(viewerCode, ownerCode, year, month);
        var stored = await _db.SheetRevisions.AsNoTracking().FirstOrDefaultAsync(r => r.SheetId == sheet.Id && r.Revision == revision)
                     ?? throw new SheetException(404, "not-found", "There is no such revision.");
        var dto = JsonSerializer.Deserialize<SheetDto>(stored.Json, Json)!;
        return dto with { Editable = false, Workflow = (await _sheets.BuildAsync(sheet, viewerCode)).Workflow };
    }

    // ---- Helpers ---------------------------------------------------------------------------------------------------

    internal async Task<MonthSheet> FindForReviewAsync(string viewerCode, string ownerCode, int year, int month)
    {
        var sheet = await _db.Sheets.Include(s => s.Entries).Include(s => s.Overtime).Include(s => s.Messages).Include(s => s.Events)
            .FirstOrDefaultAsync(s => s.EmployeeCode == ownerCode && s.Year == year && s.Month == month);

        var allowed = sheet != null && (TimesheetService.Same(viewerCode, ownerCode) || sheet.ChainCodes.Any(c => TimesheetService.Same(c, viewerCode)));
        if (!allowed) throw new SheetException(403, "forbidden", "You are not allowed to see this timesheet.");
        return sheet!;
    }

    private static TimesheetState ToState(MonthSheet sheet) => new(
        sheet.EmployeeCode,
        sheet.Status,
        sheet.Revision,
        sheet.ChainCodes,
        sheet.Step,
        sheet.Messages.OrderBy(m => m.Position).Select(m => new ReturnMessage(m.FromCode, m.Text, m.Revision, m.Answer)).ToList());

    /// <summary>Writes the new state back to the sheet and records the event, in the history and in the audit log</summary>
    private void Apply(MonthSheet sheet, Transition transition)
    {
        if (!transition.Ok) throw Refusal(transition.Error);

        var state = transition.State;
        var ev = transition.Event!;

        sheet.Status = state.Status;
        sheet.Revision = state.Revision;
        sheet.Step = state.Step;
        sheet.Chain = string.Join(',', state.ChainOrEmpty);

        // Messages: a return adds one, an answer fills one
        var messages = state.MessagesOrEmpty;
        for (var i = 0; i < messages.Count; i++)
        {
            var existing = sheet.Messages.FirstOrDefault(m => m.Position == i);
            if (existing == null)
            {
                sheet.Messages.Add(new SheetMessage
                {
                    Position = i, FromCode = messages[i].From, Text = messages[i].Text, Revision = messages[i].Revision, Answer = messages[i].Answer, CreatedUtc = DateTime.UtcNow
                });
            }
            else
            {
                existing.Answer = messages[i].Answer;
            }
        }

        sheet.Events.Add(new SheetEvent
        {
            AtUtc = DateTime.UtcNow, Kind = ev.Kind, ActorCode = ev.Actor, OnBehalfOf = ev.OnBehalfOf, Revision = ev.Revision, Step = ev.Step, Text = ev.Text
        });

        _sheets.Touch(sheet, ev.Kind, $"revision {ev.Revision}, step {ev.Step}" + (ev.Text == null ? "" : ": " + ev.Text));
    }

    private async Task SaveAsync()
    {
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new SheetException(409, "changed", "The timesheet was changed by someone else a moment ago. Open it again.");
        }
    }

    private static SheetException Refusal(WorkflowError error) => error switch
    {
        WorkflowError.NotAllowedInThisStatus => new(409, "wrong-status", "This is not possible in the current status of the timesheet."),
        WorkflowError.NotTheOwner => new(403, "forbidden", "Only the owner of the timesheet can do this."),
        WorkflowError.NotTheCurrentApprover => new(403, "not-your-turn", "It is not your turn to decide on this timesheet."),
        WorkflowError.SelfApproval => new(403, "self-approval", "Nobody approves their own timesheet."),
        WorkflowError.BlockingIssues => new(409, "blocking-issues", "Fix the blocking findings before submitting."),
        WorkflowError.UnansweredMessages => new(409, "unanswered-messages", "Answer every message of the approver before submitting again."),
        WorkflowError.MessageRequired => new(400, "message-required", "A text is required."),
        WorkflowError.EmptyChain => new(409, "no-chain", "The administrator has not set who approves your timesheet yet."),
        WorkflowError.UnknownMessage => new(404, "not-found", "There is no such message."),
        _ => new(400, "invalid", "The request is not valid.")
    };
}
