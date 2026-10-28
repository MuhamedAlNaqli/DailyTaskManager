using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Serilog;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Models;
using DailyTaskManager.Core.Reminders;
using DailyTaskManager.Infrastructure.Data;
using DailyTaskManager.Infrastructure.Services;
using DailyTaskManager.Avalonia.Background;
using DailyTaskManager.Avalonia.Services;
using DailyTaskManager.Avalonia.ViewModels;

namespace DailyTaskManager.Avalonia;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            // The Windows task scheduler starts this exe with --reminder-check; that run shows a notification
            // and exits without ever creating a window
            var reminderRun = ReminderRunner.IsReminderRun(args);
            if (!reminderRun && !SingleInstance.TryAcquire())
            {
                SingleInstance.ActivateExisting();
                return;
            }

            // Set up dependency injection host
            var host = CreateHostBuilder(args).Build();
            if (reminderRun)
            {
                Environment.ExitCode = ReminderRunner.Run(host.Services, args);
                Serilog.Log.CloseAndFlush();
                return;
            }

            // Configure application with the host's services
            // Store the service provider for global access
            App.ServiceProvider = host.Services;
            // Warm up the database (EF model, connection, schema check) on a worker thread while Avalonia
            // initializes the platform and fonts; the app waits for it before it needs data (see App).
            DatabaseReady = Task.Run(() => InitializeDatabaseAsync(host.Services));
            // The update service is not needed to show the first window
            _ = Task.Run(() => InitializeUpdateServiceAsync(host.Services));
            _ = Task.Run(() => SyncBackgroundReminderAsync(host.Services));
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
            
        }
        catch (Exception ex)
        {
            Serilog.Log.Fatal(ex, "Unhandled exception in Main");
            throw;
        }
    }

    /// <summary>Completes when the database schema is ready; faults when initialization failed</summary>
    public static Task DatabaseReady { get; private set; } = Task.CompletedTask;

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                // Clear existing sources and rebuild configuration
                config.Sources.Clear();
                
                // Get the application directory
                var appDir = AppContext.BaseDirectory;
                
                // Add JSON configuration files with explicit paths
                config.SetBasePath(appDir)
                      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                      // Machine-specific secrets (e.g. the database password); not committed to git
                      .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
                      .AddEnvironmentVariables()
                      .AddCommandLine(args);
            })
            .UseSerilog((context, configuration) =>
            {
                // File only (this is a windowed app, nobody reads the console), and no per-SQL-command lines
                configuration
                    .WriteTo.File("logs/dailytaskmanager-.txt", 
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7)
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning);
            })
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;
                
                // Add database context with improved connection string handling
                services.AddDbContextFactory<DailyTaskManagerDbContext>(options =>
                {
                    // Try multiple sources for connection string
                    var connectionString = Environment.GetEnvironmentVariable("DTM_CONNECTION_STRING")
                                         ?? configuration.GetConnectionString("DefaultConnection");
                    
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        throw new InvalidOperationException(
                            "No database connection string configured. Set ConnectionStrings:DefaultConnection in " +
                            "appsettings.local.json, or the DTM_CONNECTION_STRING environment variable.");
                    }

                    options.UseNpgsql(connectionString);
                });

                // Core Services
                services.AddSingleton<IDatabaseService, DatabaseService>();
                services.AddSingleton<ISessionStore, SessionStore>();
                services.AddSingleton<IAuthenticationService, AuthenticationService>();
                services.AddSingleton<IExportService, ExportService>();
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IBackupService, BackupService>();
                services.AddSingleton<IAutoCompleteService, AutoCompleteService>();
                services.AddSingleton<IPlatformService, PlatformService>();
                services.AddSingleton<IBackgroundReminderService, WindowsReminderScheduler>();
                services.AddSingleton<ReminderEvaluator>();
                services.AddSingleton<ISpeechRecognitionService, SpeechRecognitionService>();
                services.AddSingleton<IOAuthService, DailyTaskManager.Infrastructure.Services.OAuthService>();
                services.AddSingleton<IUpdateService, UpdateService>();
                
                // Native notifications - placeholder for future implementation
                // services.AddSingleton<INotificationManager>(provider => /* Notification manager implementation */); 
                
                // ViewModels
                services.AddTransient<MainWindowViewModel>();
                services.AddTransient<DailyTasksViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<AnalyticsViewModel>();
                services.AddTransient<ActivityCalendarViewModel>();
                services.AddTransient<ReportsViewModel>();
                services.AddTransient<LoginViewModel>();
                services.AddTransient<AddTaskViewModel>();
                services.AddTransient<EditTaskViewModel>();
                
                // App Services
                services.AddSingleton<NavigationService>();
                services.AddSingleton<DialogService>();
            });

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            // Software rendering keeps the graphics driver (tens of MB) out of the process; this UI is light enough
            .With(new Win32PlatformOptions { RenderingMode = new[] { Win32RenderingMode.Software } })
            .WithInterFont()
            .LogToTrace();

    private static async Task InitializeDatabaseAsync(IServiceProvider services)
    {
        var contextFactory = services.GetRequiredService<IDbContextFactory<DailyTaskManagerDbContext>>();
        await using var context = contextFactory.CreateDbContext();
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Initializing database...");
            await context.MigrateDatabaseAsync();
            
            logger.LogInformation("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize database");
            throw;
        }
    }

    /// <summary>
    /// Keeps the Windows scheduled reminder in line with the user's settings, and re-registers it after the
    /// app moved to another folder (an update installs to a new path)
    /// </summary>
    private static async Task SyncBackgroundReminderAsync(IServiceProvider services)
    {
        try
        {
            await DatabaseReady;
            var settings = services.GetRequiredService<ISettingsService>();
            var enabled = ReminderSettings.ParseFlag(await settings.GetSettingAsync<string>(ReminderSettings.EnabledKey, null), ReminderSettings.DefaultEnabled);
            var background = ReminderSettings.ParseFlag(await settings.GetSettingAsync<string>(ReminderSettings.BackgroundKey, null), ReminderSettings.DefaultBackground);
            await services.GetRequiredService<IBackgroundReminderService>().SetInstalledAsync(enabled && background);
        }
        catch (Exception ex)
        {
            services.GetService<ILogger<Program>>()?.LogWarning(ex, "Could not sync the background reminder");
        }
    }

    private static async Task InitializeUpdateServiceAsync(IServiceProvider services)
    {
        try
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            var updateService = services.GetRequiredService<IUpdateService>();

            logger.LogInformation("Initializing update service...");
            
            // Initialize the update service
            await updateService.InitializeAsync();
            
            // Start automatic update checking with configured interval
            var configuration = services.GetRequiredService<IConfiguration>();
            var checkIntervalString = configuration["UpdateService:CheckInterval"] ?? "24:00:00";
            if (TimeSpan.TryParse(checkIntervalString, out var checkInterval))
            {
                logger.LogInformation("Starting automatic update checking with interval: {Interval}", checkInterval);
                updateService.StartAutomaticUpdateChecking(checkInterval);
            }
            else
            {
                logger.LogWarning("Invalid CheckInterval format '{Interval}', using 24 hours default", checkIntervalString);
                updateService.StartAutomaticUpdateChecking(TimeSpan.FromHours(24));
            }
            
            logger.LogInformation("Update service initialized successfully");
        }
        catch (Exception ex)
        {
            // Don't fail startup if update service fails to initialize
            var logger = services.GetService<ILogger<Program>>();
            logger?.LogError(ex, "Failed to initialize update service");
        }
    }
}