using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Core.Interfaces
{
    /// <summary>
    /// Interface for application settings management
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// Gets a setting value by key
        /// </summary>
        Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default);

        /// <summary>
        /// Sets a setting value by key
        /// </summary>
        Task SetSettingAsync<T>(string key, T value, string? description = null);

        /// <summary>
        /// Gets all settings
        /// </summary>
        Task<List<Setting>> GetAllSettingsAsync();

        /// <summary>
        /// Removes a setting by key
        /// </summary>
        Task<bool> RemoveSettingAsync(string key);

        /// <summary>
        /// Checks if a setting exists
        /// </summary>
        Task<bool> SettingExistsAsync(string key);

        /// <summary>
        /// Gets PDF export settings
        /// </summary>
        Task<PdfSettings> GetPdfSettingsAsync();

        /// <summary>
        /// Saves PDF export settings
        /// </summary>
        Task SavePdfSettingsAsync(PdfSettings settings);

        /// <summary>
        /// Gets the configured download path for exports
        /// </summary>
        Task<string> GetDownloadPathAsync();

        /// <summary>
        /// Gets the configured PDF export path
        /// </summary>
        Task<string> GetPdfExportPathAsync();

        /// <summary>
        /// Gets the configured Word export path
        /// </summary>
        Task<string> GetWordExportPathAsync();

        /// <summary>
        /// Gets the configured Excel export path
        /// </summary>
        Task<string> GetExcelExportPathAsync();

        /// <summary>
        /// Resets all settings to default values
        /// </summary>
        Task ResetToDefaultsAsync();

        /// <summary>
        /// Event raised when a setting changes
        /// </summary>
        event EventHandler<(string Key, object? Value)>? SettingChanged;
    }
}