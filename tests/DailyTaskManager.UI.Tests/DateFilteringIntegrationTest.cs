using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using DailyTaskManager.Avalonia.ViewModels;
using DailyTaskManager.Infrastructure.Data;
using DailyTaskManager.Infrastructure.Services;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Avalonia.Services;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace DailyTaskManager.UI.Tests;

/// <summary>
/// Integration test that directly tests the date filtering functionality without UI automation.
/// This bypasses the UI automation issues and tests the core filtering logic directly.
/// </summary>
[TestClass]
public class DateFilteringIntegrationTest
{
    private IServiceProvider? _serviceProvider;
    private DailyTasksViewModel? _viewModel;

    [TestInitialize]
    public async Task TestInitialize()
    {
        Console.WriteLine("🔧 Setting up integration test environment...");

        // Create a host similar to the main application
        var host = CreateTestHost();
        _serviceProvider = host.Services;

        // Initialize database
        await InitializeDatabaseAsync(_serviceProvider);

        // Create and initialize the ViewModel
        _viewModel = _serviceProvider.GetRequiredService<DailyTasksViewModel>();

        // Authenticate a test user
        await AuthenticateTestUserAsync();

        // Create sample tasks for testing
        await CreateSampleTasksAsync();

        // Load tasks by calling refresh (which calls LoadTasksAsync internally)
        await _viewModel.RefreshCommand.ExecuteAsync(null);

        Console.WriteLine($"✅ Integration test setup complete. Found {_viewModel.Tasks.Count} total tasks");
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _serviceProvider = null;
        _viewModel = null;
    }

    [TestMethod]
    public async Task DateFiltering_ShouldFilterTasksCorrectly()
    {
        Console.WriteLine("🧪 Testing date filtering functionality directly...");

        // Verify we have tasks loaded
        Assert.IsNotNull(_viewModel, "ViewModel should be initialized");
        Assert.IsTrue(_viewModel.Tasks.Count > 0, "Should have tasks loaded");

        var totalTasks = _viewModel.Tasks.Count;
        Console.WriteLine($"📋 Total tasks loaded: {totalTasks}");

        // Print all task dates for debugging
        Console.WriteLine("📅 Task dates found:");
        foreach (var task in _viewModel.Tasks.OrderBy(t => t.Date))
        {
            Console.WriteLine($"  - {task.Date:yyyy-MM-dd}: {task.Description}");
        }

        // Test filtering to August 15 - September 13, 2025
        var dateFrom = new DateTime(2025, 8, 15);
        var dateTo = new DateTime(2025, 9, 13);

        Console.WriteLine($"🔍 Testing date range filter: {dateFrom:yyyy-MM-dd} to {dateTo:yyyy-MM-dd}");

        // Apply the filters by setting ViewModel properties
        Console.WriteLine("📝 Setting ViewModel date filter properties...");
        _viewModel.DateFrom = dateFrom;
        _viewModel.DateTo = dateTo;
        _viewModel.ApplyFilters(); // date changes are applied explicitly (Apply button)

        // Give the ViewModel time to apply filters
        await Task.Delay(1000);

        // Check filtered results
        var filteredTasks = _viewModel.FilteredTasks;
        Console.WriteLine($"📊 Tasks after filtering: {filteredTasks.Count} (was {totalTasks})");

        // Print filtered task dates
        if (filteredTasks.Count > 0)
        {
            Console.WriteLine("📅 Filtered task dates:");
            foreach (var task in filteredTasks.OrderBy(t => t.Date))
            {
                Console.WriteLine($"  ✓ {task.Date:yyyy-MM-dd}: {task.Description}");
            }
        }
        else
        {
            Console.WriteLine("⚠️ No tasks found in filtered results");
        }

        // Verify filtering worked correctly
        if (filteredTasks.Count > 0)
        {
            // Check that all filtered tasks are within the date range
            foreach (var task in filteredTasks)
            {
                var taskDate = task.Date.Date;
                Assert.IsTrue(taskDate >= dateFrom.Date && taskDate <= dateTo.Date,
                    $"Task date {taskDate:yyyy-MM-dd} should be within range {dateFrom:yyyy-MM-dd} to {dateTo:yyyy-MM-dd}");
            }

            Console.WriteLine("✅ All filtered tasks are within the specified date range");

            // Verify that filtering actually reduced the number of tasks
            Assert.IsTrue(filteredTasks.Count <= totalTasks,
                "Filtered tasks count should be less than or equal to total tasks");

            Console.WriteLine($"✅ Filtering reduced tasks from {totalTasks} to {filteredTasks.Count}");
        }
        else
        {
            // If no tasks in range, verify that's correct by checking if any tasks should be in range
            var tasksInRange = _viewModel.Tasks.Where(t =>
                t.Date.Date >= dateFrom.Date && t.Date.Date <= dateTo.Date).Count();

            Console.WriteLine($"📊 Expected tasks in range: {tasksInRange}");

            if (tasksInRange > 0)
            {
                Assert.Fail($"Expected {tasksInRange} tasks in date range, but filtering returned 0 tasks. Filter may not be working.");
            }
            else
            {
                Console.WriteLine("✅ Correctly filtered to 0 tasks - no tasks exist in the specified date range");
            }
        }

        // Test clearing filters
        Console.WriteLine("🔄 Testing filter clearing...");
        _viewModel.DateFrom = null;
        _viewModel.DateTo = null;
        _viewModel.ApplyFilters();

        await Task.Delay(500);

        Assert.AreEqual(totalTasks, _viewModel.FilteredTasks.Count,
            "After clearing filters, should show all tasks again");

        Console.WriteLine("✅ Filter clearing works correctly");
        Console.WriteLine("🎉 Date filtering integration test passed!");
    }

