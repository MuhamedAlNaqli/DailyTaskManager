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
    private readonly DailyTaskManagerDbContext _context;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(DailyTaskManagerDbContext context, ILogger<SettingsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public event EventHandler<(string Key, object? Value)>? SettingChanged;

    public async Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default)
    {
        try
        {
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
            var settingsJson = JsonConvert.SerializeObject(settings, Formatting.Indented);
            await SetSettingAsync("PdfSettings", settingsJson, "PDF export settings");
            
            _logger.LogDebug("PDF settings saved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving PDF settings");
            throw;
        }
    }

    public async Task ResetToDefaultsAsync()
    {
        try
        {
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