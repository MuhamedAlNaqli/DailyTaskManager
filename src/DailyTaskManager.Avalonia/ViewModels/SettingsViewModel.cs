using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
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

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private PdfSettings _pdfSettings = new();

    public ObservableCollection<Setting> DbSettings { get; } = new();
    public ObservableCollection<AppSettingItem> AppSettings { get; } = new();

    public SettingsViewModel(ISettingsService settingsService, IExportService exportService, 
        ILogger<SettingsViewModel> logger, IConfiguration configuration, DialogService dialogService)
    {
        _settingsService = settingsService;
        _exportService = exportService;
        _logger = logger;
        _configuration = configuration;
        _dialogService = dialogService;
        
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
            
            // Add configuration values
            var connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "(missing)";
            AppSettings.Add(new AppSettingItem { Key = "ConnectionStrings:DefaultConnection", Value = connectionString });
            
            // Add other configuration sections if needed
            var loggingSection = _configuration.GetSection("Logging");
            if (loggingSection.Exists())
            {
                AppSettings.Add(new AppSettingItem { Key = "Logging:LogLevel:Default", Value = loggingSection["LogLevel:Default"] ?? "Information" });
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

    [RelayCommand]
    private void AddSetting()
    {
        DbSettings.Add(new Setting { Key = "NewKey", Value = "" });
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            IsLoading = true;
            // Save each setting individually using the available interface methods
            foreach (var setting in DbSettings)
            {
                await _settingsService.SetSettingAsync(setting.Key, setting.Value);
            }
            await _dialogService.ShowInfoDialogAsync("Success", "Settings saved successfully!");
            _logger.LogInformation("Database settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving settings");
            await _dialogService.ShowErrorDialogAsync("Error", $"Error saving settings: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
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
            await _exportService.ExportTasksToPdfAsync(sampleTasks, PdfSettings);
            await _dialogService.ShowInfoDialogAsync("Success", "Preview PDF generated successfully! Check your desktop for the HTML file.");
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

    public bool HasDbSettings => DbSettings.Any();
}

public class AppSettingItem
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}