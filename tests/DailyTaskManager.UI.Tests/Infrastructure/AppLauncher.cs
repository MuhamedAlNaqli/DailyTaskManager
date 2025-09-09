using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using System.Diagnostics;
using System.Threading;

namespace DailyTaskManager.UI.Tests.Infrastructure;

/// <summary>
/// Handles launching and managing the Daily Task Manager application for UI tests
/// </summary>
public class AppLauncher : IDisposable
{
    private Process? _process;
    private UIA3Automation? _automation;
    private FlaUI.Core.Application? _application;
    private static readonly Mutex AppLaunchMutex = new(false, "DailyTaskManager.UI.Tests.AppLaunch");

    public const string APP_EXE_PATH = @"D:\Internal Projects\DailyTaskManager\src\DailyTaskManager.Avalonia\bin\Debug\net8.0\DailyTaskManager.Avalonia.exe";
    public const int STARTUP_TIMEOUT_MS = 10000;
    public const int DEFAULT_TIMEOUT_MS = 5000;

    /// <summary>
    /// Gets the main automation driver
    /// </summary>
    public UIA3Automation Automation => _automation ?? throw new InvalidOperationException("Application not launched. Call LaunchAsync first.");

    /// <summary>
    /// Gets the FlaUI application instance
    /// </summary>
    public FlaUI.Core.Application Application => _application ?? throw new InvalidOperationException("Application not launched. Call LaunchAsync first.");

    /// <summary>
    /// Launches the Daily Task Manager application with mutex synchronization
    /// </summary>
    public async Task<Window> LaunchAsync()
    {
        // Use mutex to ensure only one test launches the app at a time
        if (!AppLaunchMutex.WaitOne(120000)) // 2 minute timeout (increased for longer test sequences)
        {
            throw new TimeoutException("Could not acquire application launch mutex within timeout");
        }

        try
        {
            Console.WriteLine($"🔒 Acquired launch mutex for test thread {Thread.CurrentThread.ManagedThreadId}");
            
            // Clean up any existing instances more aggressively
            await KillAllInstancesAsync();
            Dispose();

            // Initialize automation
            _automation = new UIA3Automation();

            // Launch the application with better error handling
            Console.WriteLine($"🚀 Attempting to launch application at: {APP_EXE_PATH}");
            
            try
            {
                _application = FlaUI.Core.Application.Launch(APP_EXE_PATH);
                Console.WriteLine($"✅ Application process started. Process ID: {_application.ProcessId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to launch application: {ex.Message}");
                throw;
            }
            
            // Give the process a moment to initialize
            await Task.Delay(3000);
            
            // Wait for the main window to appear
            var mainWindow = await WaitForMainWindowAsync();
            
            return mainWindow;
        }
        finally
        {
            try
            {
                Console.WriteLine($"🔓 Releasing launch mutex for test thread {Thread.CurrentThread.ManagedThreadId}");
                AppLaunchMutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // Mutex was already released or never acquired, which is fine
                Console.WriteLine($"⚠️ Mutex was already released for thread {Thread.CurrentThread.ManagedThreadId}");
            }
        }
    }

