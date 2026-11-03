using Company.Api.Client;
using Company.Api.Contracts;
using DailyTaskManager.Avalonia.EmployeeMode;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

namespace DailyTaskManager.UI.Tests;

/// <summary>The employee view model against a server that is a few lines of code: no window, no network, no file dialog</summary>
[TestClass]
public class EmployeeViewModelTests
{
    // ---- the doubles ---------------------------------------------------------------------------------------------------------

    private sealed class FakeClient : ICompanyApiClient
    {
        public List<string> Calls { get; } = new();
        public Func<int, int, Task<SheetDto>> GetSheet { get; set; } = (y, m) => Task.FromResult(Sheets.Make(y, m));
        public Func<string, int, int, Task<SheetDto>> GetReview { get; set; } = (c, y, m) => Task.FromResult(Sheets.Make(y, m, SheetStatus.Submitted, editable: false, owner: c, canAct: true));
        public List<PendingApprovalDto> Pending { get; set; } = new();
        public List<ReminderDto> Reminders { get; set; } = new();
        public bool IsApprover { get; set; }
        public bool SheetExport { get; set; }

        private Task<T> Record<T>(string call, T value) { Calls.Add(call); return Task.FromResult(value); }

        public Task<MeDto> GetMeAsync(CancellationToken ct = default) =>
            Record("me", new MeDto("Z-001", "Amra Adžamija", @"corp\amra", false, IsApprover, false, "Z-002", new[] { "Z-002" }, "Test", false, SheetExport));

        public Task<SheetDto> GetSheetAsync(int year, int month, CancellationToken ct = default) { Calls.Add($"sheet {year}-{month}"); return GetSheet(year, month); }
        public Task<SheetDto> GetReviewAsync(string code, int year, int month, CancellationToken ct = default) { Calls.Add($"review {code} {year}-{month}"); return GetReview(code, year, month); }
        public Task<List<PendingApprovalDto>> GetPendingAsync(CancellationToken ct = default) => Record("pending", Pending);
        public Task<RemindersDto> GetRemindersAsync(CancellationToken ct = default) => Record("reminders", new RemindersDto(new DateOnly(2026, 9, 10), Reminders));
        public Task<SheetDto> SubmitAsync(int year, int month, CancellationToken ct = default) =>
            Record($"submit {year}-{month}", Sheets.Make(year, month, SheetStatus.Submitted, editable: false));
        public Task<SheetDto> ApproveAsync(string code, int year, int month, CancellationToken ct = default) =>
            Record($"approve {code} {year}-{month}", Sheets.Make(year, month, SheetStatus.Approved, editable: false, owner: code));
        public Task<SheetDto> ReturnAsync(string code, int year, int month, string message, CancellationToken ct = default) =>
            Record($"return {code} {year}-{month}: {message}", Sheets.Make(year, month, SheetStatus.Returned, editable: false, owner: code));
        public Task<SheetDto> SetEntryAsync(int year, int month, DateOnly date, SetEntryRequest request, CancellationToken ct = default) =>
            Record($"entry {date:yyyy-MM-dd} {request.Code} {request.Minutes}", Sheets.Make(year, month));
        public Task<SheetDto> FillStandardDaysAsync(int year, int month, CancellationToken ct = default) => Record($"fill {year}-{month}", Sheets.Make(year, month));
        public Task<SheetDto> AddOvertimeAsync(int year, int month, OvertimeRequest request, CancellationToken ct = default) => Record($"overtime add {request.Date:yyyy-MM-dd}", Sheets.Make(year, month));
        public Task<SheetDto> UpdateOvertimeAsync(int year, int month, int id, OvertimeRequest request, CancellationToken ct = default) => Record($"overtime update {id}", Sheets.Make(year, month));
        public Task<SheetDto> ApplyAbsenceAsync(int year, int month, AbsenceRangeRequest request, CancellationToken ct = default) => Record($"absence {request.Code}", Sheets.Make(year, month));
        public Task<SheetDto> AnswerAsync(int year, int month, int index, string answer, CancellationToken ct = default) => Record($"answer {index}: {answer}", Sheets.Make(year, month));

