using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using DailyTaskManager.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using DailyTaskManager.Avalonia.Services;
using DailyTaskManager.Avalonia.Views;
using DailyTaskManager.Avalonia.ViewModels;

namespace DailyTaskManager.Avalonia;

public partial class App : Application
{
    public static IServiceProvider? ServiceProvider { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            
            // Data access needs the schema; it has been initializing in parallel with the UI toolkit startup
            Program.DatabaseReady.GetAwaiter().GetResult();

            // Read the theme and check for a remembered session at the same time
            var themeTask = Task.Run(ReadSavedThemeAsync);
            var sessionTask = Task.Run(TryRestoreSessionAsync);
            Task.WaitAll(themeTask, sessionTask);

            RequestedThemeVariant = string.Equals(themeTask.Result, "Dark", StringComparison.OrdinalIgnoreCase)
                ? ThemeVariant.Dark
                : ThemeVariant.Light;

            // Skip the login screen when a valid "remember me" session exists on this machine
            desktop.MainWindow = sessionTask.Result ? CreateMainWindow() : CreateLoginWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task<bool> TryRestoreSessionAsync()
    {
        try
        {
            var auth = ServiceProvider?.GetService<IAuthenticationService>();
            if (auth == null) return false;

            return (await auth.RestoreSessionAsync()).IsSuccess;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Could not restore the saved session");
            return false;
        }
    }

    private static async Task<string?> ReadSavedThemeAsync()
    {
        try
        {
            var settings = ServiceProvider?.GetService<ISettingsService>();
            return settings == null ? null : await settings.GetSettingAsync<string>("Theme", "Light");
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Could not read the saved theme");
            return null;
        }
    }

    public LoginWindow CreateLoginWindow()
    {
        if (ServiceProvider == null)
        {
            throw new InvalidOperationException("ServiceProvider is not initialized");
        }

        var loginWindow = new LoginWindow();
        MemoryTrimmer.Attach(loginWindow);
        var viewModel = ServiceProvider.GetRequiredService<LoginViewModel>();
        loginWindow.DataContext = viewModel;

        return loginWindow;
    }

    public MainWindow CreateMainWindow()
    {
        if (ServiceProvider == null)
        {
            throw new InvalidOperationException("ServiceProvider is not initialized");
        }

        var mainWindow = new MainWindow();
        MemoryTrimmer.Attach(mainWindow);
        var viewModel = ServiceProvider.GetRequiredService<MainWindowViewModel>();
        mainWindow.DataContext = viewModel;

        return mainWindow;
    }

    /// <summary>
    /// Helper method to get services from anywhere in the application
    /// </summary>
    public static T GetService<T>() where T : notnull
    {
        if (ServiceProvider == null)
        {
            throw new InvalidOperationException("ServiceProvider is not available");
        }

        return ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Helper method to get optional services
    /// </summary>
    public static T? GetOptionalService<T>() where T : class
    {
        return ServiceProvider?.GetService<T>();
    }

}