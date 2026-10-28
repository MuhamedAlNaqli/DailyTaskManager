using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Reminders;

namespace DailyTaskManager.Avalonia.Background;

/// <summary>
/// Entry point of the headless reminder process (<c>--reminder-check</c>): started by the Windows task scheduler,
/// it shows at most a notification and exits, so nothing stays in memory between checks
/// </summary>
internal static class ReminderRunner
{
    public const string CheckArgument = "--reminder-check";
    public const string TestArgument = "--reminder-test";

    private static readonly TimeSpan NotificationTimeout = TimeSpan.FromSeconds(30);

    public static bool IsReminderRun(string[] args) =>
        Array.Exists(args, a => a == CheckArgument || a == TestArgument);

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
