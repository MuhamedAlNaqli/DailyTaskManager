using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Company.Api.Client;
using Company.Api.Contracts;
using DailyTaskManager.Avalonia.Services;
using Microsoft.Extensions.Logging;
using Timesheet.Domain;

namespace DailyTaskManager.Avalonia.EmployeeMode;

/// <summary>One day of the month as a row of the list</summary>
public sealed class EmployeeDayRow
{
    public EmployeeDayRow(DayDto day, IReadOnlyList<IssueDto> issues)
    {
        Day = day;
        Date = day.Date;
        DateText = EmployeeFormat.ShortDate(day.Date);
        WeekdayText = EmployeeFormat.Weekday(day.Date);
        TypeText = day.HolidayName is { Length: > 0 } ? $"{EmployeeFormat.DayType(day.Type)}: {day.HolidayName}" : EmployeeFormat.DayType(day.Type);
        Stripe = day.Type switch
        {
            "Weekend" => Brush.Parse("#C00000"),
            "Holiday" => Brush.Parse("#EA580C"),
            _ => Brushes.Transparent
        };
        IsWorkingDay = day.Type == "Working";
        EntriesText = string.Join("\n", day.Entries.Select(EmployeeFormat.Entry));
        OvertimeText = string.Join("\n", day.OvertimeAllocations.Select(a => $"{a.Code} {TimeCodes.Get(a.Code).NameBs}: {EmployeeFormat.Hours(a.Minutes)}"));
        var mine = issues.Where(i => i.Date == day.Date).ToList();
        IssueCount = mine.Count;
        HasBlocking = mine.Any(i => i.Severity == "Blocking");
        IssueTooltip = string.Join("\n", mine.Select(i => $"{i.RuleId}: {i.Message}"));
    }

    public DayDto Day { get; }
    public DateOnly Date { get; }
    public string DateText { get; }
    public string WeekdayText { get; }
    public string TypeText { get; }
    public IBrush Stripe { get; }
    public bool IsWorkingDay { get; }
    public string EntriesText { get; }
    public string OvertimeText { get; }
    public int IssueCount { get; }
    public bool HasIssues => IssueCount > 0;
    public bool HasBlocking { get; }
    public string IssueTooltip { get; }
    public bool HasEntries => Day.Entries.Count > 0;
}

public sealed class EmployeeOvertimeRow
{
    public EmployeeOvertimeRow(OvertimeDto record)
    {
        Record = record;
        Title = $"{EmployeeFormat.ShortDate(record.Date)} {record.Start:HH\\:mm}–{record.End:HH\\:mm} ({EmployeeFormat.Hours(record.TotalMinutes)})";
        Detail = string.Join(", ", record.Allocations.Select(a => $"{a.Code}: {EmployeeFormat.Hours(a.Minutes)}"));
        Description = record.Description;
        HasConflict = record.HasRegularWindowConflict;
    }

    public OvertimeDto Record { get; }
    public string Title { get; }
    public string Detail { get; }
    public string Description { get; }
    public bool HasConflict { get; }
}

public sealed class EmployeeIssueRow
{
    public EmployeeIssueRow(IssueDto issue)
    {
        Issue = issue;
        Title = issue.RuleId + (issue.Severity == "Blocking" ? " · zaustavlja predaju" : " · upozorenje");
        Message = issue.Message;
        IsBlocking = issue.Severity == "Blocking";
    }

    public IssueDto Issue { get; }
    public string Title { get; }
    public string Message { get; }
    public bool IsBlocking { get; }
}

/// <summary>A submitted timesheet waiting for the signed-in approver</summary>
public sealed class PendingRow
{
    public PendingRow(PendingApprovalDto item)
    {
        Item = item;
        Title = item.EmployeeName;
        Detail = $"{EmployeeFormat.Month(item.Year, item.Month)} · predano {item.SubmittedUtc.ToLocalTime():dd.MM.yyyy. HH:mm}";
        Warning = item.BlockingCount > 0
            ? $"{item.BlockingCount} primjedbi koje zaustavljaju predaju"
            : item.WarningCount > 0 ? $"{item.WarningCount} upozorenja" : "";
    }

