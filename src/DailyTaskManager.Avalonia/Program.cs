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
using DailyTaskManager.Infrastructure.Data;
using DailyTaskManager.Infrastructure.Services;
using DailyTaskManager.Avalonia.Services;
using DailyTaskManager.Avalonia.ViewModels;

namespace DailyTaskManager.Avalonia;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    public static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("DEBUG: Starting Main method");
            
            // Set up dependency injection host
            Console.WriteLine("DEBUG: Creating host builder");
            var host = CreateHostBuilder(args).Build();
            Console.WriteLine("DEBUG: Host created successfully");
            
            // Configure application with the host's services
            Console.WriteLine("DEBUG: Configuring Avalonia app");
            
            // Store the service provider for global access
            App.ServiceProvider = host.Services;
            Console.WriteLine("DEBUG: ServiceProvider set");
            
            // Initialize database before starting UI
            await InitializeDatabaseAsync(host.Services);
            Console.WriteLine("DEBUG: Database initialization completed");
            
            // Initialize update service
            await InitializeUpdateServiceAsync(host.Services);
            Console.WriteLine("DEBUG: Update service initialization completed");
            
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
            
            Console.WriteLine("DEBUG: StartWithClassicDesktopLifetime completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DEBUG: Exception in Main: {ex}");
            throw;
        }
    }

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
                      .AddEnvironmentVariables()
                      .AddCommandLine(args);
            })
            .UseSerilog((context, configuration) =>
            {
                configuration
                    .WriteTo.Console()
                    .WriteTo.File("logs/dailytaskmanager-.txt", 
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7)
                    .MinimumLevel.Information();
            })
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;
                
                // Add database context with improved connection string handling
                services.AddDbContext<DailyTaskManagerDbContext>(options =>
                {
                    // Try multiple sources for connection string
                    var connectionString = configuration.GetConnectionString("DefaultConnection") 
                                         ?? Environment.GetEnvironmentVariable("DTM_CONNECTION_STRING")
                                         ?? Environment.GetEnvironmentVariable("CONNECTION_STRING");
                    
                    // Fallback to default if none found
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        Console.WriteLine("WARNING: No connection string found in appsettings.json or environment variables, using development fallback");
                        connectionString = "Host=localhost;Port=5432;Database=DailyTaskManager;Username=postgres;Password=admin;";
                    }
                    
                    options.UseNpgsql(connectionString);
                });

                // Core Services
                services.AddScoped<IDatabaseService, DatabaseService>();
                services.AddScoped<IAuthenticationService, AuthenticationService>();
                services.AddScoped<IExportService, ExportService>();
                services.AddScoped<ISettingsService, SettingsService>();
                services.AddScoped<IAutoCompleteService, AutoCompleteService>();
                services.AddSingleton<IPlatformService, PlatformService>();
                services.AddSingleton<ISpeechRecognitionService, SpeechRecognitionService>();
                services.AddSingleton<IOAuthService, DailyTaskManager.Infrastructure.Services.OAuthService>();
                services.AddSingleton<IUpdateService, UpdateService>();
                
                // Native notifications - placeholder for future implementation
                // services.AddSingleton<INotificationManager>(provider => /* Notification manager implementation */); 
                
                // ViewModels
                services.AddTransient<MainWindowViewModel>();
                services.AddTransient<DailyTasksViewModel>();
                services.AddTransient<SettingsViewModel>();
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
            .WithInterFont()
            .LogToTrace();

    private static async Task InitializeDatabaseAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DailyTaskManagerDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

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