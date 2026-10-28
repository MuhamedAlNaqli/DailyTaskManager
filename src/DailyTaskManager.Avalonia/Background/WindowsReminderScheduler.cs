using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DailyTaskManager.Core.Interfaces;

namespace DailyTaskManager.Avalonia.Background;

/// <summary>
/// Registers a Windows scheduled task that runs this exe with <c>--reminder-check</c> every half hour while the
/// user is signed in. No process stays resident: the task starts, shows a notification if needed and exits.
/// </summary>
public class WindowsReminderScheduler : IBackgroundReminderService
{
    private const string TaskName = @"DailyTaskManager\Reminder";
    private const int TaskDefinitionVersion = 1;
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(20);

    private readonly ILogger<WindowsReminderScheduler> _logger;

    public WindowsReminderScheduler(ILogger<WindowsReminderScheduler> logger)
    {
        _logger = logger;
    }

    public bool IsSupported => OperatingSystem.IsWindows() && !string.IsNullOrEmpty(Environment.ProcessPath)
        && !string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase);

    public async Task<bool> IsInstalledAsync()
    {
        if (!IsSupported) return false;
        return (await RunSchtasksAsync("/Query", "/TN", TaskName)).ExitCode == 0;
    }

    public async Task SetInstalledAsync(bool installed)
    {
        if (!IsSupported) return;

        if (!installed)
        {
            if (await IsInstalledAsync()) await RunSchtasksAsync("/Delete", "/TN", TaskName, "/F");
            var current = ReminderState.Load();
            current.TaskMarker = "";
            current.Save();
            return;
        }

        // Registering is only needed once per install location and task definition
        var marker = Environment.ProcessPath + "|" + TaskDefinitionVersion;
        var state = ReminderState.Load();
        if (state.TaskMarker == marker && await IsInstalledAsync()) return;

        var xmlPath = Path.Combine(Path.GetTempPath(), $"dtm_reminder_{Guid.NewGuid():N}.xml");
        try
        {
            // schtasks insists on UTF-16 for task definitions
            await File.WriteAllTextAsync(xmlPath, BuildTaskXml(Environment.ProcessPath!, WindowsIdentity.GetCurrent().Name), Encoding.Unicode);
            var result = await RunSchtasksAsync("/Create", "/TN", TaskName, "/XML", xmlPath, "/F");
            if (result.ExitCode != 0)
            {
                _logger.LogWarning("Could not register the reminder task: {Output}", result.Output);
                return;
            }

            state.TaskMarker = marker;
            state.Save();
        }
        finally
        {
            try { File.Delete(xmlPath); } catch { /* temp file, harmless */ }
        }
    }

    public Task SendTestNotificationAsync()
    {
        if (!IsSupported) return Task.CompletedTask;

        // Same path as the real reminder, so the test shows exactly what the user will get
        Process.Start(new ProcessStartInfo(Environment.ProcessPath!, ReminderRunner.TestArgument)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = AppContext.BaseDirectory
        });
        return Task.CompletedTask;
    }

    public static string BuildTaskXml(string exePath, string userId)
    {
        var startBoundary = DateTime.Today.ToString("yyyy-MM-dd'T'HH:mm:ss");
        var user = SecurityElement.Escape(userId);
        return $@"<?xml version=""1.0"" encoding=""UTF-16""?>
<Task version=""1.4"" xmlns=""http://schemas.microsoft.com/windows/2004/02/mit/task"">
  <RegistrationInfo>
    <Description>Reminds you to log your hours in Daily Task Manager, even when the app is closed.</Description>
  </RegistrationInfo>
  <Triggers>
    <LogonTrigger>
      <Enabled>true</Enabled>
      <UserId>{user}</UserId>
      <Delay>PT2M</Delay>
    </LogonTrigger>
    <TimeTrigger>
      <Enabled>true</Enabled>
      <StartBoundary>{startBoundary}</StartBoundary>
      <Repetition>
        <Interval>PT30M</Interval>
      </Repetition>
    </TimeTrigger>
  </Triggers>
  <Principals>
    <Principal id=""Author"">
      <UserId>{user}</UserId>
      <LogonType>InteractiveToken</LogonType>
      <RunLevel>LeastPrivilege</RunLevel>
    </Principal>
  </Principals>
  <Settings>
    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
    <StartWhenAvailable>true</StartWhenAvailable>
    <ExecutionTimeLimit>PT3M</ExecutionTimeLimit>
    <Hidden>true</Hidden>
    <Enabled>true</Enabled>
  </Settings>
  <Actions Context=""Author"">
    <Exec>
      <Command>{SecurityElement.Escape(exePath)}</Command>
      <Arguments>{ReminderRunner.CheckArgument}</Arguments>
      <WorkingDirectory>{SecurityElement.Escape(Path.GetDirectoryName(exePath))}</WorkingDirectory>
    </Exec>
  </Actions>
</Task>";
    }

    private static async Task<(int ExitCode, string Output)> RunSchtasksAsync(params string[] arguments)
    {
        var info = new ProcessStartInfo("schtasks.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);

        using var process = Process.Start(info);
        if (process == null) return (-1, "schtasks could not be started");

        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new System.Threading.CancellationTokenSource(ProcessTimeout);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(); } catch { /* already gone */ }
            return (-1, "schtasks timed out");
        }

        return (process.ExitCode, (await output + await error).Trim());
    }
}
