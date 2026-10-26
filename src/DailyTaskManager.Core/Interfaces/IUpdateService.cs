using System;
using System.Threading.Tasks;

namespace DailyTaskManager.Core.Interfaces;

/// <summary>
/// Service for handling application updates
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Event fired when an update is available
    /// </summary>
    event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;

    /// <summary>
    /// Event fired when update check is completed
    /// </summary>
    event EventHandler<UpdateCheckCompletedEventArgs>? UpdateCheckCompleted;

    /// <summary>
    /// Initialize the update service
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Check for updates manually
    /// </summary>
    Task<bool> CheckForUpdatesAsync();

    /// <summary>
    /// Start automatic update checking
    /// </summary>
    void StartAutomaticUpdateChecking(TimeSpan interval);

    /// <summary>
    /// Stop automatic update checking
    /// </summary>
    void StopAutomaticUpdateChecking();

    /// <summary>
    /// Check for updates, report the result and show the update window when a newer version exists
    /// </summary>
    Task<UpdateCheckOutcome> CheckAndPromptAsync();

    /// <summary>
    /// Show update UI to user
    /// </summary>
    Task ShowUpdateUIAsync();

    /// <summary>
    /// Get current application version
    /// </summary>
    string GetCurrentVersion();

    /// <summary>
    /// Whether automatic updates are enabled
    /// </summary>
    bool AutoUpdatesEnabled { get; set; }
}

/// <summary>
/// Event args for update available event
/// </summary>
public class UpdateAvailableEventArgs : EventArgs
{
    public string NewVersion { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public bool IsCriticalUpdate { get; set; }
}

/// <summary>
/// Event args for update check completed event
/// </summary>
public class UpdateCheckCompletedEventArgs : EventArgs
{
    public bool UpdateAvailable { get; set; }
    public bool CheckSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result of an update check
/// </summary>
public enum UpdateCheckStatus
{
    UpToDate,
    UpdateAvailable,
    Failed
}

public class UpdateCheckOutcome
{
    public UpdateCheckStatus Status { get; set; }

    /// <summary>Latest version found on the update feed (when known)</summary>
    public string? Version { get; set; }

    /// <summary>Human readable summary suitable for showing to the user</summary>
    public string Message { get; set; } = string.Empty;

    public static UpdateCheckOutcome UpToDate(string currentVersion, string? extra = null) => new UpdateCheckOutcome
    {
        Status = UpdateCheckStatus.UpToDate,
        Version = currentVersion,
        Message = $"You are up to date (version {currentVersion})." + (extra == null ? string.Empty : " " + extra)
    };

    public static UpdateCheckOutcome Available(string version) => new UpdateCheckOutcome
    {
        Status = UpdateCheckStatus.UpdateAvailable,
        Version = version,
        Message = $"Version {version} is available."
    };

    public static UpdateCheckOutcome Failed(string message) => new UpdateCheckOutcome
    {
        Status = UpdateCheckStatus.Failed,
        Message = message
    };
}
