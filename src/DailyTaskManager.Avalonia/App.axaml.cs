using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
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
            Console.WriteLine("DEBUG: OnFrameworkInitializationCompleted called");
            
            // Start with the login window instead of main window
            Console.WriteLine("DEBUG: Creating login window");
            desktop.MainWindow = CreateLoginWindow();
            Console.WriteLine("DEBUG: Login window created");
        }

        base.OnFrameworkInitializationCompleted();
        Console.WriteLine("DEBUG: OnFrameworkInitializationCompleted completed");
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