    public PendingApprovalDto Item { get; }
    public string Title { get; }
    public string Detail { get; }
    public string Warning { get; }
    public bool HasWarning => Warning.Length > 0;
}

/// <summary>A message with which a timesheet was returned, and the answer to it</summary>
public sealed class MessageRow
{
    public MessageRow(MessageDto message, bool canAnswer)
    {
        Message = message;
        Header = $"{message.FromName} (predaja {message.Revision})";
        Text = message.Text;
        AnswerText = string.IsNullOrWhiteSpace(message.Answer) ? "" : "Odgovor: " + message.Answer;
        CanAnswer = canAnswer && string.IsNullOrWhiteSpace(message.Answer);
    }

    public MessageDto Message { get; }
    public string Header { get; }
    public string Text { get; }
    public string AnswerText { get; }
    public bool HasAnswer => AnswerText.Length > 0;
    public bool CanAnswer { get; }
}

public sealed class HistoryRow
{
    public HistoryRow(HistoryDto item)
    {
        var onBehalf = item.OnBehalfOf == null ? "" : $" (u ime {item.OnBehalfOf})";
        Title = $"{item.AtUtc.ToLocalTime():dd.MM.yyyy. HH:mm} · {EmployeeFormat.HistoryKind(item.Kind)}";
        Detail = item.ActorName + onBehalf + (string.IsNullOrWhiteSpace(item.Text) ? "" : ": " + item.Text);
    }

    public string Title { get; }
    public string Detail { get; }
}

public sealed class ReminderRow
{
    public ReminderRow(ReminderDto item)
    {
        Item = item;
        (Title, Message) = EmployeeFormat.Reminder(item);
        IsLate = item.Kind is "SubmitOverdue" or "ApproveOverdue" or "FixReturned";
    }

    public ReminderDto Item { get; }
    public string Title { get; }
    public string Message { get; }
    public bool IsLate { get; }
}

/// <summary>The employee's month: the list of days, the overtime, the totals and the findings of the rules</summary>
public partial class EmployeeViewModel : ObservableObject
{
    private readonly CompanyApiClient _client;
    private readonly DialogService _dialogService;
    private readonly ILogger<EmployeeViewModel> _logger;
    private SheetDto? _sheet;

    public ObservableCollection<EmployeeDayRow> Days { get; } = new();

    public ObservableCollection<EmployeeOvertimeRow> Overtime { get; } = new();

    public ObservableCollection<EmployeeIssueRow> Issues { get; } = new();

    public ObservableCollection<string> TotalLines { get; } = new();

    [ObservableProperty] private int _year = DateTime.Today.Year;
    [ObservableProperty] private int _month = DateTime.Today.Month;
    [ObservableProperty] private string _monthTitle = "";
    [ObservableProperty] private string _whoText = "";
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private string _fundText = "";
    [ObservableProperty] private string _overtimeText = "";
    [ObservableProperty] private string _errorText = "";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isEditable;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private bool _fundOk;
    [ObservableProperty] private string _infoText = "";

    // Submission and approval
    [ObservableProperty] private bool _canSubmit;
    [ObservableProperty] private bool _canAct;
    [ObservableProperty] private string _workflowText = "";
    [ObservableProperty] private string _chainText = "";
    [ObservableProperty] private string _reviewTitle = "";
    [ObservableProperty] private int _pendingCount;

    /// <summary>The "for approval" part is shown instead of the employee's own month</summary>
    [ObservableProperty] private bool _inApprovals;

    /// <summary>Someone else's timesheet is open (read only)</summary>
    [ObservableProperty] private bool _isReviewing;

    private string? _reviewCode;

    public ObservableCollection<PendingRow> Pending { get; } = new();

    public ObservableCollection<ReminderRow> Reminders { get; } = new();

    public bool HasReminders => Reminders.Count > 0;

    public ObservableCollection<MessageRow> Messages { get; } = new();

    public ObservableCollection<HistoryRow> History { get; } = new();

    public bool HasInfo => InfoText.Length > 0;

    public bool ShowMineToolbar => IsConnected && !InApprovals;

    public bool ShowReviewToolbar => IsConnected && IsReviewing;

    public bool ShowApprovalsHint => IsConnected && InApprovals && !IsReviewing;

