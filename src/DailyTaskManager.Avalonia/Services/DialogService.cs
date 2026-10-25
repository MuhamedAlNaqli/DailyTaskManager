using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using DailyTaskManager.Core.Models;
using DailyTaskManager.Avalonia.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace DailyTaskManager.Avalonia.Services;

/// <summary>
/// Service for showing dialogs and modal windows
/// </summary>
public class DialogService
{
    private readonly ILogger<DialogService> _logger;

    public DialogService(ILogger<DialogService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Shows an information dialog
    /// </summary>
    public async Task ShowInfoDialogAsync(string title, string message)
    {
        try
        {
            _logger.LogInformation("INFO: {Title} - {Message}", title, message);

            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                await ShowMessageBoxAsync(mainWindow, title, message, Icon.Info);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing info dialog");
        }
    }

    /// <summary>
    /// Shows an error dialog
    /// </summary>
    public async Task ShowErrorDialogAsync(string title, string message)
    {
        try
        {
            _logger.LogError("ERROR: {Title} - {Message}", title, message);
            
            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                await ShowMessageBoxAsync(mainWindow, title, message, Icon.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing error dialog");
        }
    }

    /// <summary>
    /// Shows a confirmation dialog
    /// </summary>
    public async Task<bool> ShowConfirmDialogAsync(string title, string message)
    {
        try
        {
            _logger.LogInformation("CONFIRM: {Title} - {Message}", title, message);
            
            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                var result = await ShowMessageBoxAsync(mainWindow, title, message, Icon.Question, 
                    ButtonEnum.YesNo);
                return result == ButtonResult.Yes;
            }
            
            // Fallback to safe default
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing confirmation dialog");
            return false;
        }
    }

    /// <summary>
    /// Shows the Add Task dialog
    /// </summary>
    public async Task<DailyTask?> ShowAddTaskDialogAsync()
    {
        try
        {
            _logger.LogDebug("Add task dialog requested");
            
            var viewModel = App.GetService<AddTaskViewModel>();
            var dialog = new Views.AddTaskDialog
            {
                DataContext = viewModel
            };

            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }

            var result = await dialog.ShowDialog<bool>(mainWindow);
            
            if (result)
            {
                return viewModel.CreateTask();
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing add task dialog");
            return null;
        }
    }

    /// <summary>
    /// Shows the Edit Task dialog
    /// </summary>
    public async Task<DailyTask?> ShowEditTaskDialogAsync(DailyTask task)
    {
        try
        {
            _logger.LogDebug("Edit task dialog requested for task {TaskId}", task.Id);
            
            var viewModel = App.GetService<EditTaskViewModel>();
            viewModel.LoadTask(task);
            
            var dialog = new Views.EditTaskDialog
            {
                DataContext = viewModel
            };

            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }

            var result = await dialog.ShowDialog<bool>(mainWindow);
            
            if (result)
            {
                return viewModel.GetUpdatedTask();
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing edit task dialog");
            return null;
        }
    }

    /// <summary>
    /// Shows a folder selection dialog
    /// </summary>
    public async Task<string?> ShowFolderDialogAsync(string title, string? initialFolder = null)
    {
        try
        {
            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("No main window available for folder dialog");
                return null;
            }

            var storageProvider = mainWindow.StorageProvider;
            if (storageProvider == null)
            {
                _logger.LogWarning("No storage provider available");
                return null;
            }

            var options = new FolderPickerOpenOptions
            {
                Title = title,
                AllowMultiple = false
            };

            // Set initial folder if provided
            if (!string.IsNullOrEmpty(initialFolder))
            {
                try
                {
                    var initialFolderUri = await storageProvider.TryGetFolderFromPathAsync(initialFolder);
                    if (initialFolderUri != null)
                    {
                        options.SuggestedStartLocation = initialFolderUri;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not set initial folder: {Folder}", initialFolder);
                }
            }

            var result = await storageProvider.OpenFolderPickerAsync(options);

            if (result.Count > 0)
            {
                var selectedFolder = result[0];
                var localPath = selectedFolder.TryGetLocalPath();
                _logger.LogDebug("Folder selected: {Path}", localPath);
                return localPath;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing folder dialog");
            return null;
        }
    }

    private Window? GetMainWindow()
    {
        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }

    /// <summary>
    /// Helper method to show a message box using MessageBox.Avalonia
    /// </summary>
    private async Task<ButtonResult> ShowMessageBoxAsync(Window? parentWindow, string title, string message, 
        Icon icon = Icon.Info, ButtonEnum buttons = ButtonEnum.Ok)
    {
        try
        {
            var messageBoxStandardWindow = MessageBoxManager
                .GetMessageBoxStandard(title, message, buttons, icon);

            if (parentWindow != null)
            {
                return await messageBoxStandardWindow.ShowWindowDialogAsync(parentWindow);
            }
            else
            {
                return await messageBoxStandardWindow.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing message box");
            return ButtonResult.None;
        }
    }
}