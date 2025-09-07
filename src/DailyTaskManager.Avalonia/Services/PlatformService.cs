using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using DailyTaskManager.Core.Interfaces;

namespace DailyTaskManager.Avalonia.Services;

/// <summary>
/// Platform-specific service implementations for Avalonia
/// </summary>
public class PlatformService : IPlatformService
{
    private readonly ILogger<PlatformService> _logger;
    public PlatformService(ILogger<PlatformService> logger)
    {
        _logger = logger;
    }

    public async Task<string?> ShowSaveFileDialogAsync(string title, string defaultFileName, Dictionary<string, List<string>> fileTypes)
    {
        try
        {
            var topLevel = GetTopLevel();
            if (topLevel?.StorageProvider == null) return null;

            var options = new FilePickerSaveOptions
            {
                Title = title,
                SuggestedFileName = defaultFileName,
                FileTypeChoices = ConvertFileTypes(fileTypes)
            };

            var result = await topLevel.StorageProvider.SaveFilePickerAsync(options);
            return result?.Path.LocalPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing save file dialog");
            return null;
        }
    }

    public async Task<string?> ShowOpenFileDialogAsync(string title, Dictionary<string, List<string>> fileTypes)
    {
        try
        {
            var topLevel = GetTopLevel();
            if (topLevel?.StorageProvider == null) return null;

            var options = new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = ConvertFileTypes(fileTypes)
            };

            var result = await topLevel.StorageProvider.OpenFilePickerAsync(options);
            return result.FirstOrDefault()?.Path.LocalPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing open file dialog");
            return null;
        }
    }

    public async Task<string?> ShowFolderPickerDialogAsync(string title)
    {
        try
        {
            var topLevel = GetTopLevel();
            if (topLevel?.StorageProvider == null) return null;

            var options = new FolderPickerOpenOptions
            {
                Title = title,
                AllowMultiple = false
            };

            var result = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
            return result.FirstOrDefault()?.Path.LocalPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing folder picker dialog");
            return null;
        }
    }

    public async Task<bool> OpenFileWithDefaultAppAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return false;

            var topLevel = GetTopLevel();
            if (topLevel == null) return false;

            // Use system default for file opening
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening file with default app: {FilePath}", filePath);
            return false;
        }
    }

    public async Task<bool> OpenUrlAsync(string url)
    {
        try
        {
            var topLevel = GetTopLevel();
            if (topLevel == null) return false;

            // Use system default for URL opening
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening URL: {Url}", url);
            return false;
        }
    }

    public async Task SetClipboardTextAsync(string text)
    {
        try
        {
            var topLevel = GetTopLevel();
            if (topLevel?.Clipboard == null) return;

            await topLevel.Clipboard.SetTextAsync(text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting clipboard text");
        }
    }

    public async Task<string?> GetClipboardTextAsync()
    {
        try
        {
            var topLevel = GetTopLevel();
            if (topLevel?.Clipboard == null) return null;

            return await topLevel.Clipboard.GetTextAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting clipboard text");
            return null;
        }
    }

    public async Task ShowNotificationAsync(string title, string message, TimeSpan? duration = null)
    {
        try
        {
            // For now, use logging as notification system (can be enhanced later with native notifications)
            _logger.LogInformation("Notification: {Title} - {Message}", title, message);
            
            // TODO: Implement native notifications using DesktopNotifications.Avalonia
            // This provides a clean interface for future native notification implementation
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing notification: {Title} - {Message}", title, message);
        }
    }

    public string GetPlatformName()
    {
        return Environment.OSVersion.Platform switch
        {
            PlatformID.Win32NT => "Windows",
            PlatformID.Unix => "Linux",
            PlatformID.MacOSX => "macOS",
            _ => "Unknown"
        };
    }

    public bool SupportsSpeechRecognition => GetPlatformName() == "Windows";

    private TopLevel? GetTopLevel()
    {
        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return TopLevel.GetTopLevel(desktop.MainWindow);
        }
        return null;
    }

    private List<FilePickerFileType> ConvertFileTypes(Dictionary<string, List<string>> fileTypes)
    {
        var result = new List<FilePickerFileType>();

        foreach (var (name, extensions) in fileTypes)
        {
            result.Add(new FilePickerFileType(name)
            {
                Patterns = extensions.ToArray()
            });
        }

        return result;
    }
}