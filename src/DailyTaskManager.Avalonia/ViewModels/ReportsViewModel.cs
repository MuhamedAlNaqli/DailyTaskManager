using DailyTaskManager.Core.Localization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DailyTaskManager.Avalonia.Services;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Avalonia.ViewModels;

/// <summary>A calendar month that can be reported on</summary>
public class MonthChoice
{
    public MonthChoice(DateTime firstDay)
    {
        From = firstDay;
        To = firstDay.AddMonths(1).AddDays(-1);
        Name = firstDay.ToString("MMMM yyyy", Loc.Culture);
    }

    public DateTime From { get; }

    public DateTime To { get; }

    public string Name { get; }

    public override string ToString() => Name;
}

public partial class ReportsViewModel : ObservableObject, IDisposable
{
    private const string RateKey = "Invoice.HourlyRate";
    private const string CurrencyKey = "Invoice.Currency";
    private const string VatKey = "Invoice.VatPercent";
    private const string NoteKey = "Invoice.PaymentNote";
    private const string NextNumberKey = "Invoice.NextNumber";

    private readonly IDatabaseService _databaseService;
    private readonly IExportService _exportService;
    private readonly ISettingsService _settingsService;
    private readonly IPlatformService _platformService;
    private readonly IAuthenticationService _authService;
    private readonly DialogService _dialogService;
    private readonly ILogger<ReportsViewModel> _logger;
    private bool _suspendReload = true;
    private int _loadVersion;
    private int _nextInvoiceNumber = 1;

    public ObservableCollection<MonthChoice> Months { get; } = new();

    public ObservableCollection<ClientChoice> ClientOptions { get; } = new() { ClientChoice.All, ClientChoice.Unassigned };

    [ObservableProperty]
    private MonthChoice? _selectedMonth;

    [ObservableProperty]
    private ClientChoice? _selectedClient = ClientChoice.All;

    [ObservableProperty]
    private int _entryCount;

    [ObservableProperty]
    private decimal _totalHours;

    [ObservableProperty]
    private int _daysWorked;

    [ObservableProperty]
    private string _summaryText = "";

    [ObservableProperty]
    private bool _isBusy;

    // Invoice builder
    [ObservableProperty]
    private string _invoiceNumber = "";

    [ObservableProperty]
    private DateTimeOffset? _issueDate = DateTimeOffset.Now.Date;

    [ObservableProperty]
    private decimal _hourlyRate;

    [ObservableProperty]
    private string _currency = "BAM";

    [ObservableProperty]
    private decimal _vatPercent;

    [ObservableProperty]
    private string _paymentNote = "";

    [ObservableProperty]
    private string _netText = "0,00";

    [ObservableProperty]
    private string _vatText = "0,00";

    [ObservableProperty]
    private string _grossText = "0,00";

    public ReportsViewModel(
        IDatabaseService databaseService,
        IExportService exportService,
        ISettingsService settingsService,
        IPlatformService platformService,
        IAuthenticationService authService,
        DialogService dialogService,
        ILogger<ReportsViewModel> logger)
    {
        _databaseService = databaseService;
        _exportService = exportService;
        _settingsService = settingsService;
        _platformService = platformService;
        _authService = authService;
        _dialogService = dialogService;
        _logger = logger;

        var thisMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        for (var i = 0; i < 24; i++) Months.Add(new MonthChoice(thisMonth.AddMonths(-i)));
        SelectedMonth = Months[0];

        _suspendReload = false;
        _ = InitializeAsync();
    }

    public void Dispose()
    {
        Interlocked.Increment(ref _loadVersion);
    }

    private async Task InitializeAsync()
    {
        try
        {
            await LoadInvoiceSettingsAsync();
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing reports");
        }
    }