    /// <summary>
    /// Waits for the main application window to appear
    /// </summary>
    public async Task<Window> WaitForMainWindowAsync()
    {
        var startTime = DateTime.Now;
        
        while ((DateTime.Now - startTime).TotalMilliseconds < STARTUP_TIMEOUT_MS)
        {
            try
            {
                // Try to get the login window first (appears on startup)
                var windows = Application.GetAllTopLevelWindows(Automation);
                Console.WriteLine($"🔍 Found {windows.Length} windows at {DateTime.Now:HH:mm:ss.fff}");
                
                // Also check if the process is still running
                if (Application.HasExited)
                {
                    Console.WriteLine($"❌ Application process has exited unexpectedly!");
                    try 
                    {
                        Console.WriteLine($"Exit code: {Application.ExitCode}");
                    }
                    catch { /* ignore */ }
                    throw new InvalidOperationException("Application process exited during startup");
                }
                
                foreach (var window in windows)
                {
                    var title = window.Title ?? "NULL";
                    var automationId = "N/A";
                    
                    // Try to get AutomationId safely
                    try
                    {
                        automationId = window.AutomationId ?? "NULL";
                    }
                    catch (Exception)
                    {
                        // AutomationId property is not supported, which is fine
                    }
                    
                    Console.WriteLine($"  📋 Window: '{title}' (AutomationId: '{automationId}', Available: {window.IsAvailable})");
                    
                    // Check for login window or main window - be more permissive
                    if (!string.IsNullOrEmpty(title) && (
                        title.Contains("Login", StringComparison.OrdinalIgnoreCase) || 
                        title.Contains("Daily Task Manager", StringComparison.OrdinalIgnoreCase) ||
                        title.Contains("DailyTaskManager", StringComparison.OrdinalIgnoreCase)))
                    {
                        Console.WriteLine($"✅ Found target window: '{title}' - checking availability...");
                        
                        // Ensure window is actually available before returning
                        if (window.IsAvailable)
                        {
                            Console.WriteLine($"✅ Window is available, returning: '{title}'");
                            return window;
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Window found but not available yet: '{title}'");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Exception while waiting for window: {ex.Message}");
            }

            await Task.Delay(1000); // Increased delay for more stable detection
        }

        throw new TimeoutException($"Main window did not appear within {STARTUP_TIMEOUT_MS}ms");
    }

    /// <summary>
    /// Gets the currently active main window
    /// </summary>
    public Window GetMainWindow()
    {
        var windows = Application.GetAllTopLevelWindows(Automation);
        
        foreach (var window in windows)
        {
            if (window.Title.Contains("Daily Task Manager") || 
                window.AutomationId == "MainWindow")
            {
                return window;
            }
        }
        
        throw new InvalidOperationException("Main window not found");
    }

    /// <summary>
    /// Gets the login window if it's currently displayed
    /// </summary>
    public Window? GetLoginWindow()
    {
        var windows = Application.GetAllTopLevelWindows(Automation);
        
        return windows.FirstOrDefault(w => 
            w.Title.Contains("Login") || 
            w.AutomationId == "LoginWindow");
    }

    /// <summary>
    /// Takes a screenshot of the current application state
    /// </summary>
    public void TakeScreenshot(string filename = "screenshot.png")
    {
        try
        {
            Console.WriteLine($"📸 Screenshot requested: {filename}");
            // For now, just log the request - actual screenshot implementation can be added later
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to take screenshot: {ex.Message}");
        }
    }

    /// <summary>
    /// Prints the UI tree structure for debugging
    /// </summary>
    public void PrintUITree(AutomationElement? element = null, int depth = 0)
    {
        try
        {
            if (element == null)
            {
                var windows = Application.GetAllTopLevelWindows(Automation);
                foreach (var window in windows)
                {
                    PrintUITree(window, 0);
                }
                return;
            }

            var indent = new string(' ', depth * 2);
            var automationId = "N/A";
            
            // Try to get AutomationId safely
            try
            {
                automationId = element.AutomationId ?? "NULL";
            }
            catch (Exception)
            {
                // AutomationId property is not supported, which is fine
            }
            
            Console.WriteLine($"{indent}🔹 {element.ControlType}: '{element.Name}' (AutomationId: '{automationId}')");

            try
            {
                var children = element.FindAllChildren();
                foreach (var child in children)
                {
                    PrintUITree(child, depth + 1);
                }
            }
            catch
            {
                // Some elements may not have accessible children
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error printing UI tree: {ex.Message}");
        }
    }

    /// <summary>
    /// Kills any running instances of DailyTaskManager
    /// </summary>
    public static void KillAllInstances()
    {
        try
        {
            var processes = Process.GetProcessesByName("DailyTaskManager.Avalonia");
            foreach (var process in processes)
            {
                Console.WriteLine($"🔪 Killing process {process.Id}");
                process.Kill();
                process.WaitForExit(2000);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error killing processes: {ex.Message}");
        }
    }

    /// <summary>
    /// Asynchronous version of KillAllInstances with better cleanup
    /// </summary>
    public static async Task KillAllInstancesAsync()
    {
        try
        {
            var processes = Process.GetProcessesByName("DailyTaskManager.Avalonia");
            Console.WriteLine($"🔍 Found {processes.Length} DailyTaskManager processes to kill");
            
            var killTasks = new List<Task>();
            
            foreach (var process in processes)
            {
                var killTask = Task.Run(() =>
                {
                    try
                    {
                        Console.WriteLine($"🔪 Killing process {process.Id}");
                        process.Kill();
                        
                        // Wait for the process to exit, but don't block indefinitely
                        var waitTask = Task.Run(() => process.WaitForExit());
                        if (!waitTask.Wait(5000)) // 5 second timeout
                        {
                            Console.WriteLine($"⚠️ Process {process.Id} did not exit within timeout, but continuing...");
                        }
                        else
                        {
                            Console.WriteLine($"✅ Process {process.Id} exited cleanly");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Error killing process {process.Id}: {ex.Message}");
                    }
                    finally
                    {
                        process.Dispose();
                    }
                });
                
                killTasks.Add(killTask);
            }
            
            // Wait for all kill operations to complete, but timeout after 10 seconds
            await Task.WhenAll(killTasks).WaitAsync(TimeSpan.FromSeconds(10));
            
            // Give the OS time to clean up
            await Task.Delay(1000);
            
            // Verify cleanup
            var remainingProcesses = Process.GetProcessesByName("DailyTaskManager.Avalonia");
            if (remainingProcesses.Length > 0)
            {
                Console.WriteLine($"⚠️ Warning: {remainingProcesses.Length} processes still running after cleanup");
            }
            else
            {
                Console.WriteLine($"✅ All DailyTaskManager processes cleaned up successfully");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in KillAllInstancesAsync: {ex.Message}");
        }
    }

    public void Dispose()
    {
        try
        {
            _application?.Close();
            _application?.Dispose();
        }
        catch { }

        try
        {
            _automation?.Dispose();
        }
        catch { }

        _process = null;
        _application = null;
        _automation = null;
    }
}