    public bool NoPending => Pending.Count == 0;

    public bool HasMessages => Messages.Count > 0;

    public bool HasWorkflow => WorkflowText.Length > 0;

    public string ApprovalsTabText => PendingCount > 0 ? $"Za odobravanje ({PendingCount})" : "Za odobravanje";

    partial void OnInfoTextChanged(string value) => OnPropertyChanged(nameof(HasInfo));

    partial void OnInApprovalsChanged(bool value) => RaiseModeChanged();

    partial void OnIsReviewingChanged(bool value) => RaiseModeChanged();

    partial void OnIsConnectedChanged(bool value) => RaiseModeChanged();

    partial void OnWorkflowTextChanged(string value) => OnPropertyChanged(nameof(HasWorkflow));

    partial void OnPendingCountChanged(int value) => OnPropertyChanged(nameof(ApprovalsTabText));

    private void RaiseModeChanged()
    {
        OnPropertyChanged(nameof(ShowMineToolbar));
        OnPropertyChanged(nameof(ShowReviewToolbar));
        OnPropertyChanged(nameof(ShowApprovalsHint));
    }

    public bool HasError => ErrorText.Length > 0;

    public bool HasIssues => Issues.Count > 0;

    public bool NoIssues => Issues.Count == 0 && IsConnected;

    public bool NoOvertime => Overtime.Count == 0;

    partial void OnErrorTextChanged(string value) => OnPropertyChanged(nameof(HasError));

    public EmployeeViewModel(CompanyApiClient client, DialogService dialogService, ILogger<EmployeeViewModel> logger)
    {
        _client = client;
        _dialogService = dialogService;
        _logger = logger;
        MonthTitle = EmployeeFormat.Month(Year, Month);
    }

    // ---- Loading ---------------------------------------------------------------------------------------------------

    [RelayCommand]
    public async Task LoadAsync()
    {
        await RunAsync(async () =>
        {
            if (!IsConnected)
            {
                var me = await _client.GetMeAsync();
                WhoText = $"{me.Name} · {me.Code}";
            }

            return await LoadCurrentAsync();
        }, connecting: true, refreshPending: true);
    }

    private Task<SheetDto> LoadCurrentAsync() => _reviewCode == null
        ? _client.GetSheetAsync(Year, Month)
        : _client.GetReviewAsync(_reviewCode, Year, Month);

