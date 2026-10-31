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
            
            // Data access needs the schema; it has been initializing in parallel with the UI toolkit startup
            Program.DatabaseReady.GetAwaiter().GetResult();

            // Employee mode: no local database and no personal sign-in; the Windows account is the sign-in
            if (AppMode.IsEmployee)
            {
                RequestedThemeVariant = ThemeVariant.Light;
                desktop.MainWindow = CreateEmployeeWindow();
                base.OnFrameworkInitializationCompleted();
                return;
            }

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

    /// <summary>The employee's month. A missing server address is shown in the window instead of stopping the app.</summary>
    public Window CreateEmployeeWindow()
    {
        if (ServiceProvider == null) throw new InvalidOperationException("ServiceProvider is not initialized");

        // The interface of employee mode is Bosnian only (month names, date pickers and number formats)
        var bosnian = EmployeeMode.EmployeeFormat.Bosnian;
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = bosnian;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = bosnian;
        System.Globalization.CultureInfo.CurrentCulture = bosnian;
        System.Globalization.CultureInfo.CurrentUICulture = bosnian;

        var window = new EmployeeMode.EmployeeWindow();
        MemoryTrimmer.Attach(window);
        try
        {
            window.DataContext = ServiceProvider.GetRequiredService<EmployeeMode.EmployeeViewModel>();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Employee mode could not start");
            window.Content = new TextBlock
            {
                Text = "Evidencija radnog vremena ne može se pokrenuti: " + (ex.InnerException?.Message ?? ex.Message),
                Margin = new global::Avalonia.Thickness(32),
                TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
            };
        }

        return window;
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