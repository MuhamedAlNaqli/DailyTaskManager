using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Models;
using DailyTaskManager.Infrastructure.Data;

namespace DailyTaskManager.Infrastructure.Services;

/// <summary>
/// Service for application settings management
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly IDbContextFactory<DailyTaskManagerDbContext> _contextFactory;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(IDbContextFactory<DailyTaskManagerDbContext> contextFactory, ILogger<SettingsService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public event EventHandler<(string Key, object? Value)>? SettingChanged;

    public async Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default)
    {
        try
        {
            await using var _context = _contextFactory.CreateDbContext();
            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
            {
                _logger.LogDebug("Setting {Key} not found, returning default value", key);
                return defaultValue;
            }

            return setting.GetValue<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting setting {Key}", key);
            return defaultValue;
        }
    }

    public async Task SetSettingAsync<T>(string key, T value, string? description = null)
    {
        try
        {
            await using var _context = _contextFactory.CreateDbContext();
            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
            {
                setting = new Setting
                {
                    Key = key,
                    Description = description,
                    IsSystem = false
                };
                _context.Settings.Add(setting);
            }

            setting.SetValue(value);
            if (!string.IsNullOrEmpty(description))
            {
                setting.Description = description;
            }

            await _context.SaveChangesAsync();

            SettingChanged?.Invoke(this, (key, value));
            _logger.LogDebug("Setting {Key} updated to {Value}", key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting {Key} to {Value}", key, value);
            throw;
        }
    }

    public async Task<List<Setting>> GetAllSettingsAsync()
    {
        try
        {
            await using var _context = _contextFactory.CreateDbContext();
            return await _context.Settings
                .OrderBy(s => s.Key)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all settings");
            return new List<Setting>();
        }
    }

    public async Task<bool> RemoveSettingAsync(string key)
    {
        try
        {
            await using var _context = _contextFactory.CreateDbContext();
            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
            {
                return false;
            }

            if (setting.IsSystem)
            {
                _logger.LogWarning("Attempted to remove system setting {Key}", key);
                return false;
            }

            _context.Settings.Remove(setting);
            await _context.SaveChangesAsync();

            SettingChanged?.Invoke(this, (key, null));
            _logger.LogDebug("Setting {Key} removed", key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing setting {Key}", key);
            return false;
        }
    }

    public async Task<bool> SettingExistsAsync(string key)
    {
        try
        {
            await using var _context = _contextFactory.CreateDbContext();
            return await _context.Settings
                .AnyAsync(s => s.Key == key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if setting {Key} exists", key);
            return false;
        }
    }

    public async Task<PdfSettings> GetPdfSettingsAsync()
    {
        try
        {
            var settingsJson = await GetSettingAsync<string>("PdfSettings");
            
            if (string.IsNullOrEmpty(settingsJson))
            {
                // Return default settings
                return new PdfSettings();
            }

            var settings = JsonConvert.DeserializeObject<PdfSettings>(settingsJson);
            return settings ?? new PdfSettings();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PDF settings");
            return new PdfSettings();
        }
    }

    public async Task SavePdfSettingsAsync(PdfSettings settings)
    {
        try
        {
            var settingsJson = JsonConvert.SerializeObject(settings, Formatting.None);
            await SetSettingAsync("PdfSettings", settingsJson, "PDF export settings");

            _logger.LogDebug("PDF settings saved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving PDF settings");
            throw;
        }
    }

    public async Task<string> GetDownloadPathAsync()
    {
        try
        {
            var downloadPath = await GetSettingAsync<string>("DownloadPath");

            if (string.IsNullOrEmpty(downloadPath))
            {
                // Default to Desktop if no setting is found
                downloadPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                _logger.LogDebug("Using default download path: {Path}", downloadPath);
            }
            else
            {
                _logger.LogDebug("Using configured download path: {Path}", downloadPath);
            }

            return downloadPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting download path, using default");
            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }
    }

    public async Task<string> GetPdfExportPathAsync()
    {
        try
        {
            var path = await GetSettingAsync<string>("PdfExportPath");
            if (string.IsNullOrEmpty(path))
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                _logger.LogDebug("Using default PDF export path: {Path}", path);
            }
            return path;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PDF export path, using default");
            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }
    }

    public async Task<string> GetWordExportPathAsync()
    {
        try
        {
            var path = await GetSettingAsync<string>("WordExportPath");
            if (string.IsNullOrEmpty(path))
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                _logger.LogDebug("Using default Word export path: {Path}", path);
            }
            return path;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Word export path, using default");
            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }
    }

    public async Task<string> GetExcelExportPathAsync()
    {
        try
        {
            var path = await GetSettingAsync<string>("ExcelExportPath");
            if (string.IsNullOrEmpty(path))
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                _logger.LogDebug("Using default Excel export path: {Path}", path);
            }
            return path;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Excel export path, using default");
            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }
    }

    public async Task ResetToDefaultsAsync()
    {
        try
        {
            await using var _context = _contextFactory.CreateDbContext();
            // Remove all non-system settings
            var userSettings = await _context.Settings
                .Where(s => !s.IsSystem)
                .ToListAsync();

            _context.Settings.RemoveRange(userSettings);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Reset {Count} user settings to defaults", userSettings.Count);

            // Notify about reset
            SettingChanged?.Invoke(this, ("*", null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting settings to defaults");
            throw;
        }
    }
}