    private async Task LoadInvoiceSettingsAsync()
    {
        var rate = await _settingsService.GetSettingAsync<string>(RateKey, "0");
        var vat = await _settingsService.GetSettingAsync<string>(VatKey, "0");
        var next = await _settingsService.GetSettingAsync<string>(NextNumberKey, "1");

        HourlyRate = ParseDecimal(rate);
        VatPercent = ParseDecimal(vat);
        Currency = await _settingsService.GetSettingAsync<string>(CurrencyKey, "BAM") ?? "BAM";
        PaymentNote = await _settingsService.GetSettingAsync<string>(NoteKey, "") ?? "";
        _nextInvoiceNumber = int.TryParse(next, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n > 0 ? n : 1;
        InvoiceNumber = ComposeInvoiceNumber(_nextInvoiceNumber);
        UpdateAmounts();
    }

    private static decimal ParseDecimal(string? text) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : 0m;

    private static string ComposeInvoiceNumber(int number) => $"{number:000}/{DateTime.Now:yy}";

    partial void OnSelectedMonthChanged(MonthChoice? value)
    {
        if (!_suspendReload) _ = RefreshAsync();
    }

    partial void OnSelectedClientChanged(ClientChoice? value)
    {
        if (!_suspendReload) _ = RefreshAsync();
    }

    partial void OnHourlyRateChanged(decimal value) => UpdateAmounts();

    partial void OnVatPercentChanged(decimal value) => UpdateAmounts();

    partial void OnTotalHoursChanged(decimal value) => UpdateAmounts();

    private static string Money(decimal value)
    {
        CultureInfo culture;
        try { culture = new CultureInfo("de-DE"); } catch (CultureNotFoundException) { culture = CultureInfo.InvariantCulture; }
        return value.ToString("N2", culture);
    }

    private InvoiceData BuildInvoice() => new()
    {
        InvoiceNumber = InvoiceNumber.Trim(),
        IssueDate = (IssueDate ?? DateTimeOffset.Now).Date,
        PeriodFrom = SelectedMonth?.From ?? DateTime.Today,
        PeriodTo = SelectedMonth?.To ?? DateTime.Today,
        Hours = TotalHours,
        HourlyRate = HourlyRate,
        Currency = string.IsNullOrWhiteSpace(Currency) ? "BAM" : Currency.Trim(),
        VatPercent = VatPercent,
        PaymentNote = PaymentNote
    };

    private void UpdateAmounts()
    {
        var invoice = BuildInvoice();
        NetText = Money(invoice.NetAmount);
        VatText = Money(invoice.VatAmount);
        GrossText = Money(invoice.GrossAmount);
    }

    private TaskQuery CurrentQuery => new(null, SelectedMonth?.From, SelectedMonth?.To, SelectedClient?.Id);

    private async Task RefreshAsync()
    {
        var version = Interlocked.Increment(ref _loadVersion);
        try
        {
            if (!_authService.IsAuthenticated) return;

            var daysTask = _databaseService.GetDailyHoursAsync(CurrentQuery);
            var clientsTask = _databaseService.GetClientsAsync();
            await Task.WhenAll(daysTask, clientsTask);
            if (version != _loadVersion) return;

            RebuildClientOptions(clientsTask.Result);

            var days = daysTask.Result;
            EntryCount = days.Sum(d => d.Entries);
            TotalHours = days.Sum(d => d.Hours);
            DaysWorked = days.Count(d => d.Hours > 0);
            SummaryText = EntryCount == 0
                ? "No entries for this selection"
                : (EntryCount == 1 ? Loc.T("1 entry") : Loc.F("{0} entries", EntryCount)) + ", " +
                  (DaysWorked == 1 ? Loc.F("{0}h over 1 day", $"{TotalHours:0.0}") : Loc.F("{0}h over {1} days", $"{TotalHours:0.0}", DaysWorked));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading report summary");
        }
    }

    private void RebuildClientOptions(List<Client> clients)
    {
        var existing = ClientOptions.Skip(2).Select(c => c.Id).ToList();
        if (existing.SequenceEqual(clients.Select(c => (int?)c.Id))) return;

        var selectedId = SelectedClient?.Id;
        _suspendReload = true;
        try
        {
            while (ClientOptions.Count > 2) ClientOptions.RemoveAt(2);
            foreach (var client in clients) ClientOptions.Add(ClientChoice.From(client));
            SelectedClient = ClientOptions.FirstOrDefault(c => c.Id == selectedId) ?? ClientChoice.All;
        }
        finally
        {
            _suspendReload = false;
        }
    }

    private string FileSuffix()
    {
        var month = SelectedMonth?.From ?? DateTime.Today;
        var suffix = $"{month:MMyy}";
        if (SelectedClient?.Id > 0)
        {
            var safe = string.Concat(SelectedClient.Name.Where(c => !Path.GetInvalidFileNameChars().Contains(c))).Replace(' ', '_');
            suffix += $"_{safe}";
        }

        return suffix;
    }

    private async Task<List<DailyTask>?> LoadEntriesOrWarnAsync()
    {
        var tasks = await _databaseService.GetTasksAsync(CurrentQuery);
        if (tasks.Count == 0)
        {
            await _dialogService.ShowInfoDialogAsync("No Data", "There are no entries for the selected month and client.");
            return null;
        }

        return tasks;
    }

    /// <summary>Runs an export and offers to open the result; returns true when a file was created</summary>
    private async Task<bool> RunExportAsync(string what, Func<Task<string>> export)
    {
        if (IsBusy) return false;

        try
        {
            IsBusy = true;
            var path = await export();
            _logger.LogInformation("{What} created: {Path}", what, path);

            var open = await _dialogService.ShowConfirmDialogAsync(what + " created", $"Saved to:\n{path}\n\nOpen it now?");
            if (open) await _platformService.OpenFileWithDefaultAppAsync(path);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false; // nothing to export; the user was already told why
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating {What}", what);
            await _dialogService.ShowErrorDialogAsync("Report Error", $"Failed to create {what}: {ex.Message}");
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task ExportPdfAsync() => RunExportAsync("Work order (PDF)", async () =>
    {
        var tasks = await LoadEntriesOrWarnAsync();
        if (tasks == null) throw new OperationCanceledException();
        var settings = await _settingsService.GetPdfSettingsAsync();
        var folder = await _settingsService.GetPdfExportPathAsync();
        return await _exportService.ExportTasksToPdfAsync(tasks, settings, Path.Combine(folder, $"RadniNalog_{FileSuffix()}.pdf"));
    });

    [RelayCommand]
    private Task ExportWordAsync() => RunExportAsync("Work order (Word)", async () =>
    {
        var tasks = await LoadEntriesOrWarnAsync();
        if (tasks == null) throw new OperationCanceledException();
        var settings = await _settingsService.GetPdfSettingsAsync();
        var folder = await _settingsService.GetWordExportPathAsync();
        return await _exportService.ExportTasksToWordAsync(tasks, settings, Path.Combine(folder, $"RadniNalog_{FileSuffix()}.docx"));
    });

    [RelayCommand]
    private Task ExportExcelAsync() => RunExportAsync("Hours overview (Excel)", async () =>
    {
        var tasks = await LoadEntriesOrWarnAsync();
        if (tasks == null) throw new OperationCanceledException();
        var folder = await _settingsService.GetExcelExportPathAsync();
        return await _exportService.ExportTasksToExcelAsync(tasks, Path.Combine(folder, $"Pregled utrošenih sati - MA - {FileSuffix()}.xlsx"));
    });

    [RelayCommand]
    private async Task CreateInvoiceAsync()
    {
        if (string.IsNullOrWhiteSpace(InvoiceNumber))
        {
            await _dialogService.ShowInfoDialogAsync("Invoice", "Enter an invoice number.");
            return;
        }

        if (HourlyRate <= 0)
        {
            await _dialogService.ShowInfoDialogAsync("Invoice", "Enter an hourly rate greater than 0.");
            return;
        }

        if (TotalHours <= 0)
        {
            await _dialogService.ShowInfoDialogAsync("Invoice", "There are no hours for the selected month and client to invoice.");
            return;
        }

        var invoice = BuildInvoice();
        var issued = InvoiceNumber;

        var created = await RunExportAsync("Invoice", async () =>
        {
            var settings = await _settingsService.GetPdfSettingsAsync();
            var folder = await _settingsService.GetPdfExportPathAsync();
            var safeNumber = string.Concat(issued.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
            var path = await _exportService.ExportInvoiceToPdfAsync(invoice, settings, Path.Combine(folder, $"Racun_{safeNumber}.pdf"));

            await SaveInvoiceSettingsAsync();
            return path;
        });

        // Move on to the next number when this invoice used the suggested one
        if (created && issued == ComposeInvoiceNumber(_nextInvoiceNumber))
        {
            _nextInvoiceNumber++;
            await _settingsService.SetSettingAsync(NextNumberKey, _nextInvoiceNumber.ToString(CultureInfo.InvariantCulture));
            InvoiceNumber = ComposeInvoiceNumber(_nextInvoiceNumber);
        }
    }

    private async Task SaveInvoiceSettingsAsync()
    {
        await _settingsService.SetSettingAsync(RateKey, HourlyRate.ToString(CultureInfo.InvariantCulture));
        await _settingsService.SetSettingAsync(VatKey, VatPercent.ToString(CultureInfo.InvariantCulture));
        await _settingsService.SetSettingAsync(CurrencyKey, string.IsNullOrWhiteSpace(Currency) ? "BAM" : Currency.Trim());
        await _settingsService.SetSettingAsync(NoteKey, PaymentNote ?? "");
    }
}
