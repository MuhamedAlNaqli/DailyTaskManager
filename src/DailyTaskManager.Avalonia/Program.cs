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
                var appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Environment.CurrentDirectory;
                
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
                    
                    Console.WriteLine($"DEBUG: Connection string loaded: '{connectionString}'");
                    
                    // Fallback to default if none found
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        Console.WriteLine("WARNING: No connection string found in appsettings.json or environment variables, using development fallback");
                        connectionString = "Host=localhost;Port=5432;Database=DailyTaskManager;Username=postgres;Password=admin;";
                    }
                    
                    Console.WriteLine($"DEBUG: Using connection string: '{connectionString}'");
                    options.UseNpgsql(connectionString);
                });

                // Core Services
                services.AddScoped<IDatabaseService, DatabaseService>();
                services.AddScoped<IAuthenticationService, AuthenticationService>();
                services.AddScoped<IExportService, ExportService>();
                services.AddScoped<ISettingsService, SettingsService>();
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
        var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();

        try
        {
            logger.LogInformation("Initializing database...");
            await context.MigrateDatabaseAsync();
            
            // Create default user if no users exist
            await CreateDefaultUserIfNeededAsync(context, authService, logger);
            
            // Skip sample task creation - users will create their own tasks after login
            
            logger.LogInformation("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize database");
            throw;
        }
    }

    private static async Task CreateDefaultUserIfNeededAsync(DailyTaskManagerDbContext context, IAuthenticationService authService, ILogger<Program> logger)
    {
        try
        {
            // Check if admin user exists (non-OAuth admin user)
            var adminUserExists = await context.Users.AnyAsync(u => u.Username == "admin" && string.IsNullOrEmpty(u.OAuthProvider));
            
            if (!adminUserExists)
            {
                logger.LogInformation("Admin user not found, creating default admin user...");
                var result = await authService.RegisterAsync("admin", "admin@example.com", "admin123");
                
                if (result.IsSuccess)
                {
                    logger.LogInformation("Default admin user created successfully - Username: admin, Password: admin123");
                    Console.WriteLine("DEBUG: Default admin user created - Username: admin, Password: admin123");
                }
                else
                {
                    logger.LogError("Failed to create default admin user: {Error}", result.ErrorMessage);
                }
            }
            else
            {
                logger.LogInformation("Admin user already exists, login window will handle authentication");
                Console.WriteLine("DEBUG: Admin user already exists - Username: admin, Password: admin123");
            }

            // Also check if we need to add password authentication to existing OAuth users
            await AddPasswordToOAuthUsersAsync(context, authService, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating default admin user");
        }
    }

    private static async Task AutoLoginUserAsync(Core.Models.User user, IAuthenticationService authService, ILogger<Program> logger, DailyTaskManagerDbContext context)
    {
        try
        {
            // Check if user is already marked as OAuth user
            if (user.IsOAuthUser)
            {
                // Use OAuth flow for OAuth users
                var oauthResult = new Core.Models.OAuthResult
                {
                    Email = user.Email,
                    DisplayName = user.DisplayName ?? user.Username,
                    Provider = user.OAuthProvider ?? "Auto",
                    ProviderId = user.OAuthId ?? user.Id.ToString()
                };

                var result = await authService.LoginWithOAuthAsync(oauthResult);
                if (result.IsSuccess)
                {
                    logger.LogInformation("Auto-login successful via OAuth for user: {Username}", user.Username);
                    Console.WriteLine($"DEBUG: Auto-login successful via OAuth for user: {user.Username}");
                }
                else
                {
                    logger.LogWarning("OAuth auto-login failed for user: {Username} - {Error}", user.Username, result.ErrorMessage);
                }
            }
            else
            {
                // For non-OAuth users, mark them as OAuth users for auto-login to work
                // Update user in database to be OAuth user
                var dbUser = await context.Users.FindAsync(user.Id);
                if (dbUser != null)
                {
                    dbUser.OAuthProvider = "Auto";
                    dbUser.OAuthId = user.Id.ToString();
                    dbUser.UpdateTimestamp();
                    await context.SaveChangesAsync();
                    
                    // Now attempt OAuth login
                    var oauthResult = new Core.Models.OAuthResult
                    {
                        Email = user.Email,
                        DisplayName = user.DisplayName ?? user.Username,
                        Provider = "Auto",
                        ProviderId = user.Id.ToString()
                    };

                    var result = await authService.LoginWithOAuthAsync(oauthResult);
                    if (result.IsSuccess)
                    {
                        logger.LogInformation("Auto-login successful (converted to OAuth) for user: {Username}", user.Username);
                        Console.WriteLine($"DEBUG: Auto-login successful (converted to OAuth) for user: {user.Username}");
                    }
                    else
                    {
                        logger.LogWarning("Auto-login failed after OAuth conversion for user: {Username} - {Error}", user.Username, result.ErrorMessage);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during auto-login for user {Username}", user.Username);
        }
    }

    private static async Task CreateSampleTasksIfNeededAsync(DailyTaskManagerDbContext context, ILogger<Program> logger)
    {
        try
        {
            // Find the currently authenticated user (from the auth service) - include all OAuth providers
            var authenticatedUser = await context.Users.FirstOrDefaultAsync(u => u.IsActive && !string.IsNullOrEmpty(u.OAuthProvider));
            if (authenticatedUser == null)
            {
                logger.LogWarning("No authenticated OAuth user found for sample task creation");
                return;
            }

            // Check if the authenticated user has any tasks
            var userHasTasks = await context.DailyTasks.AnyAsync(t => t.UserId == authenticatedUser.Id);
            
            if (!userHasTasks)
            {
                logger.LogInformation("No tasks found for user {UserId}, creating sample tasks...", authenticatedUser.Id);
                
                var today = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc);
                var yesterday = DateTime.SpecifyKind(DateTime.Today.AddDays(-1), DateTimeKind.Utc);
                
                var sampleTasks = new[]
                {
                    new Core.Models.DailyTask
                    {
                        UserId = authenticatedUser.Id,
                        Date = today,
                        Description = "Implemented Google OAuth authentication system",
                        Hours = 4.5m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Core.Models.DailyTask
                    {
                        UserId = authenticatedUser.Id,
                        Date = today,
                        Description = "Updated project documentation and README files",
                        Hours = 2.0m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Core.Models.DailyTask
                    {
                        UserId = authenticatedUser.Id,
                        Date = yesterday,
                        Description = "Migrated UI from WPF to Avalonia framework",
                        Hours = 6.5m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Core.Models.DailyTask
                    {
                        UserId = authenticatedUser.Id,
                        Date = yesterday,
                        Description = "Fixed database connection issues with PostgreSQL",
                        Hours = 3.0m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Core.Models.DailyTask
                    {
                        UserId = authenticatedUser.Id,
                        Date = DateTime.SpecifyKind(DateTime.Today.AddDays(-2), DateTimeKind.Utc),
                        Description = "Designed modern UI inspired by Clockify interface",
                        Hours = 5.0m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                await context.DailyTasks.AddRangeAsync(sampleTasks);
                await context.SaveChangesAsync();
                
                logger.LogInformation("Sample tasks created successfully for user {UserId}", authenticatedUser.Id);
                Console.WriteLine($"DEBUG: Sample tasks created successfully for user {authenticatedUser.Id}");
            }
            else
            {
                logger.LogInformation("User {UserId} already has tasks, skipping sample data creation", authenticatedUser.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating sample tasks");
        }
    }

    private static async Task AddPasswordToOAuthUsersAsync(DailyTaskManagerDbContext context, IAuthenticationService authService, ILogger<Program> logger)
    {
        try
        {
            // Find OAuth users who don't have password authentication
            var oauthUsersWithoutPasswords = await context.Users
                .Where(u => u.IsActive && !string.IsNullOrEmpty(u.OAuthProvider) && string.IsNullOrEmpty(u.PasswordHash))
                .ToListAsync();

            if (oauthUsersWithoutPasswords.Any())
            {
                logger.LogInformation("Found {Count} OAuth users without password authentication", oauthUsersWithoutPasswords.Count);
                
                foreach (var user in oauthUsersWithoutPasswords)
                {
                    // Create a default password for OAuth users to enable dual authentication
                    var defaultPassword = "oauth123"; // Default password for OAuth users
                    
                    if (await ((AuthenticationService)authService).AddPasswordToUserAsync(user.Username, defaultPassword))
                    {
                        logger.LogInformation("Added password authentication for OAuth user: {Username} (password: {Password})", 
                            user.Username, defaultPassword);
                        Console.WriteLine($"DEBUG: Added password authentication for OAuth user: {user.Username} (password: {defaultPassword})");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding password authentication to OAuth users");
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
            
            // Start automatic update checking (check every 24 hours)
            updateService.StartAutomaticUpdateChecking(TimeSpan.FromHours(24));
            
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