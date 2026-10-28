using System;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;

namespace DailyTaskManager.Avalonia.Services;

/// <summary>
/// Hands unused memory back to Windows once the app has settled or is minimized, which keeps the footprint
/// shown in Task Manager small for an app that is mostly left open in the background
/// </summary>
internal static class MemoryTrimmer
{
    private static readonly TimeSpan SettleDelay = TimeSpan.FromSeconds(6);
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(20);

    public static void Attach(Window window)
    {
        DispatcherTimer? idleTimer = null;

        window.Opened += (_, _) => TrimLater(SettleDelay);
        window.PropertyChanged += (_, e) =>
        {
            if (e.Property == Window.WindowStateProperty && window.WindowState == WindowState.Minimized) TrimLater(TimeSpan.FromSeconds(1));
        };

        // Working in another program for a while: give the memory back, unless the user returns first
        window.Deactivated += (_, _) =>
        {
            idleTimer?.Stop();
            idleTimer = new DispatcherTimer { Interval = IdleDelay };
            idleTimer.Tick += (_, _) =>
            {
                idleTimer?.Stop();
                _ = Task.Run(Trim);
            };
            idleTimer.Start();
        };
        window.Activated += (_, _) => idleTimer?.Stop();
    }

    private static void TrimLater(TimeSpan delay)
    {
        DispatcherTimer.RunOnce(() => _ = Task.Run(Trim), delay);
    }

    public static void Trim()
    {
        try
        {
            var before = GC.GetTotalMemory(false);
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            if (OperatingSystem.IsWindows())
            {
                using var self = Process.GetCurrentProcess();
                SetProcessWorkingSetSize(self.Handle, (IntPtr)(-1), (IntPtr)(-1));
                self.Refresh();
                Serilog.Log.Information("Memory trimmed: managed heap {Before} -> {After} MB, working set {WorkingSet} MB, private {Private} MB",
                    before / 1048576, GC.GetTotalMemory(false) / 1048576, self.WorkingSet64 / 1048576, self.PrivateMemorySize64 / 1048576);
            }
        }
        catch
        {
            // Purely an optimization
        }
    }

    [DllImport("kernel32.dll")]
    private static extern bool SetProcessWorkingSetSize(IntPtr process, IntPtr minimum, IntPtr maximum);
}