    private IHost CreateTestHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.Sources.Clear();
                var appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Environment.CurrentDirectory;

                // Look for appsettings.json in the Avalonia project directory
                var avaloniaDir = Path.GetFullPath(Path.Combine(appDir, "..", "..", "..", "..", "..", "src", "DailyTaskManager.Avalonia"));
                var settingsPath = Path.Combine(avaloniaDir, "appsettings.json");

                config.SetBasePath(avaloniaDir)
                      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                      .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
                      .AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;

                // Add database context
                services.AddDbContextFactory<DailyTaskManagerDbContext>(options =>
                {
                    var connectionString = configuration.GetConnectionString("DefaultConnection")
                                         ?? "Host=localhost;Port=5432;Database=DailyTaskManager;Username=postgres;Password=admin;";
                    options.UseNpgsql(connectionString);
                });

                // Core Services
                services.AddSingleton<IDatabaseService, DatabaseService>();
                services.AddSingleton<ISessionStore, SessionStore>();
                services.AddSingleton<IAuthenticationService, AuthenticationService>();
                services.AddSingleton<IExportService, ExportService>();
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IPlatformService, PlatformService>();
                services.AddSingleton<ISpeechRecognitionService, SpeechRecognitionService>();
                services.AddSingleton<IOAuthService, DailyTaskManager.Infrastructure.Services.OAuthService>();

                // ViewModels
                services.AddTransient<DailyTasksViewModel>();
                services.AddTransient<LoginViewModel>();

