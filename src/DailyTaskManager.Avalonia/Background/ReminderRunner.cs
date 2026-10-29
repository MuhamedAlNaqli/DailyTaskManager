using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Reminders;

namespace DailyTaskManager.Avalonia.Background;

/// <summary>
/// Entry point of the headless background process (<c>--reminder-check</c>): started by the Windows task scheduler,
/// it shows at most a notification (missing hours, or a new version) and exits, so nothing stays in memory between checks
/// </summary>
internal static class ReminderRunner
{
    public const string CheckArgument = "--reminder-check";
    public const string TestArgument = "--reminder-test";

    /// <summary>Diagnostics: shows the "update available" notification as it would look</summary>
    public const string UpdateTestArgument = "--update-test";

    /// <summary>Diagnostics: <c>--update-check-as=3.1.0</c> checks the feed as if that version were installed, ignoring the schedule</summary>
    public const string UpdateCheckAsPrefix = "--update-check-as=";

    private const string DefaultAppCastUrl = "https://github.com/MuhamedAlNaqli/DailyTaskManager/releases/latest/download/appcast.xml";

    private static readonly TimeSpan NotificationTimeout = TimeSpan.FromSeconds(30);

    public static bool IsReminderRun(string[] args) =>
        Array.Exists(args, a => a == CheckArgument || a == TestArgument || a == UpdateTestArgument || a.StartsWith(UpdateCheckAsPrefix, StringComparison.Ordinal));

    public static int Run(IServiceProvider services, string[] args)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("ReminderRunner");
        try
        {
            var exePath = Environment.ProcessPath ?? "";
            if (Array.Exists(args, a => a == TestArgument))
            {
                ShowAndOpenIfClicked(logger, "Time to log your hours", "This is how your daily reminder looks. Click it to open Daily Task Manager.", exePath);
                return 0;
            }

            if (Array.Exists(args, a => a == UpdateTestArgument))
            {
                ShowAndOpenIfClicked(logger, "Update available", "This is how the update notice looks. Click it to open Daily Task Manager and install the new version.", exePath);
                return 0;
            }

            var checkAs = Array.Find(args, a => a.StartsWith(UpdateCheckAsPrefix, StringComparison.Ordinal));
            if (checkAs != null)
            {
                // Diagnostics only: no schedule, no reminder
                Version.TryParse(checkAs.Substring(UpdateCheckAsPrefix.Length), out var pretend);
                return CheckForUpdateAsync(services, logger, exePath, ReminderState.Load(), pretend).GetAwaiter().GetResult();
            }

            return RunCheckAsync(services, logger, exePath).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "The background reminder failed");
            return 1;
        }
    }

    private static async Task<int> RunCheckAsync(IServiceProvider services, ILogger logger, string exePath)
    {
        var state = ReminderState.Load();
        var now = DateTime.Now;

        if (state.UpdateCheckDue(now))
        {
            await CheckForUpdateAsync(services, logger, exePath, state, pretendVersion: null);
        }

        return await RemindAboutMissingHoursAsync(services, logger, exePath, state, now);
    }

    /// <summary>
    /// Tells the user about a new version, once per release (and again after a few days). It stays quiet while the app is
    /// open, because the app then offers the update itself.
    /// </summary>
    private static async Task<int> CheckForUpdateAsync(IServiceProvider services, ILogger logger, string exePath, ReminderState state, Version? pretendVersion)
    {
        var now = DateTime.Now;
        try
        {
            var current = pretendVersion ?? typeof(ReminderRunner).Assembly.GetName().Version ?? new Version(0, 0);
            var feed = services.GetRequiredService<IConfiguration>()["UpdateService:AppCastUrl"] is { Length: > 0 } configured ? configured : DefaultAppCastUrl;

            var update = await BackgroundUpdateChecker.FindNewerAsync(feed, current, TimeSpan.FromSeconds(15));
            state.LastUpdateCheck = now;
            state.Save();

            if (update == null)
            {
                logger.LogInformation("Update check: version {Current} is up to date", current);
                return 0;
            }

            if (!state.ShouldAnnounce(update.DisplayVersion, now) && pretendVersion == null) return 0;
            if (IsAppRunning(exePath))
            {
                logger.LogInformation("Update {Version} is available; the app is open and will offer it itself", update.DisplayVersion);
                return 0;
            }

            state.RecordAnnounced(update.DisplayVersion, now);
            state.Save();
            logger.LogInformation("Announcing update {Version}", update.DisplayVersion);
            ShowAndOpenIfClicked(logger, "Update available",
                $"Daily Task Manager {update.DisplayVersion} is ready. Click to open the app and install it.", exePath);
        }
        catch (Exception ex)
        {
            // Offline or the feed is down: try again at the next scheduled run
            logger.LogInformation("Update check skipped: {Reason}", ex.Message);
        }

        return 0;
    }

    private static bool IsAppRunning(string exePath)
    {
        if (string.IsNullOrEmpty(exePath)) return false;

        var name = Path.GetFileNameWithoutExtension(exePath);
        using var self = Process.GetCurrentProcess();
        foreach (var process in Process.GetProcessesByName(name))
        {
            using (process)
            {
                if (process.Id != self.Id) return true;
            }
        }

        return false;
    }

    private static async Task<int> RemindAboutMissingHoursAsync(IServiceProvider services, ILogger logger, string exePath, ReminderState state, DateTime now)
    {
        if (!state.CanNotify(now)) return 0;

        // The reminder belongs to the user who chose "remember me"; without a saved session there is nobody to remind.
        // The expiry is not extended, so the background job never keeps a session alive by itself.
        var auth = services.GetRequiredService<IAuthenticationService>();
        if (!(await auth.RestoreSessionAsync(extendExpiry: false)).IsSuccess)
        {
            logger.LogInformation("No saved session; skipping the reminder");
            return 0;
        }

        var status = await services.GetRequiredService<ReminderEvaluator>().EvaluateAsync(now);
        if (!status.IsDue || !status.BackgroundEnabled) return 0;

        state.Record(now);
        state.Save();

        var title = status.MissingDays.Count == 1 ? "Time to log your hours" : "Hours are missing";
        logger.LogInformation("Showing the reminder: {Message}", status.Message);
        ShowAndOpenIfClicked(logger, title, status.Message, exePath);
        return 0;
    }

    private static void ShowAndOpenIfClicked(ILogger logger, string title, string message, string exePath)
    {
        var clicked = TrayNotifier.Show(title, message, exePath, NotificationTimeout);
        if (TrayNotifier.LastFailure != null) logger.LogWarning("Notification problem: {Problem}", TrayNotifier.LastFailure);
        else logger.LogInformation("Notification closed (clicked: {Clicked})", clicked);
        if (!clicked || string.IsNullOrEmpty(exePath)) return;

        Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true, WorkingDirectory = AppContext.BaseDirectory });
    }
}
