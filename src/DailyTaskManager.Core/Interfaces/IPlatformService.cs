using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DailyTaskManager.Core.Interfaces
{
    /// <summary>
    /// Interface for platform-specific operations (file dialogs, clipboard, etc.)
    /// </summary>
    public interface IPlatformService
    {
        /// <summary>
        /// Shows a file save dialog
        /// </summary>
        Task<string?> ShowSaveFileDialogAsync(string title, string defaultFileName, Dictionary<string, List<string>> fileTypes);

        /// <summary>
        /// Shows a file open dialog
        /// </summary>
        Task<string?> ShowOpenFileDialogAsync(string title, Dictionary<string, List<string>> fileTypes);

        /// <summary>
        /// Shows a folder picker dialog
        /// </summary>
        Task<string?> ShowFolderPickerDialogAsync(string title);

        /// <summary>
        /// Opens a file with the default application
        /// </summary>
        Task<bool> OpenFileWithDefaultAppAsync(string filePath);

        /// <summary>
        /// Opens a URL in the default browser
        /// </summary>
        Task<bool> OpenUrlAsync(string url);

        /// <summary>
        /// Copies text to the clipboard
        /// </summary>
        Task SetClipboardTextAsync(string text);

        /// <summary>
        /// Gets text from the clipboard
        /// </summary>
        Task<string?> GetClipboardTextAsync();

        /// <summary>
        /// Shows a system notification
        /// </summary>
        Task ShowNotificationAsync(string title, string message, TimeSpan? duration = null);

        /// <summary>
        /// Gets the current platform name
        /// </summary>
        string GetPlatformName();

        /// <summary>
        /// Indicates if the platform supports speech recognition
        /// </summary>
        bool SupportsSpeechRecognition { get; }
    }
}