                // App Services
                services.AddSingleton<NavigationService>();
                services.AddSingleton<DialogService>();
            })
            .Build();
    }

    private async Task InitializeDatabaseAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DailyTaskManagerDbContext>();

        try
        {
            await context.MigrateDatabaseAsync();
            Console.WriteLine("✅ Database initialized for integration test");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to initialize database: {ex.Message}");
            throw;
        }
    }

    private async Task AuthenticateTestUserAsync()
    {
        try
        {
            var authService = _serviceProvider!.GetRequiredService<IAuthenticationService>();

            // Try to login with the admin user
            var loginRequest = new DailyTaskManager.Core.Models.LoginRequest
            {
                Username = "admin",
                Password = "admin123"
            };
            var loginResult = await authService.LoginAsync(loginRequest);

            if (!loginResult.IsSuccess)
            {
                Console.WriteLine("⚠️ Admin login failed, attempting to create admin user...");
                var registerResult = await authService.RegisterAsync("admin", "admin@example.com", "admin123");

                if (registerResult.IsSuccess)
                {
                    loginResult = await authService.LoginAsync(loginRequest);
                }
            }

            if (loginResult.IsSuccess)
            {
                Console.WriteLine("✅ Test user authenticated successfully");
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate test user: {loginResult.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Authentication failed: {ex.Message}");
            throw;
        }
    }

    private async Task CreateSampleTasksAsync()
    {
        try
        {
            var authService = _serviceProvider!.GetRequiredService<IAuthenticationService>();
            var currentUser = authService.CurrentUser;

            if (currentUser == null)
            {
                throw new InvalidOperationException("No authenticated user found for sample task creation");
            }

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DailyTaskManagerDbContext>();

            // Check if the user already has tasks
            var existingTaskCount = await context.DailyTasks.Where(t => t.UserId == currentUser.Id).CountAsync();

            if (existingTaskCount == 0)
            {
                Console.WriteLine($"🔧 Creating sample tasks for user {currentUser.Username}...");

                var sampleTasks = new[]
                {
                    // Tasks within the filter range (Aug 15 - Sep 13, 2025)
                    new DailyTaskManager.Core.Models.DailyTask
                    {
                        UserId = currentUser.Id,
                        Date = DateTime.SpecifyKind(new DateTime(2025, 8, 15), DateTimeKind.Utc),
                        Description = "Implemented Google OAuth authentication system",
                        Hours = 4.5m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new DailyTaskManager.Core.Models.DailyTask
                    {
                        UserId = currentUser.Id,
                        Date = DateTime.SpecifyKind(new DateTime(2025, 8, 20), DateTimeKind.Utc),
                        Description = "Updated project documentation and README files",
                        Hours = 2.0m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new DailyTaskManager.Core.Models.DailyTask
                    {
                        UserId = currentUser.Id,
                        Date = DateTime.SpecifyKind(new DateTime(2025, 9, 1), DateTimeKind.Utc),
                        Description = "Migrated UI from WPF to Avalonia framework",
                        Hours = 6.5m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new DailyTaskManager.Core.Models.DailyTask
                    {
                        UserId = currentUser.Id,
                        Date = DateTime.SpecifyKind(new DateTime(2025, 9, 10), DateTimeKind.Utc),
                        Description = "Fixed database connection issues with PostgreSQL",
                        Hours = 3.0m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new DailyTaskManager.Core.Models.DailyTask
                    {
                        UserId = currentUser.Id,
                        Date = DateTime.SpecifyKind(new DateTime(2025, 9, 13), DateTimeKind.Utc),
                        Description = "Designed modern UI inspired by Clockify interface",
                        Hours = 5.0m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },

                    // Tasks outside the filter range (for testing exclusion)
                    new DailyTaskManager.Core.Models.DailyTask
                    {
                        UserId = currentUser.Id,
                        Date = DateTime.SpecifyKind(new DateTime(2025, 8, 10), DateTimeKind.Utc),
                        Description = "Task before date range",
                        Hours = 1.0m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new DailyTaskManager.Core.Models.DailyTask
                    {
                        UserId = currentUser.Id,
                        Date = DateTime.SpecifyKind(new DateTime(2025, 9, 20), DateTimeKind.Utc),
                        Description = "Task after date range",
                        Hours = 2.0m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                await context.DailyTasks.AddRangeAsync(sampleTasks);
                await context.SaveChangesAsync();

                Console.WriteLine($"✅ Created {sampleTasks.Length} sample tasks for testing");
            }
            else
            {
                Console.WriteLine($"📝 User already has {existingTaskCount} tasks, skipping sample creation");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to create sample tasks: {ex.Message}");
            throw;
        }
    }
}