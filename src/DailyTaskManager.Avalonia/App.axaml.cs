using System;
using Avalonia.Controls;
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
        UiLocalizer.Initialize();
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Something is on screen at once; the database, the theme and the remembered session are read behind it
            var splash = new SplashWindow();
            desktop.MainWindow = splash;
            _ = FinishStartupAsync(desktop, splash);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task FinishStartupAsync(IClassicDesktopStyleApplicationLifetime desktop, SplashWindow splash)
    {
        try
        {
            // Data access needs the schema; it has been initializing in parallel with the UI toolkit startup
            await Program.DatabaseReady;

            // Read the theme and check for a remembered session at the same time
            var themeTask = Task.Run(ReadSavedThemeAsync);
            var sessionTask = Task.Run(TryRestoreSessionAsync);
            await Task.WhenAll(themeTask, sessionTask);

            RequestedThemeVariant = string.Equals(themeTask.Result, "Dark", StringComparison.OrdinalIgnoreCase)
                ? ThemeVariant.Dark
                : ThemeVariant.Light;

            // Skip the login screen when a valid "remember me" session exists on this machine
            Window next = sessionTask.Result ? CreateMainWindow() : CreateLoginWindow();

            // The new window is the main one before the splash closes, so closing the splash does not end the app
            desktop.MainWindow = next;
            next.Show();
            splash.Close();
        }
        catch (Exception ex)
        {
            Serilog.Log.Fatal(ex, "The app could not start");
            splash.ShowFailure("The app could not start: " + (ex.InnerException?.Message ?? ex.Message));
        }
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