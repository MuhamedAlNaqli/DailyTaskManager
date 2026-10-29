using DailyTaskManager.Core.Localization;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Models;
using DailyTaskManager.Avalonia.Services;

namespace DailyTaskManager.Avalonia.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IExportService _exportService;
    private readonly ILogger<SettingsViewModel> _logger;
    private readonly IConfiguration _configuration;
    private readonly DialogService _dialogService;
    private readonly IUpdateService _updateService;
    private readonly IPlatformService _platformService;
    private readonly IBackupService _backupService;
    private readonly IBackgroundReminderService _backgroundReminder;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private PdfSettings _pdfSettings = new();

    [ObservableProperty]
    private string _pdfExportPath = string.Empty;

    [ObservableProperty]
    private string _wordExportPath = string.Empty;

    [ObservableProperty]
    private string _excelExportPath = string.Empty;

    [ObservableProperty]
    private bool _reminderEnabled = ReminderSettings.DefaultEnabled;

    [ObservableProperty]
    private decimal _reminderHour = ReminderSettings.DefaultHour;

    /// <summary>Languages of the interface (shown in their own names)</summary>
    public System.Collections.Generic.IReadOnlyList<System.Collections.Generic.KeyValuePair<string, string>> AppLanguages => Loc.Languages;

    [ObservableProperty]
    private System.Collections.Generic.KeyValuePair<string, string>? _selectedAppLanguage =
        Loc.Languages.FirstOrDefault(l => l.Key == Loc.Language);

    partial void OnSelectedAppLanguageChanged(System.Collections.Generic.KeyValuePair<string, string>? value)
    {
        if (value != null) Loc.SetLanguage(value.Value.Key);
    }

    /// <summary>Languages offered for dictation</summary>
    public System.Collections.Generic.IReadOnlyList<System.Collections.Generic.KeyValuePair<string, string>> DictationLanguages => DictationSettings.Languages;

    [ObservableProperty]
    private System.Collections.Generic.KeyValuePair<string, string>? _selectedDictationLanguage = DictationSettings.Languages[0];

    [ObservableProperty]
    private bool _backgroundUpdateNotifyEnabled = UpdateSettings.DefaultBackgroundNotify;

    [ObservableProperty]
    private bool _backgroundReminderEnabled = ReminderSettings.DefaultBackground;

    [ObservableProperty]
    private string _backgroundReminderStatus = string.Empty;

    private bool _loadingReminder;

    /// <summary>False where Windows cannot run the reminder while the app is closed</summary>
    public bool IsBackgroundReminderSupported => _backgroundReminder.IsSupported;

    [ObservableProperty]
    private bool _isCheckingForUpdates = false;

    [ObservableProperty]
    private string _updateCheckResult = string.Empty;

    public ObservableCollection<Setting> DbSettings { get; } = new();
    public ObservableCollection<AppSettingItem> AppSettings { get; } = new();

    public SettingsViewModel(ISettingsService settingsService, IExportService exportService,
        ILogger<SettingsViewModel> logger, IConfiguration configuration, DialogService dialogService, IUpdateService updateService, IPlatformService platformService, IBackupService backupService,
        IBackgroundReminderService backgroundReminder)
    {
        _backupService = backupService;
        _backgroundReminder = backgroundReminder;
        _settingsService = settingsService;
        _exportService = exportService;
        _logger = logger;
        _configuration = configuration;
        _dialogService = dialogService;
        _updateService = updateService;
        _platformService = platformService;
        
        _logger.LogInformation("Settings view initialized");
        
        // Load data sequentially to avoid DbContext concurrency issues
        _ = LoadAllDataAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            // Load DB settings
            DbSettings.Clear();
            try
            {
                var allSettings = await _settingsService.GetAllSettingsAsync();
                foreach (var setting in allSettings)
                {
                    DbSettings.Add(setting);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load database settings");
            }

            // Load App.config equivalent (appsettings.json)
            AppSettings.Clear();

            // Add connection string section
            var connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "(missing)";
            AppSettings.Add(new AppSettingItem { Key = "Database Connection String", Value = MaskPassword(connectionString) });

            // Parse and display connection details
            if (!string.IsNullOrEmpty(connectionString) && connectionString != "(missing)")
            {
                try
                {
                    var connectionParts = connectionString.Split(';');
                    foreach (var part in connectionParts)
                    {
                        var keyValue = part.Split('=', 2);
                        if (keyValue.Length == 2)
                        {
                            var key = keyValue[0].Trim();
                            var value = keyValue[1].Trim();

                            // Add readable connection details
                            switch (key.ToLower())
                            {
                                case "host":
                                    AppSettings.Add(new AppSettingItem { Key = "Database Host", Value = value });
                                    break;
                                case "port":
                                    AppSettings.Add(new AppSettingItem { Key = "Database Port", Value = value });
                                    break;
                                case "database":
                                    AppSettings.Add(new AppSettingItem { Key = "Database Name", Value = value });
                                    break;
                                case "username":
                                    AppSettings.Add(new AppSettingItem { Key = "Database Username", Value = value });
                                    break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not parse connection string details");
                }
            }

            // Add AppSettings section
            var appSettingsSection = _configuration.GetSection("AppSettings");
            if (appSettingsSection.Exists())
            {
                foreach (var child in appSettingsSection.GetChildren())
                {
                    var key = child.Key;
                    var value = child.Value ?? "N/A";
                    AppSettings.Add(new AppSettingItem { Key = $"AppSettings:{key}", Value = value });
                }
            }

            // Add Logging section
            var loggingSection = _configuration.GetSection("Logging");
            if (loggingSection.Exists())
            {
                foreach (var child in loggingSection.GetChildren())
                {
                    if (child.Key == "LogLevel")
                    {
                        foreach (var logLevel in child.GetChildren())
                        {
                            AppSettings.Add(new AppSettingItem { Key = $"Logging:LogLevel:{logLevel.Key}", Value = logLevel.Value ?? "Information" });
                        }
                    }
                    else
                    {
                        AppSettings.Add(new AppSettingItem { Key = $"Logging:{child.Key}", Value = child.Value ?? "N/A" });
                    }
                }
            }

            // Add OAuth section
            var oauthSection = _configuration.GetSection("OAuth");
            if (oauthSection.Exists())
            {
                foreach (var provider in oauthSection.GetChildren())
                {
                    foreach (var setting in provider.GetChildren())
                    {
                        var value = setting.Value ?? "Not configured";
                        var isPlaceholder = string.IsNullOrWhiteSpace(value) || value.Contains("_here", StringComparison.OrdinalIgnoreCase);
                        if (isPlaceholder)
                        {
                            value = "Not configured";
                        }
                        else if (setting.Key.ToLower().Contains("secret"))
                        {
                            // Mask sensitive information like client secrets
                            value = "***configured***";
                        }
                        AppSettings.Add(new AppSettingItem { Key = $"OAuth:{provider.Key}:{setting.Key}", Value = value });
                    }
                }
            }

            // Add Features section
            var featuresSection = _configuration.GetSection("Features");
            if (featuresSection.Exists())
            {
                foreach (var feature in featuresSection.GetChildren())
                {
                    AppSettings.Add(new AppSettingItem { Key = $"Features:{feature.Key}", Value = feature.Value ?? "N/A" });
                }
            }

            // Add UpdateService section
            var updateServiceSection = _configuration.GetSection("UpdateService");
            if (updateServiceSection.Exists())
            {
                foreach (var setting in updateServiceSection.GetChildren())
                {
                    AppSettings.Add(new AppSettingItem { Key = $"UpdateService:{setting.Key}", Value = setting.Value ?? "N/A" });
                }
            }
            
            _logger.LogInformation("Settings loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings");
        }
    }

    private async Task LoadAllDataAsync()
    {
        try
        {
            IsLoading = true;
            
            // Load all settings first
            await LoadAsync();
            
            // Then load PDF settings to avoid DbContext concurrency
            await LoadPdfSettingsAsync();

            // Load export paths
            await LoadExportPathsAsync();

            await LoadReminderSettingsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings data");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ---- Backup and import ------------------------------------------------------------------------

    private static readonly Dictionary<string, List<string>> JsonFiles = new() { ["Backup file (*.json)"] = new List<string> { "*.json" } };

    private static readonly Dictionary<string, List<string>> SpreadsheetFiles = new()
    {
        ["CSV and Excel files"] = new List<string> { "*.csv", "*.xlsx" }
    };

    [RelayCommand]
    private async Task BackupDataAsync()
    {
        try
        {
            var path = await _platformService.ShowSaveFileDialogAsync("Save backup",
                $"DailyTaskManager-backup-{DateTime.Now:yyyyMMdd}.json", JsonFiles);
            if (string.IsNullOrEmpty(path)) return;

            IsLoading = true;
            await _backupService.CreateBackupAsync(path);
            await _dialogService.ShowInfoDialogAsync("Backup created", $"Your entries and templates were saved to:\n{path}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating backup");
            await _dialogService.ShowErrorDialogAsync("Backup Error", $"Failed to create the backup: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private Task RestoreBackupAsync() => ImportFromFileAsync("Restore from backup", JsonFiles);

    [RelayCommand]
    private Task ImportEntriesAsync() => ImportFromFileAsync("Import entries from a CSV or Excel file", SpreadsheetFiles);

    private async Task ImportFromFileAsync(string title, Dictionary<string, List<string>> fileTypes)
    {
        try
        {
            var path = await _platformService.ShowOpenFileDialogAsync(title, fileTypes);
            if (string.IsNullOrEmpty(path)) return;

            IsLoading = true;
            var preview = await _backupService.ReadImportFileAsync(path);
            IsLoading = false;

            if (preview.Entries.Count == 0 && preview.Templates.Count == 0)
            {
                await _dialogService.ShowInfoDialogAsync("Nothing to import",
                    "No entries could be read from this file. A CSV or Excel file needs Date, Description and Hours columns.");
                return;
            }

            var message = new System.Text.StringBuilder();
            message.AppendLine(Loc.F("{0}: {1} entries", preview.SourceName, preview.Entries.Count) +
                               (preview.From.HasValue ? " " + Loc.F("from {0} to {1}.", $"{preview.From:d MMM yyyy}", $"{preview.To:d MMM yyyy}") : "."));
            message.AppendLine(Loc.F("{0} are new and {1} already exist (they will be skipped).", preview.NewCount, preview.DuplicateCount));
            if (preview.Templates.Count > 0) message.AppendLine(Loc.F("{0} templates are included (existing names are kept).", preview.Templates.Count));
            if (preview.SkippedRows > 0) message.AppendLine(Loc.F("{0} rows could not be read and will be ignored.", preview.SkippedRows));
            message.AppendLine();
            message.Append(Loc.T("Import now?"));

            if (!await _dialogService.ShowConfirmDialogAsync(title, message.ToString())) return;

            IsLoading = true;
            var result = await _backupService.ImportAsync(preview);
            await _dialogService.ShowInfoDialogAsync("Import complete",
                Loc.F("Added {0} entries ({1} skipped as duplicates).", result.Added, result.Duplicates) +
                (result.ClientsCreated > 0 ? "\n" + Loc.F("Created {0} new clients.", result.ClientsCreated) : "") +
                (result.TemplatesAdded > 0 ? "\n" + Loc.F("Added {0} templates.", result.TemplatesAdded) : ""));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing from file");
            await _dialogService.ShowErrorDialogAsync("Import Error", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadReminderSettingsAsync()
    {
        try
        {
            _loadingReminder = true;
            var enabled = await _settingsService.GetSettingAsync<string>(ReminderSettings.EnabledKey, null);
            var hour = await _settingsService.GetSettingAsync<string>(ReminderSettings.HourKey, null);
            var background = await _settingsService.GetSettingAsync<string>(ReminderSettings.BackgroundKey, null);
            ReminderEnabled = ReminderSettings.ParseFlag(enabled, ReminderSettings.DefaultEnabled);
            ReminderHour = ReminderSettings.ParseHour(hour);
            BackgroundReminderEnabled = ReminderSettings.ParseFlag(background, ReminderSettings.DefaultBackground);
            var updateNotify = await _settingsService.GetSettingAsync<string>(UpdateSettings.BackgroundNotifyKey, null);
            var dictationLanguage = await _settingsService.GetSettingAsync<string>(DictationSettings.LanguageKey, null);
            SelectedDictationLanguage = DictationSettings.Languages.FirstOrDefault(l => l.Key == dictationLanguage) is { Key: not null } found
                ? found
                : DictationSettings.Languages[0];
            BackgroundUpdateNotifyEnabled = ReminderSettings.ParseFlag(updateNotify, UpdateSettings.DefaultBackgroundNotify);
            await ShowBackgroundReminderStatusAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load reminder settings");
        }
        finally
        {
            _loadingReminder = false;
        }
    }

    partial void OnReminderEnabledChanged(bool value)
    {
        if (_loadingReminder) return;
        _ = _settingsService.SetSettingAsync(ReminderSettings.EnabledKey, value ? "true" : "false");
        _ = ApplyBackgroundReminderAsync();
    }

    partial void OnBackgroundReminderEnabledChanged(bool value)
    {
        if (_loadingReminder) return;
        _ = _settingsService.SetSettingAsync(ReminderSettings.BackgroundKey, value ? "true" : "false");
        _ = ApplyBackgroundReminderAsync();
    }

    partial void OnSelectedDictationLanguageChanged(System.Collections.Generic.KeyValuePair<string, string>? value)
    {
        if (_loadingReminder || value == null) return;
        _ = _settingsService.SetSettingAsync(DictationSettings.LanguageKey, value.Value.Key);
    }

    partial void OnBackgroundUpdateNotifyEnabledChanged(bool value)
    {
        if (_loadingReminder) return;
        _ = _settingsService.SetSettingAsync(UpdateSettings.BackgroundNotifyKey, value ? "true" : "false");
        _ = ApplyBackgroundReminderAsync();
    }

    /// <summary>Registers or removes the Windows scheduled task (shared by the hours reminder and the update notice) to match the switches</summary>
    private async Task ApplyBackgroundReminderAsync()
    {
        try
        {
            var state = Background.ReminderState.Load();
            state.UpdateNotifications = BackgroundUpdateNotifyEnabled;
            state.Save();
            await _backgroundReminder.SetInstalledAsync((ReminderEnabled && BackgroundReminderEnabled) || BackgroundUpdateNotifyEnabled);
            await ShowBackgroundReminderStatusAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not change the background reminder");
            BackgroundReminderStatus = "Could not change the background reminder: " + ex.Message;
        }
    }

    private async Task ShowBackgroundReminderStatusAsync()
    {
        if (!_backgroundReminder.IsSupported)
        {
            BackgroundReminderStatus = "Background reminders are not available on this system.";
            return;
        }

        var installed = await _backgroundReminder.IsInstalledAsync();
        BackgroundReminderStatus = installed
            ? "Active: Windows checks every 30 minutes, even when this app is closed."
            : "Off: you will only be reminded while this app is open.";
    }

    [RelayCommand]
    private async Task SendTestReminderAsync()
    {
        try
        {
            await _backgroundReminder.SendTestNotificationAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not send the test notification");
            BackgroundReminderStatus = "Could not show the test notification: " + ex.Message;
        }
    }

    partial void OnReminderHourChanged(decimal value)
    {
        if (!_loadingReminder) _ = _settingsService.SetSettingAsync(ReminderSettings.HourKey, ((int)Math.Clamp(value, 0, 23)).ToString());
    }

    private async Task LoadPdfSettingsAsync()
    {
        try
        {
            // Load PDF settings from database
            PdfSettings = await _settingsService.GetPdfSettingsAsync();
            _logger.LogInformation("PDF settings loaded from database");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading PDF settings, using defaults");
        }
    }

    private async Task LoadExportPathsAsync()
    {
        try
        {
            // Load PDF export path
            var pdfPath = await _settingsService.GetSettingAsync<string>("PdfExportPath");
            PdfExportPath = await GetExportPathWithFallback(pdfPath, "PDF");

            // Load Word export path
            var wordPath = await _settingsService.GetSettingAsync<string>("WordExportPath");
            WordExportPath = await GetExportPathWithFallback(wordPath, "Word");

            // Load Excel export path
            var excelPath = await _settingsService.GetSettingAsync<string>("ExcelExportPath");
            ExcelExportPath = await GetExportPathWithFallback(excelPath, "Excel");

            _logger.LogInformation("Export paths loaded - PDF: {PdfPath}, Word: {WordPath}, Excel: {ExcelPath}",
                PdfExportPath, WordExportPath, ExcelExportPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading export paths, using defaults");
            var defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            PdfExportPath = defaultPath;
            WordExportPath = defaultPath;
            ExcelExportPath = defaultPath;
        }
    }

    private async Task<string> GetExportPathWithFallback(string? savedPath, string exportType)
    {
        if (!string.IsNullOrEmpty(savedPath))
            return savedPath;

        // Fallback to appsettings.json
        var configPath = _configuration["AppSettings:ExportPath"];
        if (!string.IsNullOrEmpty(configPath))
        {
            if (configPath.StartsWith("~/"))
            {
                return configPath.Replace("~/", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + Path.DirectorySeparatorChar);
            }
            return configPath;
        }

        // Default to Desktop
        return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    }

    [RelayCommand]
    private async Task BrowsePdfFolderAsync()
    {
        try
        {
            var result = await _dialogService.ShowFolderDialogAsync("Select PDF Export Folder", PdfExportPath);
            if (!string.IsNullOrEmpty(result))
            {
                PdfExportPath = result;
                await _settingsService.SetSettingAsync("PdfExportPath", PdfExportPath, "PDF export location");
                _logger.LogInformation("PDF export path updated to: {Path}", PdfExportPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error browsing for PDF folder");
            await _dialogService.ShowErrorDialogAsync("Error", $"Error selecting PDF folder: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task BrowseWordFolderAsync()
    {
        try
        {
            var result = await _dialogService.ShowFolderDialogAsync("Select Word Export Folder", WordExportPath);
            if (!string.IsNullOrEmpty(result))
            {
                WordExportPath = result;
                await _settingsService.SetSettingAsync("WordExportPath", WordExportPath, "Word export location");
                _logger.LogInformation("Word export path updated to: {Path}", WordExportPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error browsing for Word folder");
            await _dialogService.ShowErrorDialogAsync("Error", $"Error selecting Word folder: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task BrowseExcelFolderAsync()
    {
        try
        {
            var result = await _dialogService.ShowFolderDialogAsync("Select Excel Export Folder", ExcelExportPath);
            if (!string.IsNullOrEmpty(result))
            {
                ExcelExportPath = result;
                await _settingsService.SetSettingAsync("ExcelExportPath", ExcelExportPath, "Excel export location");
                _logger.LogInformation("Excel export path updated to: {Path}", ExcelExportPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error browsing for Excel folder");
            await _dialogService.ShowErrorDialogAsync("Error", $"Error selecting Excel folder: {ex.Message}");
        }
    }


    [RelayCommand]
    private async Task SavePdfSettingsAsync()
    {
        try
        {
            IsLoading = true;
            
            // Save PDF settings using the service method
            await _settingsService.SavePdfSettingsAsync(PdfSettings);
            await _dialogService.ShowInfoDialogAsync("Success", "PDF settings saved successfully!");
            _logger.LogInformation("PDF settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving PDF settings");
            await _dialogService.ShowErrorDialogAsync("Error", $"Error saving PDF settings: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task PreviewPdfAsync()
    {
        try
        {
            IsLoading = true;
            
            // Create sample tasks for preview
            var sampleTasks = new List<DailyTask>
            {
                new DailyTask { Description = "Sample Task 1 - Development work", Date = DateTime.Now.AddDays(-2), Hours = 8 },
                new DailyTask { Description = "Sample Task 2 - Testing and debugging", Date = DateTime.Now.AddDays(-1), Hours = 6 },
                new DailyTask { Description = "Sample Task 3 - Documentation update", Date = DateTime.Now, Hours = 4 }
            };

            // Generate preview with current settings using the correct method
            // Write the preview to a temp file so it never overwrites a real report
            var previewPath = Path.Combine(Path.GetTempPath(), "DailyTaskManager_PdfPreview.pdf");
            await _exportService.ExportTasksToPdfAsync(sampleTasks, PdfSettings, previewPath);
            await _platformService.OpenFileWithDefaultAppAsync(previewPath);
            _logger.LogInformation("PDF preview generated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF preview");
            await _dialogService.ShowErrorDialogAsync("Error", $"Error generating PDF preview: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        try
        {
            IsCheckingForUpdates = true;
            var currentVersion = _updateService.GetCurrentVersion();
            UpdateCheckResult = $"Current version: {currentVersion}\nChecking for updates...";
            _logger.LogInformation("Manual update check requested (current version {Version})", currentVersion);

            var outcome = await _updateService.CheckAndPromptAsync();

            UpdateCheckResult = outcome.Status switch
            {
                UpdateCheckStatus.UpdateAvailable => $"Current version: {currentVersion}\n{outcome.Message}",
                UpdateCheckStatus.UpToDate => outcome.Message,
                _ => $"Update check failed.\n{outcome.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
            UpdateCheckResult = $"Error checking for updates: {ex.Message}";
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    private static string MaskPassword(string connectionString) =>
        System.Text.RegularExpressions.Regex.Replace(connectionString, @"(?i)(password|pwd)\s*=\s*[^;]*", "$1=*****");

    public bool HasDbSettings => DbSettings.Any();
}

public class AppSettingItem
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}