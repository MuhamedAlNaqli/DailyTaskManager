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

            // Add connection string section
            var connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "(missing)";
            AppSettings.Add(new AppSettingItem { Key = "Database Connection String", Value = connectionString });

            // Parse and display connection details
            if (!string.IsNullOrEmpty(connectionString) && connectionString != "(missing)")
            {
                try
                {
                    var connectionParts = connectionString.Split(';');
                    foreach (var part in connectionParts)
                    {
                        var keyValue = part.Split('=');
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
                        // Mask sensitive information like client secrets
                        if (setting.Key.ToLower().Contains("secret"))
                        {
                            value = value.Length > 0 ? "***configured***" : "Not configured";
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