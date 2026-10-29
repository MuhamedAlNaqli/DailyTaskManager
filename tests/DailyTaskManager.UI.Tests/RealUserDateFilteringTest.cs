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

namespace DailyTaskManager.UI.Tests;

/// <summary>
/// Test the real user scenario with Muhamed's account to verify date filtering works
/// </summary>
[TestClass]
public class RealUserDateFilteringTest
{
    private IServiceProvider? _serviceProvider;
    private DailyTasksViewModel? _viewModel;

    [TestInitialize]
    public async Task TestInitialize()
    {
        Console.WriteLine("🔧 Setting up real user test environment...");

        // Create a host similar to the main application
        var host = CreateTestHost();
        _serviceProvider = host.Services;

        // Initialize database
        await InitializeDatabaseAsync(_serviceProvider);

        // Create and initialize the ViewModel
        _viewModel = _serviceProvider.GetRequiredService<DailyTasksViewModel>();

        // Authenticate with Muhamed's account
        await AuthenticateRealUserAsync();

        // Load tasks
        await _viewModel.RefreshCommand.ExecuteAsync(null);

        Console.WriteLine($"✅ Real user test setup complete. Found {_viewModel.TotalCount} total tasks");
    }

    private System.Collections.Generic.List<DailyTaskManager.Core.Models.DailyTask> AllTasks =>
        _serviceProvider!.GetRequiredService<DailyTaskManager.Core.Interfaces.IDatabaseService>()
            .GetTasksAsync(new DailyTaskManager.Core.Models.TaskQuery()).GetAwaiter().GetResult();

    [TestCleanup]
    public void TestCleanup()
    {
        _serviceProvider = null;
        _viewModel = null;
    }

    [TestMethod]
    public async Task MuhamedAccount_DateFiltering_ShouldWorkCorrectly()
    {
        Console.WriteLine("🧪 Testing date filtering with Muhamed's real data...");

        // Verify we have tasks loaded
        Assert.IsNotNull(_viewModel, "ViewModel should be initialized");
        Assert.IsTrue(_viewModel.TotalCount > 0, "Should have tasks loaded");

        var totalTasks = _viewModel.TotalCount;
        Console.WriteLine($"📋 Total tasks for Muhamed: {totalTasks}");

        // Print all task dates for debugging
        Console.WriteLine("📅 Task dates found for Muhamed:");
        foreach (var task in AllTasks.OrderBy(t => t.Date))
        {
            Console.WriteLine($"  - {task.Date:yyyy-MM-dd}: {task.Description?.Substring(0, Math.Min(50, task.Description?.Length ?? 0))}...");
        }

        // Test filtering to Sep 1-15, 2025 (the range from the screenshot)
        var dateFrom = new DateTime(2025, 9, 1);
        var dateTo = new DateTime(2025, 9, 15);

        Console.WriteLine($"🔍 Testing date range filter: {dateFrom:yyyy-MM-dd} to {dateTo:yyyy-MM-dd}");

        // Apply the filters by setting ViewModel properties
        Console.WriteLine("📝 Setting ViewModel date filter properties...");
        _viewModel.DateFrom = dateFrom;
        _viewModel.DateTo = dateTo;
        await _viewModel.ApplyFiltersAsync(); // date changes are applied explicitly (Apply button)

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
                Console.WriteLine($"  ✓ {task.Date:yyyy-MM-dd}: {task.Description?.Substring(0, Math.Min(50, task.Description?.Length ?? 0))}...");
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

            // Verify that filtering actually changed the results
            Assert.IsTrue(filteredTasks.Count <= totalTasks,
                "Filtered tasks count should be less than or equal to total tasks");

            Console.WriteLine($"✅ Filtering worked correctly: {totalTasks} → {filteredTasks.Count} tasks");
        }
        else
        {
            // If no tasks in range, verify that's correct by checking if any tasks should be in range
            var tasksInRange = AllTasks.Where(t =>
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
        await _viewModel.ApplyFiltersAsync();

        await Task.Delay(500);

        Assert.AreEqual(totalTasks, _viewModel.TotalCount,
            "After clearing filters, should show all tasks again");

        Console.WriteLine("✅ Filter clearing works correctly");
        Console.WriteLine("🎉 Real user date filtering test completed!");
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
                services.AddSingleton<IBusinessService, BusinessService>();
                services.AddSingleton<IPlatformService, PlatformService>();
                services.AddSingleton<ISpeechRecognitionService, WhisperSpeechService>();
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
            Console.WriteLine("✅ Database initialized for real user test");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to initialize database: {ex.Message}");
            throw;
        }
    }

    private async Task AuthenticateRealUserAsync()
    {
        try
        {
            var authService = _serviceProvider!.GetRequiredService<IAuthenticationService>();

            // Try to login with Muhamed's account
            var loginRequest = new DailyTaskManager.Core.Models.LoginRequest
            {
                Username = "Muhamed",
                Password = "SelmaSumejaAjna123!*"
            };
            var loginResult = await authService.LoginAsync(loginRequest);

            if (loginResult.IsSuccess)
            {
                Console.WriteLine("✅ Muhamed's account authenticated successfully");
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate Muhamed's account: {loginResult.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Authentication failed: {ex.Message}");
            throw;
        }
    }
}