    /// <summary>Runs one call, shows the new state of the month, and turns a refusal into a message instead of an exception</summary>
    private async Task RunAsync(Func<Task<SheetDto>> call, bool connecting = false, bool refreshPending = false)
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            ErrorText = "";
            InfoText = "";
            Show(await call());
            IsConnected = true;
            if (refreshPending) await RefreshPendingAsync();
        }
        catch (ApiException ex)
        {
            _logger.LogWarning(ex, "The company server refused or could not be reached ({Code})", ex.Code);
            ErrorText = ex.Code switch
            {
                "not-registered" => "Vaš račun nije povezan sa zaposlenikom. Obratite se administratoru.",
                "unauthorized" => "Server nije prihvatio vašu Windows prijavu. Provjerite da ste prijavljeni firmskim računom i da ste u firmskoj mreži.",
                "unreachable" or "timeout" => "Server firme nije dostupan. Provjerite mrežnu vezu (VPN). " + ex.Message,
                _ => ex.Message
            };
            if (connecting) IsConnected = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Employee mode failed");
            ErrorText = "Došlo je do greške: " + ex.Message;
            if (connecting) IsConnected = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>The list of timesheets waiting for this person. A failure here must not spoil the month that was just shown.</summary>
    private async Task RefreshPendingAsync()
    {
        try
        {
            var due = await _client.GetRemindersAsync();
            Reminders.Clear();
            foreach (var item in due.Items.OrderBy(i => EmployeeFormat.ReminderOrder(i.Kind))) Reminders.Add(new ReminderRow(item));
            OnPropertyChanged(nameof(HasReminders));

            var items = await _client.GetPendingAsync();
            Pending.Clear();
            foreach (var item in items) Pending.Add(new PendingRow(item));
            PendingCount = items.Count;
            OnPropertyChanged(nameof(NoPending));
        }
        catch (ApiException ex)
        {
            _logger.LogWarning(ex, "The list of timesheets for approval could not be read ({Code})", ex.Code);
        }
    }

    private void ClearSheet()
    {
        _sheet = null;
        Days.Clear();
        Overtime.Clear();
        Issues.Clear();
        TotalLines.Clear();
        Messages.Clear();
        History.Clear();
        StatusText = "";
        FundText = "";
        OvertimeText = "";
        WorkflowText = "";
        ChainText = "";
        ReviewTitle = "";
        IsEditable = false;
        CanSubmit = false;
        CanAct = false;
        OnPropertyChanged(nameof(HasMessages));
        OnPropertyChanged(nameof(NoOvertime));
    }

    private void Show(SheetDto sheet)
    {
        _sheet = sheet;
        Year = sheet.Year;
        Month = sheet.Month;
        MonthTitle = EmployeeFormat.Month(sheet.Year, sheet.Month);
        StatusText = EmployeeFormat.Status(sheet.Status);
        IsEditable = sheet.Editable;

        Days.Clear();
        foreach (var day in sheet.Days) Days.Add(new EmployeeDayRow(day, sheet.Issues));

        Overtime.Clear();
        foreach (var record in sheet.Overtime) Overtime.Add(new EmployeeOvertimeRow(record));

        Issues.Clear();
        foreach (var issue in sheet.Issues.OrderByDescending(i => i.Severity == "Blocking")) Issues.Add(new EmployeeIssueRow(issue));

        var summary = sheet.Summary;
        FundOk = summary.FundMinutes >= summary.ExpectedFundMinutes;
        FundText = $"Mjesečni fond: {EmployeeFormat.Hours(summary.FundMinutes)} od {EmployeeFormat.Hours(summary.ExpectedFundMinutes)}";
        OvertimeText = $"Prekovremeni rad: {EmployeeFormat.Hours(summary.OvertimeMinutes)}";

        TotalLines.Clear();
        foreach (var (code, minutes) in summary.MinutesByCode.OrderBy(p => p.Key))
            TotalLines.Add($"{code} {TimeCodes.Get(code).NameBs}: {EmployeeFormat.Hours(minutes)}");
        foreach (var (code, days) in summary.DaysByCode.Where(p => TimeCodes.Get(p.Key).IsAbsence).OrderBy(p => p.Key))
            TotalLines.Add($"{code} {TimeCodes.Get(code).NameBs}: {days} {(days == 1 ? "dan" : "dana")}");

        ShowWorkflow(sheet);

        OnPropertyChanged(nameof(HasIssues));
        OnPropertyChanged(nameof(NoIssues));
        OnPropertyChanged(nameof(NoOvertime));
    }

    private void ShowWorkflow(SheetDto sheet)
    {
        var workflow = sheet.Workflow;
        Messages.Clear();
        History.Clear();
        CanSubmit = workflow?.CanSubmit == true;
        CanAct = workflow?.CanAct == true;
        ReviewTitle = _reviewCode != null && workflow != null ? $"{workflow.OwnerName} · {MonthTitle}" : "";

        if (workflow == null)
        {
            WorkflowText = "";
            ChainText = "";
        }
        else
        {
            WorkflowText = sheet.Status switch
            {
                "Submitted" => $"Čeka odobravanje: {workflow.CurrentApproverName} (korak {workflow.Step + 1} od {workflow.Chain.Count}).",
                "Returned" => _reviewCode == null
                    ? "Vraćeno na ispravku. Odgovorite na poruke, ispravite unos i predajte ponovo."
                    : "Vraćeno zaposleniku na ispravku.",
                "Approved" => "Odobreno. Šihtarica je zaključana.",
                _ => _reviewCode == null ? "Još nije predano na odobravanje." : ""
            };
            ChainText = workflow.Chain.Count == 0 ? "" : "Lanac odobravanja: " + string.Join(" → ", workflow.Chain.Select(c => c.Name));

            var canAnswer = _reviewCode == null && sheet.Status == "Returned";
            foreach (var message in workflow.Messages) Messages.Add(new MessageRow(message, canAnswer));
            foreach (var item in workflow.History.Reverse()) History.Add(new HistoryRow(item));
        }

        OnPropertyChanged(nameof(HasMessages));
    }

    // ---- Month navigation ------------------------------------------------------------------------------------------

    [RelayCommand]
    private Task PreviousMonthAsync() => ChangeMonthAsync(-1);

    [RelayCommand]
    private Task NextMonthAsync() => ChangeMonthAsync(1);

    [RelayCommand]
    private Task ThisMonthAsync()
    {
        Year = DateTime.Today.Year;
        Month = DateTime.Today.Month;
        return LoadAsync();
    }

    private Task ChangeMonthAsync(int delta)
    {
        if (IsReviewing) return Task.CompletedTask;
        var next = new DateTime(Year, Month, 1).AddMonths(delta);
        Year = next.Year;
        Month = next.Month;
        return LoadAsync();
    }

    // ---- Editing ---------------------------------------------------------------------------------------------------

    [RelayCommand]
    private async Task FillStandardDaysAsync()
    {
        if (!IsEditable) return;
        var confirmed = await _dialogService.ShowConfirmDialogAsync("Popuni standardne dane",
            "Svaki radni dan bez unosa dobit će redovan rad od 8 sati. Praznici i vaši vjerski praznici se preskaču. Nastaviti?");
        if (confirmed) await RunAsync(() => _client.FillStandardDaysAsync(Year, Month));
    }

    [RelayCommand]
    private async Task EditDayAsync(EmployeeDayRow? row)
    {
        if (row == null || !IsEditable) return;

        var viewModel = new DayEntryViewModel(row.Date, row.Day.Entries);
        var request = await EmployeeDialogs.ShowAsync<DayEntryDialog, SetEntryRequest?>(new DayEntryDialog { DataContext = viewModel }, viewModel);
        if (request != null) await RunAsync(() => _client.SetEntryAsync(Year, Month, row.Date, request));
    }

    [RelayCommand]
    private async Task ClearDayAsync(EmployeeDayRow? row)
    {
        if (row == null || !IsEditable || !row.HasEntries) return;

        await RunAsync(async () =>
        {
            SheetDto? last = null;
            foreach (var entry in row.Day.Entries) last = await _client.ClearEntryAsync(Year, Month, row.Date, entry.Code);
            return last!;
        });
    }

    [RelayCommand]
    private async Task AddAbsenceAsync()
    {
        if (!IsEditable) return;

        var viewModel = new AbsenceViewModel(Year, Month, request => _client.PreviewAbsenceAsync(Year, Month, request));
        var request = await EmployeeDialogs.ShowAsync<AbsenceDialog, AbsenceRangeRequest?>(new AbsenceDialog { DataContext = viewModel }, viewModel);
        if (request != null) await RunAsync(() => _client.ApplyAbsenceAsync(Year, Month, request));
    }

    [RelayCommand]
    private async Task AddOvertimeAsync()
    {
        if (!IsEditable || _sheet == null) return;

        var viewModel = new OvertimeViewModel(Year, Month, DayTypeResolver(), null);
        var request = await EmployeeDialogs.ShowAsync<OvertimeDialog, OvertimeRequest?>(new OvertimeDialog { DataContext = viewModel }, viewModel);
        if (request != null) await RunAsync(() => _client.AddOvertimeAsync(Year, Month, request));
    }

    [RelayCommand]
    private async Task EditOvertimeAsync(EmployeeOvertimeRow? row)
    {
        if (row == null || !IsEditable || _sheet == null) return;

        var viewModel = new OvertimeViewModel(Year, Month, DayTypeResolver(), row.Record);
        var request = await EmployeeDialogs.ShowAsync<OvertimeDialog, OvertimeRequest?>(new OvertimeDialog { DataContext = viewModel }, viewModel);
        if (request != null) await RunAsync(() => _client.UpdateOvertimeAsync(Year, Month, row.Record.Id, request));
    }

    [RelayCommand]
    private async Task DeleteOvertimeAsync(EmployeeOvertimeRow? row)
    {
        if (row == null || !IsEditable) return;

        var confirmed = await _dialogService.ShowConfirmDialogAsync("Obriši prekovremeni rad", $"Obrisati unos {row.Title}?");
        if (confirmed) await RunAsync(() => _client.DeleteOvertimeAsync(Year, Month, row.Record.Id));
    }

    /// <summary>The type of a date for the live preview of overtime: from the month's calendar, else by weekday</summary>
    private Func<DateOnly, DayType> DayTypeResolver()
    {
        var types = _sheet!.Days.ToDictionary(d => d.Date, d => Enum.Parse<DayType>(d.Type));
        return date => types.TryGetValue(date, out var type) ? type : (WorkCalendar.IsWeekend(date) ? DayType.Weekend : DayType.Working);
    }

    // ---- Submission ------------------------------------------------------------------------------------------------

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (!CanSubmit) return;
        var confirmed = await _dialogService.ShowConfirmDialogAsync("Predaj na odobravanje",
            $"Nakon predaje je šihtarica za {MonthTitle} zaključana dok je neko od odobravatelja ne vrati. Predati?");
        if (confirmed) await RunAsync(() => _client.SubmitAsync(Year, Month));
    }

    [RelayCommand]
    private async Task AnswerAsync(MessageRow? row)
    {
        if (row == null || !row.CanAnswer) return;

        var viewModel = new TextPromptViewModel("Odgovor na poruku", "Vaš odgovor", "Pošalji odgovor", $"{row.Header}: {row.Text}");
        var answer = await EmployeeDialogs.ShowAsync<TextPromptDialog, string?>(new TextPromptDialog { DataContext = viewModel }, viewModel);
        if (answer != null) await RunAsync(() => _client.AnswerAsync(Year, Month, row.Message.Index, answer));
    }

    // ---- Approval --------------------------------------------------------------------------------------------------

    [RelayCommand]
    private async Task ShowMineAsync()
    {
        InApprovals = false;
        IsReviewing = false;
        _reviewCode = null;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ShowApprovalsAsync()
    {
        InApprovals = true;
        IsReviewing = false;
        _reviewCode = null;
        ClearSheet();
        InfoText = "";
        await RefreshPendingAsync();
    }

    [RelayCommand]
    private async Task OpenPendingAsync(PendingRow? row)
    {
        if (row == null) return;

        _reviewCode = row.Item.EmployeeCode;
        Year = row.Item.Year;
        Month = row.Item.Month;
        MonthTitle = EmployeeFormat.Month(Year, Month);
        IsReviewing = true;
        await LoadAsync();
        if (HasError) CloseReview();
    }

    [RelayCommand]
    private void CloseReview()
    {
        _reviewCode = null;
        IsReviewing = false;
        ClearSheet();
    }

    [RelayCommand]
    private async Task ApproveAsync()
    {
        if (!CanAct || _reviewCode == null) return;
        var code = _reviewCode;
        var confirmed = await _dialogService.ShowConfirmDialogAsync("Odobri šihtaricu", $"Odobriti šihtaricu: {ReviewTitle}?");
        if (!confirmed) return;

        await RunAsync(() => _client.ApproveAsync(code, Year, Month), refreshPending: true);
        if (!HasError) InfoText = "Odobreno.";
    }

    [RelayCommand]
    private async Task ReturnAsync()
    {
        if (!CanAct || _reviewCode == null) return;
        var code = _reviewCode;

        var viewModel = new TextPromptViewModel("Vrati na ispravku", "Šta treba ispraviti? (obavezno)", "Vrati zaposleniku");
        var message = await EmployeeDialogs.ShowAsync<TextPromptDialog, string?>(new TextPromptDialog { DataContext = viewModel }, viewModel);
        if (message == null) return;

        await RunAsync(() => _client.ReturnAsync(code, Year, Month, message), refreshPending: true);
        if (!HasError) InfoText = "Šihtarica je vraćena zaposleniku.";
    }

    // ---- Mode --------------------------------------------------------------------------------------------------------

    [RelayCommand]
    private void SwitchToConsultant()
    {
        AppMode.Set(Mode.Consultant);
        if (App.Current is App app && app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var old = desktop.MainWindow;
            var next = app.CreateLoginWindow();
            desktop.MainWindow = next;
            next.Show();
            old?.Close();
        }
    }
}
