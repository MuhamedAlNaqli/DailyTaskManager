using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DailyTaskManager.Avalonia.Services;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Localization;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Avalonia.ViewModels;

/// <summary>A contract as shown in the list under a client</summary>
public class ContractRow
{
    public ContractRow(ClientContract contract)
    {
        Contract = contract;
        var from = contract.ValidFrom.ToString("d", Loc.Culture);
        var to = contract.ValidTo.HasValue ? contract.ValidTo.Value.ToString("d", Loc.Culture) : Loc.T("open");
        Period = $"{from} – {to}";
        Rate = contract.HourlyRate.HasValue ? contract.HourlyRate.Value.ToString("0.00", Loc.Culture) + " / h" : "";
    }

    public ClientContract Contract { get; }

    public string Number => Contract.ContractNumber;

    public string Period { get; }

    public string Rate { get; }
}

/// <summary>A tag colour a client can have</summary>
public record ColorChoice(string Name, string Hex)
{
    public static readonly IReadOnlyList<ColorChoice> All = new[]
    {
        new ColorChoice("No colour", ""), new ColorChoice("Teal", "#0F766E"), new ColorChoice("Blue", "#2563EB"),
        new ColorChoice("Purple", "#7C3AED"), new ColorChoice("Pink", "#DB2777"), new ColorChoice("Red", "#DC2626"),
        new ColorChoice("Orange", "#EA580C"), new ColorChoice("Amber", "#CA8A04"), new ColorChoice("Green", "#16A34A"),
        new ColorChoice("Grey", "#6B7280")
    };
}

/// <summary>"Clients" in Settings: legal details, contracts and export folder of each client</summary>
public partial class ClientsViewModel : ObservableObject
{
    public static readonly string[] CurrencyCodes = { "BAM", "EUR", "USD", "GBP", "CHF", "RSD", "TRY" };

    private readonly IBusinessService _business;
    private readonly IDocumentGenerator _generator;
    private readonly IExportService _exportService;
    private readonly IPlatformService _platformService;
    private readonly DialogService _dialogService;
    private readonly ILogger<ClientsViewModel> _logger;
    private bool _loadingEditor;

    public ObservableCollection<Client> Clients { get; } = new();

