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
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Localization;
using Microsoft.Extensions.Logging;
using Timesheet.Domain;

namespace DailyTaskManager.Avalonia.EmployeeMode;

/// <summary>The employee's month: the list of days, the overtime, the totals and the findings of the rules</summary>
public partial class EmployeeViewModel : ObservableObject
{
    private readonly ICompanyApiClient _client;
    private readonly IEmployeeDialogs _dialogs;
    private readonly ISheetFileSaver _files;
    private readonly IEmployeeShell _shell;
    private readonly ILogger<EmployeeViewModel> _logger;
    private SheetDto? _sheet;

    [ObservableProperty] private int _year = DateTime.Today.Year;
    [ObservableProperty] private int _month = DateTime.Today.Month;
    [ObservableProperty] private string _monthTitle = "";
    [ObservableProperty] private string _whoText = "";
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private string _errorText = "";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isEditable;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private string _infoText = "";

    /// <summary>The person has people reporting to them: the sheet of their team can be printed</summary>
    [ObservableProperty] private bool _isApprover;

    /// <summary>The month as a calendar instead of a list; remembered for the next time</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsListView))]
    private bool _isCalendarView = EmployeePreferences.LoadCalendarView();

    public bool IsListView => !IsCalendarView;

    partial void OnIsCalendarViewChanged(bool value) => EmployeePreferences.SaveCalendarView(value);

    [RelayCommand]
    private void ShowListView() => IsCalendarView = false;

    [RelayCommand]
    private void ShowCalendarView() => IsCalendarView = true;

    // Submission and approval
    [ObservableProperty] private bool _canSubmit;
    [ObservableProperty] private bool _canAct;
    [ObservableProperty] private string _reviewTitle = "";

    /// <summary>The "for approval" part is shown instead of the employee's own month</summary>
    [ObservableProperty] private bool _inApprovals;

    /// <summary>Someone else's timesheet is open (read only)</summary>
    [ObservableProperty] private bool _isReviewing;

    private string? _reviewCode;

    /// <summary>The month on screen: days, calendar, overtime, totals, findings, messages and history</summary>
    public SheetViewModel Sheet { get; } = new();

    /// <summary>The timesheets waiting for this person as an approver</summary>
    public PendingListViewModel PendingApprovals { get; } = new();

    /// <summary>What is due for this person (submit, fix, approve)</summary>
    public RemindersViewModel Reminders { get; } = new();

    public bool HasInfo => InfoText.Length > 0;

    public bool ShowMineToolbar => IsConnected && !InApprovals;

    public bool ShowMonthNav => IsConnected && !IsReviewing;

    /// <summary>A person with nobody above them decides on their own timesheet from their own screen</summary>
    public bool CanApproveOwn => CanAct && !IsReviewing && !InApprovals;

    public bool ShowApprovalsToolbar => IsConnected && InApprovals && !IsReviewing;

    public bool ShowReviewToolbar => IsConnected && IsReviewing;

    public bool ShowApprovalsHint => IsConnected && InApprovals && !IsReviewing;

    partial void OnInfoTextChanged(string value) => OnPropertyChanged(nameof(HasInfo));

    partial void OnInApprovalsChanged(bool value) => RaiseModeChanged();

    partial void OnIsReviewingChanged(bool value) => RaiseModeChanged();

    partial void OnIsConnectedChanged(bool value) => RaiseModeChanged();

    partial void OnCanActChanged(bool value) => OnPropertyChanged(nameof(CanApproveOwn));

    private void RaiseModeChanged()
    {
        OnPropertyChanged(nameof(ShowMonthNav));
        OnPropertyChanged(nameof(CanApproveOwn));
        OnPropertyChanged(nameof(ShowApprovalsToolbar));
        OnPropertyChanged(nameof(ShowMineToolbar));
        OnPropertyChanged(nameof(ShowReviewToolbar));
        OnPropertyChanged(nameof(ShowApprovalsHint));
    }

    public bool HasError => ErrorText.Length > 0;

    partial void OnErrorTextChanged(string value) => OnPropertyChanged(nameof(HasError));

    public EmployeeViewModel(ICompanyApiClient client, IEmployeeDialogs dialogs, ISheetFileSaver files, IEmployeeShell shell, ILogger<EmployeeViewModel> logger)
    {
        _client = client;
        _dialogs = dialogs;
        _files = files;
        _shell = shell;
        _logger = logger;
        MonthTitle = EmployeeFormat.Month(Year, Month);
    }

    // ---- Loading ---------------------------------------------------------------------------------------------------

    [RelayCommand]
    public Task LoadAsync() => LoadCoreAsync();

    /// <summary>Loads the month on screen; false when the server refused, was out of reach, or another call was still running</summary>
    private Task<bool> LoadCoreAsync() =>
        RunAsync(async () =>
        {
            if (!IsConnected)
            {
                var me = await _client.GetMeAsync();
                WhoText = $"{me.Name} · {me.Code}";
                IsApprover = me.IsApprover;
            }

            return await LoadCurrentAsync();
        }, connecting: true, refreshPending: true);