        public Task<AuthDiagnosticsDto> GetDiagnosticsAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<TimeCodeDto>> GetTimeCodesAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<SheetDto> ClearEntryAsync(int year, int month, DateOnly date, int code, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<AbsencePreviewDto> PreviewAbsenceAsync(int year, int month, AbsenceRangeRequest request, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<SheetDto> DeleteOvertimeAsync(int year, int month, int id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<SheetDto> GetRevisionAsync(int year, int month, int revision, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<SheetDto> GetReviewRevisionAsync(string code, int year, int month, int revision, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<DownloadedFile> GetPrintAsync(int year, int month, string format, CancellationToken ct = default) => Record($"print {format}", new DownloadedFile("a." + format, new byte[] { 1 }));
        public Task<DownloadedFile> GetReviewPrintAsync(string code, int year, int month, string format, CancellationToken ct = default) => Record($"print {code} {format}", new DownloadedFile("a." + format, new byte[] { 1 }));
        public Task<DownloadedFile> GetTeamPrintAsync(int year, int month, string format, CancellationToken ct = default) => Record($"team {format}", new DownloadedFile("t." + format, new byte[] { 1 }));
        public Task<DownloadedFile> GetApprovedExportAsync(int year, int month, string format, CancellationToken ct = default) => throw new NotSupportedException();

        public List<TeamMemberDto> Team { get; set; } = new();
        public Dictionary<string, string> RemindRefusals { get; } = new();
        public string SummaryText { get; set; } = "Šihtarice za Septembar 2026";
        public Task<TeamSummaryDto> GetTeamSummaryTextAsync(int year, int month, CancellationToken ct = default) =>
            Record($"summary-text {year}-{month}", new TeamSummaryDto(year, month,
                new[] { new SummaryGroupDto(null, new[] { new SummaryPersonDto("Amra Adžamija", new[] { "Nema prekovremenih sati niti izostanaka." }, "Šihtarica: Nacrt (nije odobreno)") }) },
                Array.Empty<SummaryOvertimeDto>(), SummaryText));
        public Task<DownloadedFile> GetTeamSummaryAsync(int year, int month, string format, CancellationToken ct = default) => Record($"summary {format}", new DownloadedFile("s." + format, new byte[] { 1 }));

        public Task<TeamStatusDto> GetTeamStatusAsync(int year, int month, CancellationToken ct = default)
        {
            Calls.Add($"team-status {year}-{month}");
            return Team.Count == 0
                ? throw new ApiException(System.Net.HttpStatusCode.NotFound, "no-team", "Nobody reports to you.")
                : Task.FromResult(new TeamStatusDto(year, month, new DateOnly(year, month, 25), Team));
        }

        public Task RemindAsync(string code, int year, int month, CancellationToken ct = default)
        {
            Calls.Add($"remind {code} {year}-{month}");
            if (RemindRefusals.TryGetValue(code, out var refusal)) throw new ApiException(System.Net.HttpStatusCode.Conflict, refusal, refusal);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDialogs : IEmployeeDialogs
    {
        public bool Answer { get; set; } = true;
        public string? Prompted { get; set; }
        public EntryResult? Entry { get; set; }
        public List<string> Asked { get; } = new();

        public Task<bool> ConfirmAsync(string title, string message) { Asked.Add(title + ": " + message); return Task.FromResult(Answer); }
        public Task<EntryResult?> EntryAsync(EntryViewModel viewModel) { Asked.Add("entry"); return Task.FromResult(Entry); }
        public Task<string?> PromptAsync(TextPromptViewModel viewModel) { Asked.Add("prompt"); return Task.FromResult(Prompted); }
    }

    private sealed class FakeSaver : ISheetFileSaver
    {
        public FileSaveResult Result { get; set; } = new("C:\\out\\a.xlsx", null);
        public Task<FileSaveResult> SaveAsync(string? format, Func<string, Task<DownloadedFile>> download) => Task.FromResult(Result);
    }

    private sealed class FakeShell : IEmployeeShell
    {
        public int SignedOut { get; private set; }
        public void SignOut() => SignedOut++;
        public List<string> Copied { get; } = new();
        public Task CopyAsync(string text) { Copied.Add(text); return Task.CompletedTask; }
    }

    private static class Sheets
    {
        public static SheetDto Make(int year, int month, SheetStatus status = SheetStatus.Draft, bool editable = true, string owner = "Z-001", bool canAct = false, bool canSubmit = false)
        {
            var days = Enumerable.Range(1, DateTime.DaysInMonth(year, month)).Select(d =>
            {
                var date = new DateOnly(year, month, d);
                var weekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                return new DayDto(date, "P", weekend ? DayKind.Weekend : DayKind.Working, null, new List<EntryDto>(), new List<AllocationDto>());
            }).ToList();

            var workflow = new WorkflowDto(owner, "Amra Adžamija", new[] { new ChainStepDto("Z-002", "Benjamin Bašić") }, 0, status == SheetStatus.Submitted ? "Z-002" : null,
                status == SheetStatus.Submitted ? "Benjamin Bašić" : null, new List<MessageDto>(), new List<HistoryDto>(), new List<RevisionDto>(), canSubmit, canAct);
            return new SheetDto(year, month, status, 0, editable, days, new List<OvertimeDto>(),
                new SummaryDto(new Dictionary<int, int>(), new Dictionary<int, int>(), 0, 176 * 60, 0), new List<IssueDto>(), workflow, new List<CarriedOvertimeDto>());
        }
    }

    private static (EmployeeViewModel Vm, FakeClient Client, FakeDialogs Dialogs, FakeSaver Saver, FakeShell Shell) Create()
    {
        var client = new FakeClient();
        var dialogs = new FakeDialogs();
        var saver = new FakeSaver();
        var shell = new FakeShell();
        var vm = new EmployeeViewModel(client, dialogs, saver, shell, NullLogger<EmployeeViewModel>.Instance) { Year = 2026, Month = 9 };
        return (vm, client, dialogs, saver, shell);
    }

    // ---- loading ----------------------------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task LoadingShowsTheMonth_TheReminders_AndWhatWaitsForApproval()
    {
        var (vm, client, _, _, _) = Create();
        client.IsApprover = true;
        client.Pending.Add(new PendingApprovalDto("Z-004", "Dino Kadrović", 2026, 9, 1, 0, DateTime.UtcNow, 0, 1));
        client.Reminders.Add(new ReminderDto(ReminderType.SubmitOverdue, 2026, 8, 1, new DateOnly(2026, 8, 25)));

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.IsTrue(vm.IsConnected);
        Assert.AreEqual(30, vm.Sheet.Days.Count);
        Assert.AreEqual(1 + 30, vm.Sheet.CalendarCells.Count, "September 2026 starts on a Tuesday: one empty square, then the days");
        Assert.IsTrue(vm.IsApprover);
        Assert.IsTrue(vm.Reminders.HasAny);
        Assert.AreEqual("Za odobravanje (1)", vm.PendingApprovals.TabText);
        Assert.AreEqual("", vm.ErrorText);
    }

    [TestMethod]
    public async Task AServerThatCannotBeReached_IsExplainedInTheWindow()
    {
        var (vm, client, _, _, _) = Create();
        client.GetSheet = (_, _) => throw new ApiException(HttpStatusCode.ServiceUnavailable, "unreachable", "no network");

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.IsFalse(vm.IsConnected);
        StringAssert.Contains(vm.ErrorText, "nije dostupan");
    }

    // ---- only one call at a time, and the month on screen is the month acted on -----------------------------------------------------

    [TestMethod]
    public async Task ACallThatIsStillRunning_IsNotOverlapped()
    {
        var (vm, client, _, _, _) = Create();
        await vm.LoadCommand.ExecuteAsync(null);

        var release = new TaskCompletionSource<SheetDto>();
        client.GetSheet = (_, _) => release.Task;
        var first = vm.LoadCommand.ExecuteAsync(null);
        Assert.IsTrue(vm.IsBusy);

        var callsBefore = client.Calls.Count;
        await vm.LoadCommand.ExecuteAsync(null);
        await vm.NextMonthCommand.ExecuteAsync(null);
        Assert.AreEqual(callsBefore, client.Calls.Count, "nothing was sent while the first call runs");
        Assert.AreEqual(9, vm.Month, "and the month did not move");

        release.SetResult(Sheets.Make(2026, 9));
        await first;
        Assert.IsFalse(vm.IsBusy);
    }

    [TestMethod]
    public async Task AMonthThatCouldNotBeLoaded_IsNotTheMonthThatIsActedOn()
    {
        var (vm, client, dialogs, _, _) = Create();
        await vm.LoadCommand.ExecuteAsync(null);

        client.GetSheet = (y, m) => m == 10 ? throw new ApiException(HttpStatusCode.ServiceUnavailable, "unreachable", "no network") : Task.FromResult(Sheets.Make(y, m));
        await vm.NextMonthCommand.ExecuteAsync(null);

        Assert.AreEqual(9, vm.Month, "the screen still shows September");
        Assert.AreEqual("Septembar 2026.", vm.MonthTitle);

    }

    // ---- submitting and deciding ------------------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Submitting_AsksFirst_AndDoesNothingWhenTheAnswerIsNo()
    {
        var (vm, client, dialogs, _, _) = Create();
        client.GetSheet = (y, m) => Task.FromResult(Sheets.Make(y, m, canSubmit: true));
        await vm.LoadCommand.ExecuteAsync(null);

        dialogs.Answer = false;
        await vm.SubmitCommand.ExecuteAsync(null);
        Assert.IsFalse(client.Calls.Any(c => c.StartsWith("submit")));

        dialogs.Answer = true;
        await vm.SubmitCommand.ExecuteAsync(null);
        CollectionAssert.Contains(client.Calls, "submit 2026-9");
        Assert.AreEqual("Predano na odobravanje", vm.StatusText);
    }

    [TestMethod]
    public async Task AReturnNeedsAMessage_AndNothingIsSentWhenItIsCancelled()
    {
        var (vm, client, dialogs, _, _) = Create();
        await vm.LoadCommand.ExecuteAsync(null);
        vm.OpenPendingCommand.Execute(new PendingRow(new PendingApprovalDto("Z-004", "Dino", 2026, 9, 1, 0, DateTime.UtcNow, 0, 0)));
        await Task.Delay(50);
        Assert.IsTrue(vm.IsReviewing);
        Assert.IsTrue(vm.CanAct);

        dialogs.Prompted = null;
        await vm.ReturnCommand.ExecuteAsync(null);
        Assert.IsFalse(client.Calls.Any(c => c.StartsWith("return")));

        dialogs.Prompted = "Ispravi sate";
        await vm.ReturnCommand.ExecuteAsync(null);
        CollectionAssert.Contains(client.Calls, "return Z-004 2026-9: Ispravi sate");
        Assert.AreEqual("Šihtarica je vraćena zaposleniku.", vm.InfoText);
    }

    [TestMethod]
    public async Task ApprovingWhileAnotherCallRuns_IsNotReportedAsDone()
    {
        var (vm, client, dialogs, _, _) = Create();
        await vm.LoadCommand.ExecuteAsync(null);
        vm.OpenPendingCommand.Execute(new PendingRow(new PendingApprovalDto("Z-004", "Dino", 2026, 9, 1, 0, DateTime.UtcNow, 0, 0)));
        await Task.Delay(50);

        var release = new TaskCompletionSource<SheetDto>();
        client.GetReview = (_, _, _) => release.Task;
        var running = vm.LoadCommand.ExecuteAsync(null);
        Assert.IsTrue(vm.IsBusy);

        await vm.ApproveCommand.ExecuteAsync(null);
        Assert.IsFalse(client.Calls.Any(c => c.StartsWith("approve")), "the server was not asked");
        Assert.AreEqual("", vm.InfoText, "so nothing is said to have happened");

        release.SetResult(Sheets.Make(2026, 9, SheetStatus.Submitted, editable: false, owner: "Z-004", canAct: true));
        await running;
    }

    [TestMethod]
    public async Task APersonWithNobodyAbove_ApprovesTheirOwnMonthFromTheirOwnScreen()
    {
        var (vm, client, dialogs, _, _) = Create();
        client.GetSheet = (y, m) => Task.FromResult(Sheets.Make(y, m, SheetStatus.Submitted, editable: false, canAct: true));
        await vm.LoadCommand.ExecuteAsync(null);

        Assert.IsTrue(vm.CanApproveOwn);
        await vm.ApproveCommand.ExecuteAsync(null);

        CollectionAssert.Contains(client.Calls, "approve Z-001 2026-9");
        Assert.AreEqual("Odobreno.", vm.InfoText);
        StringAssert.Contains(dialogs.Asked.Last(), "svoju šihtaricu");
    }

    // ---- entries ----------------------------------------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task WhatTheEntryDialogReturns_IsSentToTheServer_AndACancelledDialogSendsNothing()
    {
        var (vm, client, dialogs, _, _) = Create();
        await vm.LoadCommand.ExecuteAsync(null);

        dialogs.Entry = null;
        await vm.AddEntryCommand.ExecuteAsync(null);
        Assert.IsFalse(client.Calls.Any(c => c.StartsWith("entry")));

        dialogs.Entry = new EntryResult(new SetEntryRequest(1010, 450), new DateOnly(2026, 9, 7), null, null, null);
        await vm.AddEntryCommand.ExecuteAsync(null);
        CollectionAssert.Contains(client.Calls, "entry 2026-09-07 1010 450");

        dialogs.Entry = new EntryResult(null, null, null, new OvertimeRequest(new DateOnly(2026, 9, 5), new TimeOnly(8, 30), new TimeOnly(11, 0), "x"), 42);
        await vm.AddEntryCommand.ExecuteAsync(null);
        CollectionAssert.Contains(client.Calls, "overtime update 42");
    }

    [TestMethod]
    public async Task ALockedMonth_CannotBeEdited()
    {
        var (vm, client, dialogs, _, _) = Create();
        client.GetSheet = (y, m) => Task.FromResult(Sheets.Make(y, m, SheetStatus.Submitted, editable: false));
        await vm.LoadCommand.ExecuteAsync(null);

        await vm.AddEntryCommand.ExecuteAsync(null);

        Assert.AreEqual(0, dialogs.Asked.Count, "no dialog is opened");
        Assert.IsFalse(vm.IsEditable);
    }

    // ---- printing and leaving --------------------------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task APrintout_ReportsWhereItWentOrWhatWentWrong_AndNothingWhenCancelled()
    {
        var (vm, _, _, saver, _) = Create();
        await vm.LoadCommand.ExecuteAsync(null);

        await vm.PrintCommand.ExecuteAsync("xlsx");
        Assert.AreEqual("Sačuvano: C:\\out\\a.xlsx", vm.InfoText);

        saver.Result = new FileSaveResult(null, "Datoteka nije sačuvana: disk je pun");
        await vm.PrintCommand.ExecuteAsync("pdf");
        Assert.AreEqual("Datoteka nije sačuvana: disk je pun", vm.ErrorText);
        Assert.AreEqual("", vm.InfoText);

        saver.Result = FileSaveResult.Cancelled;
        await vm.PrintTeamCommand.ExecuteAsync("pdf");
        Assert.AreEqual("", vm.ErrorText);
        Assert.AreEqual("", vm.InfoText);
        Assert.IsFalse(vm.IsBusy);
    }

    // ---- the team -----------------------------------------------------------------------------------------------------------------

    private static TeamMemberDto Member(string code, string name, SheetStatus status, int depth = 0, string? parent = null, bool canRemind = true, string? waiting = null) =>
        new(code, name, parent, depth, status, waiting, canRemind, null);

    [TestMethod]
    public async Task TheTeamTab_ShowsTheTreeWithWhoHasNotSubmitted()
    {
        var (vm, client, _, _, _) = Create();
        client.IsApprover = true;
        client.Team = new()
        {
            Member("Z-002", "Benjamin Bašić", SheetStatus.Submitted, canRemind: false, waiting: "Emir Ferhatbegović"),
            Member("Z-001", "Amra Adžamija", SheetStatus.Draft, depth: 1, parent: "Z-002"),
            Member("Z-004", "Damir Dizdarević", SheetStatus.Approved, depth: 1, parent: "Z-002", canRemind: false)
        };
        await vm.LoadCommand.ExecuteAsync(null);

        await vm.ShowTeamCommand.ExecuteAsync(null);

        Assert.IsTrue(vm.InTeam);
        Assert.IsFalse(vm.InMine);
        Assert.IsTrue(vm.ShowTeamToolbar);
        Assert.IsFalse(vm.ShowMineToolbar, "the toolbar of the own month is replaced");
        CollectionAssert.AreEqual(new[] { "Benjamin Bašić", "Amra Adžamija", "Damir Dizdarević" }, vm.Team.Rows.Select(r => r.Name).ToArray());
        Assert.AreEqual("Predano, čeka: Emir Ferhatbegović", vm.Team.Rows[0].StatusText);
        Assert.AreEqual("Nije predano", vm.Team.Rows[1].StatusText);
        Assert.IsTrue(vm.Team.Rows[1].CanRemind);
        Assert.IsFalse(vm.Team.Rows[2].CanRemind, "approved: nothing to remind");
        Assert.AreEqual(30, vm.Team.Rows[1].Indent.Left, "one step of the tree for the level below");
        StringAssert.StartsWith(vm.Team.SummaryText, "Nisu predali: 1 od 3");
        Assert.IsTrue(vm.Team.HasPeopleToRemind);

        await vm.ShowMineCommand.ExecuteAsync(null);
        Assert.IsFalse(vm.InTeam);
        Assert.IsTrue(vm.ShowMineToolbar);
    }

    [TestMethod]
    public async Task ChangingTheMonthInTheTeamTab_ReadsTheTeamOfThatMonth_NotTheOwnSheet()
    {
        var (vm, client, _, _, _) = Create();
        client.IsApprover = true;
        client.Team = new() { Member("Z-001", "Amra Adžamija", SheetStatus.Draft) };
        await vm.LoadCommand.ExecuteAsync(null);
        await vm.ShowTeamCommand.ExecuteAsync(null);
        client.Calls.Clear();

        await vm.NextMonthCommand.ExecuteAsync(null);

        CollectionAssert.AreEqual(new[] { "team-status 2026-10" }, client.Calls);
        Assert.AreEqual(10, vm.Month);
    }

    [TestMethod]
    public async Task Reminding_AsksTheServerForThatPerson_AndSaysSo()
    {
        var (vm, client, _, _, _) = Create();
        client.IsApprover = true;
        client.Team = new() { Member("Z-001", "Amra Adžamija", SheetStatus.Draft), Member("Z-004", "Damir Dizdarević", SheetStatus.Draft) };
        await vm.LoadCommand.ExecuteAsync(null);
        await vm.ShowTeamCommand.ExecuteAsync(null);

        await vm.RemindCommand.ExecuteAsync(vm.Team.Rows[0]);

        CollectionAssert.Contains(client.Calls, "remind Z-001 2026-9");
        Assert.AreEqual("Podsjetnik je poslan: Amra Adžamija.", vm.InfoText);
        Assert.IsTrue(vm.Team.Rows[0].HasReminder, "the row shows that it was sent");
        Assert.IsFalse(vm.Team.Rows[1].HasReminder);
    }

    [TestMethod]
    public async Task AReminderThatWasRefused_IsExplainedInBosnian()
    {
        var (vm, client, _, _, _) = Create();
        client.IsApprover = true;
        client.Team = new() { Member("Z-001", "Amra Adžamija", SheetStatus.Draft) };
        client.RemindRefusals["Z-001"] = "already-reminded";
        await vm.LoadCommand.ExecuteAsync(null);
        await vm.ShowTeamCommand.ExecuteAsync(null);

        await vm.RemindCommand.ExecuteAsync(vm.Team.Rows[0]);

        Assert.AreEqual("Podsjetnik za Amra Adžamija je već poslan. Pokušajte kasnije.", vm.ErrorText);
        Assert.IsFalse(vm.Team.Rows[0].HasReminder);
        Assert.IsFalse(vm.IsBusy);
    }

    [TestMethod]
    public async Task RemindingEverybody_SkipsThoseWhoCannotBeReminded_AndThoseAlreadyReminded()
    {
        var (vm, client, _, _, _) = Create();
        client.IsApprover = true;
        client.Team = new()
        {
            Member("Z-001", "Amra Adžamija", SheetStatus.Draft),
            Member("Z-002", "Benjamin Bašić", SheetStatus.Approved, canRemind: false),
            Member("Z-004", "Damir Dizdarević", SheetStatus.Returned),
            Member("Z-005", "Dino Kadrović", SheetStatus.Draft)
        };
        client.RemindRefusals["Z-004"] = "already-reminded";
        await vm.LoadCommand.ExecuteAsync(null);
        await vm.ShowTeamCommand.ExecuteAsync(null);

        await vm.RemindAllCommand.ExecuteAsync(null);

        CollectionAssert.AreEqual(new[] { "remind Z-001 2026-9", "remind Z-004 2026-9", "remind Z-005 2026-9" }, client.Calls.Where(c => c.StartsWith("remind ")).ToArray());
        Assert.AreEqual("Podsjetnik je poslan: 2.", vm.InfoText);
        Assert.AreEqual("", vm.ErrorText);
    }

    [TestMethod]
    public async Task ANobodyBelow_GivesAnEmptyTreeNotAnError()
    {
        var (vm, client, _, _, _) = Create();
        client.IsApprover = true;
        await vm.LoadCommand.ExecuteAsync(null);

        await vm.ShowTeamCommand.ExecuteAsync(null);

        Assert.AreEqual("", vm.ErrorText);
        Assert.IsTrue(vm.Team.IsEmpty);
        Assert.IsFalse(vm.Team.HasPeopleToRemind);
    }

    [TestMethod]
    public async Task TheSummaryMessage_IsShownInsteadOfTheTree_AndCopiedAsItIs()
    {
        var (vm, client, _, _, shell) = Create();
        client.IsApprover = true;
        client.Team = new() { Member("Z-001", "Amra Adžamija", SheetStatus.Draft) };
        await vm.LoadCommand.ExecuteAsync(null);
        await vm.ShowTeamCommand.ExecuteAsync(null);

        await vm.ShowSummaryCommand.ExecuteAsync(null);

        Assert.IsTrue(vm.Team.ShowSummary);
        Assert.IsFalse(vm.Team.ShowTree);
        Assert.AreEqual("Šihtarice za Septembar 2026", vm.Team.SummaryBody);
        Assert.AreEqual("Još nisu odobrene šihtarice: Amra Adžamija.", vm.Team.SummaryNote, "the supervisor sees it before sending");

        await vm.CopySummaryCommand.ExecuteAsync(null);
        CollectionAssert.AreEqual(new[] { "Šihtarice za Septembar 2026" }, shell.Copied);
        Assert.AreEqual("Kopirano. Zalijepite u mail.", vm.InfoText);

        client.Calls.Clear();
        await vm.NextMonthCommand.ExecuteAsync(null);
        CollectionAssert.Contains(client.Calls, "summary-text 2026-10", "the message follows the month");

        vm.ShowTreeCommand.Execute(null);
        Assert.IsTrue(vm.Team.ShowTree);
    }

    [TestMethod]
    public async Task TheFileExport_IsOfferedOnlyWhenTheServerHasItSwitchedOn()
    {
        var (vm, client, _, _, _) = Create();
        await vm.LoadCommand.ExecuteAsync(null);
        Assert.IsFalse(vm.CanExport, "the default of the product");

        var (on, onClient, _, _, _) = Create();
        onClient.SheetExport = true;
        await on.LoadCommand.ExecuteAsync(null);
        Assert.IsTrue(on.CanExport);
    }

    [TestMethod]
    public async Task TheSummary_IsSavedLikeAnyPrintout()
    {
        var (vm, client, _, saver, _) = Create();
        client.IsApprover = true;
        await vm.LoadCommand.ExecuteAsync(null);

        await vm.PrintSummaryCommand.ExecuteAsync("xlsx");

        Assert.AreEqual("Sačuvano: C:\\out\\a.xlsx", vm.InfoText);
    }

    [TestMethod]
    public void ANudgeIsWordedWithTheNameOfWhoAsked()
    {
        var item = new ReminderDto(ReminderType.Nudged, 2026, 9, 1, new DateOnly(2026, 10, 5), "Benjamin Bašić", DateTime.UtcNow);
        var (title, message) = EmployeeFormat.Reminder(item);
        StringAssert.Contains(title, "Benjamin Bašić");
        StringAssert.Contains(message, "Septembar 2026.");
        StringAssert.Contains(message, "05.10.2026.");

        var first = DailyTaskManager.Avalonia.Background.EmployeeReminders.Signature(new[] { item });
        var later = DailyTaskManager.Avalonia.Background.EmployeeReminders.Signature(new[] { item with { SentUtc = item.SentUtc!.Value.AddHours(5) } });
        Assert.AreNotEqual(first, later, "a new request is a new notification, even for the same month");
    }

    [TestMethod]
    public async Task SigningOut_LeavesThroughTheShell()
    {
        var (vm, _, _, _, shell) = Create();
        await vm.LoadCommand.ExecuteAsync(null);
        vm.LogoutCommand.Execute(null);
        Assert.AreEqual(1, shell.SignedOut);
    }
}