    public ObservableCollection<ContractRow> Contracts { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing), nameof(CanDelete), nameof(CanEditContracts), nameof(EditorTitle))]
    private Client? _selectedClient;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing), nameof(EditorTitle))]
    private bool _isNewClient;

    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _legalName = "";
    [ObservableProperty] private string _companyId = "";
    [ObservableProperty] private string _vatNumber = "";
    [ObservableProperty] private string _addressLine1 = "";
    [ObservableProperty] private string _addressLine2 = "";
    [ObservableProperty] private string _notes = "";
    [ObservableProperty] private string _currency = "BAM";
    [ObservableProperty] private string _exportFolder = "";
    [ObservableProperty] private bool _isActive = true;

    public IReadOnlyList<ColorChoice> Colors => ColorChoice.All;

    [ObservableProperty] private ColorChoice? _selectedColor = ColorChoice.All[0];

    [ObservableProperty] private ContractRow? _selectedContract;
    [ObservableProperty] private string _contractNumber = "";
    [ObservableProperty] private string _contractRate = "";
    [ObservableProperty] private DateTimeOffset? _contractFrom = DateTimeOffset.Now;
    [ObservableProperty] private DateTimeOffset? _contractTo;

    [ObservableProperty] private bool _isBusy;

    // Template files of the selected client
    public ObservableCollection<DocumentTemplate> Templates { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedTemplate))]
    private DocumentTemplate? _selectedTemplate;

    [ObservableProperty] private string _templateName = "";
    [ObservableProperty] private string _templatePattern = DocumentTemplate.DefaultFileNamePattern;
    [ObservableProperty] private bool _templateActive = true;

    public bool HasSelectedTemplate => SelectedTemplate != null;

    public IReadOnlyList<string> Currencies => CurrencyCodes;

    public bool IsEditing => IsNewClient || SelectedClient != null;

    public bool CanDelete => SelectedClient != null;

    public bool CanEditContracts => SelectedClient != null;

    public string EditorTitle => IsNewClient ? "New client" : SelectedClient?.Name ?? "";

    public ClientsViewModel(IBusinessService business, IDocumentGenerator generator, IExportService exportService, IPlatformService platformService,
        DialogService dialogService, ILogger<ClientsViewModel> logger)
    {
        _business = business;
        _generator = generator;
        _exportService = exportService;
        _platformService = platformService;
        _dialogService = dialogService;
        _logger = logger;

        _ = ReloadAsync(null);
    }

    public async Task ReloadAsync(int? select)
    {
        try
        {
            var clients = await _business.GetClientsAsync();
            Clients.Clear();
            foreach (var client in clients) Clients.Add(client);

            SelectedClient = Clients.FirstOrDefault(c => c.Id == select) ?? (select == null ? Clients.FirstOrDefault(c => c.IsActive) : null);
            if (SelectedClient == null && !IsNewClient) LoadEditor(null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load clients");
        }
    }

    partial void OnSelectedClientChanged(Client? value)
    {
        if (value == null) return;

        IsNewClient = false;
        LoadEditor(value);
        _ = LoadContractsAsync(value.Id);
        _ = LoadTemplatesAsync(value.Id);
    }

    private void LoadEditor(Client? client)
    {
        _loadingEditor = true;
        Name = client?.Name ?? "";
        LegalName = client?.LegalName ?? "";
        CompanyId = client?.CompanyId ?? "";
        VatNumber = client?.VatNumber ?? "";
        AddressLine1 = client?.AddressLine1 ?? "";
        AddressLine2 = client?.AddressLine2 ?? "";
        Notes = client?.Notes ?? "";
        Currency = client?.Currency ?? "BAM";
        ExportFolder = client?.ExportFolder ?? "";
        IsActive = client?.IsActive ?? true;
        SelectedColor = ColorChoice.All.FirstOrDefault(c => string.Equals(c.Hex, client?.Color ?? "", StringComparison.OrdinalIgnoreCase)) ?? ColorChoice.All[0];
        _loadingEditor = false;

        if (client == null)
        {
            Contracts.Clear();
            Templates.Clear();
            SelectedTemplate = null;
            ClearContractEditor();
        }
    }

    private async Task LoadContractsAsync(int clientId)
    {
        try
        {
            var contracts = await _business.GetContractsAsync(clientId);
            Contracts.Clear();
            foreach (var contract in contracts) Contracts.Add(new ContractRow(contract));
            SelectedContract = Contracts.FirstOrDefault(c => c.Contract.ValidTo == null) ?? Contracts.FirstOrDefault();
            if (SelectedContract == null) ClearContractEditor();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load contracts");
        }
    }

    partial void OnSelectedContractChanged(ContractRow? value)
    {
        if (value == null) return;

        ContractNumber = value.Contract.ContractNumber;
        ContractRate = value.Contract.HourlyRate?.ToString("0.00", Loc.Culture) ?? "";
        ContractFrom = new DateTimeOffset(value.Contract.ValidFrom.Date);
        ContractTo = value.Contract.ValidTo.HasValue ? new DateTimeOffset(value.Contract.ValidTo.Value.Date) : null;
    }

    private void ClearContractEditor()
    {
        SelectedContract = null;
        ContractNumber = "";
        ContractRate = "";
        ContractFrom = DateTimeOffset.Now;
        ContractTo = null;
    }

    // ---- Client commands --------------------------------------------------------------------------------------

    [RelayCommand]
    private void NewClient()
    {
        SelectedClient = null;
        IsNewClient = true;
        LoadEditor(null);
    }

    [RelayCommand]
    private async Task SaveClientAsync()
    {
        if (_loadingEditor) return;

        try
        {
            IsBusy = true;
            var saved = await _business.SaveClientAsync(new Client
            {
                Id = IsNewClient ? 0 : SelectedClient?.Id ?? 0,
                Name = Name,
                LegalName = LegalName,
                CompanyId = CompanyId,
                VatNumber = VatNumber,
                AddressLine1 = AddressLine1,
                AddressLine2 = AddressLine2,
                Notes = Notes,
                Currency = Currency,
                ExportFolder = ExportFolder,
                IsActive = IsActive,
                Color = SelectedColor?.Hex ?? ""
            });

            IsNewClient = false;
            await ReloadAsync(saved.Id);
            await _dialogService.ShowInfoDialogAsync("Saved", "The client was saved.");
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            await _dialogService.ShowErrorDialogAsync("Client", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving the client");
            await _dialogService.ShowErrorDialogAsync("Error", $"Could not save the client: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteClientAsync()
    {
        var client = SelectedClient;
        if (client == null) return;

        var confirmed = await _dialogService.ShowConfirmDialogAsync("Delete client",
            $"Delete \"{client.Name}\"? A client that has entries is made inactive instead, so its entries and documents stay available.");
        if (!confirmed) return;

        try
        {
            IsBusy = true;
            var deleted = await _business.DeleteOrDeactivateClientAsync(client.Id);
            await ReloadAsync(deleted ? null : client.Id);
            if (!deleted)
            {
                await _dialogService.ShowInfoDialogAsync("Client", "The client has entries, so it was made inactive instead of deleted.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting the client");
            await _dialogService.ShowErrorDialogAsync("Error", $"Could not delete the client: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task BrowseFolderAsync()
    {
        var result = await _dialogService.ShowFolderDialogAsync(Loc.T("Select the folder for this client's documents"), ExportFolder);
        if (!string.IsNullOrEmpty(result)) ExportFolder = result;
    }

    [RelayCommand]
    private void ClearFolder() => ExportFolder = "";

    [RelayCommand]
    private async Task PreviewAsync()
    {
        var client = SelectedClient;
        if (client == null) return;

        try
        {
            IsBusy = true;
            var settings = await _business.GetDocumentSettingsAsync(client.Id, DateTime.Today);
            await ShowSampleWorkOrderAsync(_exportService, _platformService, settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating the preview");
            await _dialogService.ShowErrorDialogAsync("Error", $"Could not create the preview: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Creates a work order with made-up entries in the temp folder and opens it</summary>
    public static async Task ShowSampleWorkOrderAsync(IExportService exportService, IPlatformService platformService, PdfSettings settings)
    {
        var sample = new List<DailyTask>
        {
            new() { Description = "Sample task 1 - development", Date = DateTime.Today.AddDays(-2), Hours = 8 },
            new() { Description = "Sample task 2 - testing and fixes", Date = DateTime.Today.AddDays(-1), Hours = 6 },
            new() { Description = "Sample task 3 - documentation", Date = DateTime.Today, Hours = 4 }
        };

        var path = Path.Combine(Path.GetTempPath(), "DailyTaskManager_Preview.pdf");
        await exportService.ExportTasksToPdfAsync(sample, settings, path);
        await platformService.OpenFileWithDefaultAppAsync(path);
    }

    // ---- Template commands ------------------------------------------------------------------------------------

    private static readonly Dictionary<string, List<string>> TemplateFiles = new()
    {
        ["Word and Excel files"] = new List<string> { "*.docx", "*.xlsx" }
    };

    private async Task LoadTemplatesAsync(int clientId, int? select = null)
    {
        try
        {
            var templates = await _business.GetTemplatesAsync(clientId);
            Templates.Clear();
            foreach (var template in templates) Templates.Add(template);
            SelectedTemplate = Templates.FirstOrDefault(t => t.Id == select) ?? Templates.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load templates");
        }
    }

    partial void OnSelectedTemplateChanged(DocumentTemplate? value)
    {
        TemplateName = value?.Name ?? "";
        TemplatePattern = value?.FileNamePattern ?? DocumentTemplate.DefaultFileNamePattern;
        TemplateActive = value?.IsActive ?? true;
    }

    [RelayCommand]
    private async Task AddTemplateAsync()
    {
        var client = SelectedClient;
        if (client == null) return;

        try
        {
            var path = await _platformService.ShowOpenFileDialogAsync(Loc.T("Choose a Word or Excel template"), TemplateFiles);
            if (string.IsNullOrEmpty(path)) return;

            IsBusy = true;
            var added = await _business.AddTemplateAsync(client.Id, path);
            await LoadTemplatesAsync(client.Id, added.Id);

            var found = await _generator.ScanPlaceholdersAsync(added.TemplatePath);
            var unknown = found.Where(f => !_generator.KnownPlaceholders.Contains(f, StringComparer.OrdinalIgnoreCase)).ToList();
            await _dialogService.ShowInfoDialogAsync("Template added", unknown.Count == 0
                ? (found.Count == 0 ? "The template was added, but it contains no {{placeholders}} yet." : Loc.F("The template was added. It uses {0} placeholders.", found.Count))
                : Loc.F("The template was added, but these placeholders are not known and will stay visible: {0}", string.Join(", ", unknown)));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or IOException)
        {
            await _dialogService.ShowErrorDialogAsync("Template", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding the template");
            await _dialogService.ShowErrorDialogAsync("Error", $"Could not add the template: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveTemplateAsync()
    {
        var client = SelectedClient;
        var template = SelectedTemplate;
        if (client == null || template == null) return;

        try
        {
            await _business.UpdateTemplateAsync(new DocumentTemplate
            {
                Id = template.Id, Name = TemplateName, FileNamePattern = TemplatePattern, IsActive = TemplateActive
            });
            await LoadTemplatesAsync(client.Id, template.Id);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            await _dialogService.ShowErrorDialogAsync("Template", ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteTemplateAsync()
    {
        var client = SelectedClient;
        var template = SelectedTemplate;
        if (client == null || template == null) return;

        var confirmed = await _dialogService.ShowConfirmDialogAsync("Delete template", $"Delete the template \"{template.Name}\"? Documents already made from it stay where they are.");
        if (!confirmed) return;

        await _business.DeleteTemplateAsync(template.Id);
        await LoadTemplatesAsync(client.Id);
    }

    [RelayCommand]
    private async Task ShowPlaceholdersAsync()
    {
        var lines = new System.Text.StringBuilder();
        lines.AppendLine(Loc.T("Write a placeholder in your Word or Excel file like {{Client.LegalName}}. A table row (or sheet row) with {{Task.Date}}, {{Task.Description}} or {{Task.Hours}} is repeated for every entry."));
        lines.AppendLine();
        lines.Append(string.Join("   ", _generator.KnownPlaceholders.Select(p => "{{" + p + "}}")));
        await _dialogService.ShowInfoDialogAsync("Placeholders", lines.ToString());
    }

    // ---- Contract commands ------------------------------------------------------------------------------------

    [RelayCommand]
    private void ClearContractTo() => ContractTo = null;

    [RelayCommand]
    private void NewContract()
    {
        ClearContractEditor();
        var last = Contracts.Select(c => c.Contract).OrderByDescending(c => c.ValidFrom).FirstOrDefault();
        if (last != null)
        {
            // A renewal usually starts the day after the previous contract ends
            var next = (last.ValidTo ?? last.ValidFrom.AddYears(1).AddDays(-1)).AddDays(1);
            ContractFrom = new DateTimeOffset(next.Date);
        }
    }

    [RelayCommand]
    private async Task SaveContractAsync()
    {
        var client = SelectedClient;
        if (client == null) return;

        if (!TryParseRate(ContractRate, out var rate))
        {
            await _dialogService.ShowErrorDialogAsync("Contract", "The hourly rate is not a number.");
            return;
        }

        if (ContractFrom == null)
        {
            await _dialogService.ShowErrorDialogAsync("Contract", "Choose the date the contract starts.");
            return;
        }

        try
        {
            IsBusy = true;
            var contract = new ClientContract
            {
                Id = SelectedContract?.Contract.Id ?? 0,
                ClientId = client.Id,
                ContractNumber = ContractNumber,
                HourlyRate = rate,
                ValidFrom = ContractFrom.Value.Date,
                ValidTo = ContractTo?.Date
            };

            // Renewing: the contract that is still open ends the day before the new one starts
            if (contract.Id == 0)
            {
                var open = Contracts.Select(c => c.Contract)
                    .FirstOrDefault(c => c.ValidTo == null && c.ValidFrom.Date < contract.ValidFrom.Date);
                if (open != null)
                {
                    open.ValidTo = contract.ValidFrom.AddDays(-1);
                    await _business.SaveContractAsync(open);
                }
            }

            await _business.SaveContractAsync(contract);
            await LoadContractsAsync(client.Id);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            await _dialogService.ShowErrorDialogAsync("Contract", ex.Message);
            await LoadContractsAsync(client.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving the contract");
            await _dialogService.ShowErrorDialogAsync("Error", $"Could not save the contract: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteContractAsync()
    {
        var client = SelectedClient;
        var row = SelectedContract;
        if (client == null || row == null) return;

        var confirmed = await _dialogService.ShowConfirmDialogAsync("Delete contract",
            $"Delete contract {row.Number}? Documents for its months will have no contract number.");
        if (!confirmed) return;

        await _business.DeleteContractAsync(row.Contract.Id);
        await LoadContractsAsync(client.Id);
    }

    /// <summary>
    /// Reads a rate typed with either "30,5" or "30.5" regardless of the interface language ("30,5" must not
    /// become 305 just because English uses the comma for thousands). A separator followed by one or two digits
    /// at the end is the decimal point; any other separator groups thousands.
    /// </summary>
    public static bool TryParseRate(string text, out decimal? rate)
    {
        rate = null;
        var trimmed = (text ?? "").Trim().Replace(" ", "");
        if (trimmed.Length == 0) return true;

        var lastComma = trimmed.LastIndexOf(',');
        var lastDot = trimmed.LastIndexOf('.');
        var decimalAt = Math.Max(lastComma, lastDot);
        string normalized;
        if (decimalAt >= 0 && trimmed.Length - decimalAt - 1 is >= 1 and <= 2)
        {
            normalized = trimmed.Substring(0, decimalAt).Replace(",", "").Replace(".", "") + "." + trimmed.Substring(decimalAt + 1);
        }
        else
        {
            normalized = trimmed.Replace(",", "").Replace(".", "");
        }

        if (decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value) && value >= 0)
        {
            rate = value;
            return true;
        }

        return false;
    }
}
