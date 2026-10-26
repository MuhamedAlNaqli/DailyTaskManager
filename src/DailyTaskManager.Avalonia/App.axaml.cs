using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using DailyTaskManager.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
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
            
            // Start with the login window instead of main window
            ApplySavedTheme();

            // Skip the login screen when a valid "remember me" session exists on this machine
            desktop.MainWindow = TryRestoreSession() ? CreateMainWindow() : CreateLoginWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static bool TryRestoreSession()
    {
        try
        {
            var auth = ServiceProvider?.GetService<IAuthenticationService>();
            if (auth == null) return false;

            // Run off the UI thread so the database call can't deadlock the startup sequence
            return Task.Run(() => auth.RestoreSessionAsync()).GetAwaiter().GetResult().IsSuccess;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Could not restore the saved session");
            return false;
        }
    }

    private void ApplySavedTheme()
    {
        try
        {
            var settings = ServiceProvider?.GetService<ISettingsService>();
            if (settings == null) return;

            var theme = Task.Run(() => settings.GetSettingAsync<string>("Theme", "Light")).GetAwaiter().GetResult();
            RequestedThemeVariant = string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase)
                ? ThemeVariant.Dark
                : ThemeVariant.Light;
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Could not apply the saved theme");
        }
    }

    public LoginWindow CreateLoginWindow()
    {
        if (ServiceProvider == null)
        {
            throw new InvalidOperationException("ServiceProvider is not initialized");
        }

        var loginWindow = new LoginWindow();
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