    private Task<SheetDto> LoadCurrentAsync() => _reviewCode == null
        ? _client.GetSheetAsync(Year, Month)
        : _client.GetReviewAsync(_reviewCode, Year, Month);

    /// <summary>Runs one call, shows the new state of the month, and turns a refusal into a message instead of an exception</summary>
    private async Task<bool> RunAsync(Func<Task<SheetDto>> call, bool connecting = false, bool refreshPending = false)
    {
        if (IsBusy) return false;
        try
        {
            IsBusy = true;
            ErrorText = "";
            InfoText = "";
            Show(await call());
            IsConnected = true;
            if (refreshPending) await RefreshPendingAsync();
            return true;
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
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Employee mode failed");
            ErrorText = "Došlo je do greške: " + ex.Message;
            if (connecting) IsConnected = false;
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>The reminders and the list of timesheets for approval. A failure here must not spoil the month that was just shown.</summary>
    private async Task RefreshPendingAsync()
    {
        try
        {
            await Reminders.RefreshAsync(_client);
            await PendingApprovals.RefreshAsync(_client);
        }
        catch (Exception ex)
        {
            // An answer that is not ours (a login page of a proxy, say) or a failure of the network: the month stays as it is
            _logger.LogWarning(ex, "The reminders and the list of timesheets for approval could not be read");
        }
    }

    private void ClearSheet()
    {
        _sheet = null;
        Sheet.Clear();
        StatusText = "";
        ReviewTitle = "";
        IsEditable = false;
        CanSubmit = false;
        CanAct = false;
    }

    private void Show(SheetDto sheet)
    {
        _sheet = sheet;
        Year = sheet.Year;
        Month = sheet.Month;
        MonthTitle = EmployeeFormat.Month(sheet.Year, sheet.Month);
        StatusText = EmployeeFormat.Status(sheet.Status);
        IsEditable = sheet.Editable;

        Sheet.Show(sheet, own: _reviewCode == null);

        var workflow = sheet.Workflow;
        CanSubmit = workflow?.CanSubmit == true;
        CanAct = workflow?.CanAct == true;
        ReviewTitle = _reviewCode != null && workflow != null ? $"{workflow.OwnerName} · {MonthTitle}" : "";
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

    private async Task ChangeMonthAsync(int delta)
    {
        if (IsReviewing || IsBusy) return;
        var (previousYear, previousMonth) = (Year, Month);
        var next = new DateTime(Year, Month, 1).AddMonths(delta);
        Year = next.Year;
        Month = next.Month;

        // In the list of timesheets for approval the month only chooses the month of the team sheet
        if (InApprovals)
        {
            MonthTitle = EmployeeFormat.Month(Year, Month);
            return;
        }

        // If the month could not be loaded, the screen still shows the old one, so that is the one that stays selected
        if (!await LoadCoreAsync())
        {
            Year = previousYear;
            Month = previousMonth;
            MonthTitle = EmployeeFormat.Month(Year, Month);
        }
    }

    // ---- Editing ---------------------------------------------------------------------------------------------------

    [RelayCommand]
    private async Task FillStandardDaysAsync()
    {
        if (!IsEditable) return;
        var confirmed = await _dialogs.ConfirmAsync("Popuni standardne dane",
            $"Svaki radni dan bez unosa u mjesecu {MonthTitle} dobit će redovan rad od 8 sati. Praznici i vaši vjerski praznici se preskaču. Nastaviti?");
        if (confirmed) await RunAsync(() => _client.FillStandardDaysAsync(Year, Month));
    }

    /// <summary>A new entry: the form changes with the code (hours, absence over several days, or overtime)</summary>
    [RelayCommand]
    private Task AddEntryAsync() => OpenEntryAsync(null, null, null);

    /// <summary>A day of the list or of the calendar: its entry, or a new entry for that day when it is empty</summary>
    [RelayCommand]
    private Task EditDayAsync(EmployeeDayRow? row)
    {
        if (row == null || !IsEditable) return Task.CompletedTask;

        var entry = row.Day.Entries.FirstOrDefault(e => TimeCodes.Get(e.Code).IsBaseEntry) ?? row.Day.Entries.FirstOrDefault();
        return OpenEntryAsync(row.Date, entry, null);
    }

    private async Task OpenEntryAsync(DateOnly? date, EntryDto? entry, OvertimeDto? overtime)
    {
        if (!IsEditable || _sheet == null) return;

        var viewModel = new EntryViewModel(Year, Month, DayTypeResolver(), request => _client.PreviewAbsenceAsync(Year, Month, request), date, entry, overtime);
        var result = await _dialogs.EntryAsync(viewModel);
        if (result == null) return;

        if (result.Overtime != null)
            await RunAsync(() => result.OvertimeId is { } id
                ? _client.UpdateOvertimeAsync(Year, Month, id, result.Overtime)
                : _client.AddOvertimeAsync(Year, Month, result.Overtime));
        else if (result.Absence != null)
            await RunAsync(() => _client.ApplyAbsenceAsync(Year, Month, result.Absence));
        else
            await RunAsync(() => _client.SetEntryAsync(Year, Month, result.DayDate!.Value, result.Day!));
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
    private Task EditOvertimeAsync(EmployeeOvertimeRow? row) => row == null ? Task.CompletedTask : OpenEntryAsync(null, null, row.Record);

    [RelayCommand]
    private async Task DeleteOvertimeAsync(EmployeeOvertimeRow? row)
    {
        if (row == null || !IsEditable) return;

        var confirmed = await _dialogs.ConfirmAsync("Obriši prekovremeni rad", $"Obrisati unos {row.Title}?");
        if (confirmed) await RunAsync(() => _client.DeleteOvertimeAsync(Year, Month, row.Record.Id));
    }

    /// <summary>The type of a date for the live preview of overtime: from the month's calendar, else by weekday</summary>
    private Func<DateOnly, DayType> DayTypeResolver()
    {
        var types = _sheet!.Days.ToDictionary(d => d.Date, d => d.Type switch { DayKind.Weekend => DayType.Weekend, DayKind.Holiday => DayType.Holiday, _ => DayType.Working });
        return date => types.TryGetValue(date, out var type) ? type : (WorkCalendar.IsWeekend(date) ? DayType.Weekend : DayType.Working);
    }

    // ---- Printed sheet ---------------------------------------------------------------------------------------------

    /// <summary>Prints the month on screen (the own sheet, or the one opened for approval) in the layout of the current Excel sheet</summary>
    [RelayCommand]
    private Task PrintAsync(string? format) => SaveFileAsync(format, f => _reviewCode == null
        ? _client.GetPrintAsync(Year, Month, f)
        : _client.GetReviewPrintAsync(_reviewCode, Year, Month, f));

    /// <summary>The month of everybody who reports to the signed-in person, on one sheet, whatever the status of each</summary>
    [RelayCommand]
    private Task PrintTeamAsync(string? format) => SaveFileAsync(format, f => _client.GetTeamPrintAsync(Year, Month, f));

    private async Task SaveFileAsync(string? format, Func<string, Task<DownloadedFile>> download)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ErrorText = "";
            InfoText = "";
            var result = await _files.SaveAsync(format, download);
            if (result.Error != null) ErrorText = result.Error;
            else if (result.Path != null) InfoText = $"Sačuvano: {result.Path}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ---- Submission ------------------------------------------------------------------------------------------------

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (!CanSubmit) return;
        var confirmed = await _dialogs.ConfirmAsync("Predaj na odobravanje",
            $"Nakon predaje je šihtarica za {MonthTitle} zaključana dok je neko od odobravatelja ne vrati. Predati?");
        if (confirmed) await RunAsync(() => _client.SubmitAsync(Year, Month), refreshPending: true);
    }

    [RelayCommand]
    private async Task AnswerAsync(MessageRow? row)
    {
        if (row == null || !row.CanAnswer) return;

        var viewModel = new TextPromptViewModel("Odgovor na poruku", "Vaš odgovor", "Pošalji odgovor", $"{row.Header}: {row.Text}");
        var answer = await _dialogs.PromptAsync(viewModel);
        if (answer != null) await RunAsync(() => _client.AnswerAsync(Year, Month, row.Message.Index, answer), refreshPending: true);
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
        if (!await LoadCoreAsync()) CloseReview();
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
        var code = _reviewCode ?? _sheet?.Workflow?.OwnerCode;
        if (!CanAct || code == null) return;
        var what = _reviewCode == null ? $"svoju šihtaricu za {MonthTitle}" : $"šihtaricu: {ReviewTitle}";
        var confirmed = await _dialogs.ConfirmAsync("Odobri šihtaricu", $"Odobriti {what}?");
        if (!confirmed) return;

        if (await RunAsync(() => _client.ApproveAsync(code, Year, Month), refreshPending: true)) InfoText = "Odobreno.";
    }

    [RelayCommand]
    private async Task ReturnAsync()
    {
        if (!CanAct || _reviewCode == null) return;
        var code = _reviewCode;

        var viewModel = new TextPromptViewModel("Vrati na ispravku", "Šta treba ispraviti? (obavezno)", "Vrati zaposleniku");
        var message = await _dialogs.PromptAsync(viewModel);
        if (message == null) return;

        if (await RunAsync(() => _client.ReturnAsync(code, Year, Month, message), refreshPending: true)) InfoText = "Šihtarica je vraćena zaposleniku.";
    }

    // ---- Sign-out ----------------------------------------------------------------------------------------------------

    /// <summary>Back to the sign-in screen. The next start shows it too, until the user signs in again with "remember me".</summary>
    [RelayCommand]
    private void Logout() => _shell.SignOut();
}
