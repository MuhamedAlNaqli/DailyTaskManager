using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Models;
using DailyTaskManager.Infrastructure.Data;
using DailyTaskManager.Infrastructure.Services;

namespace ConsoleTest;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 DailyTaskManager Migration Test");
        Console.WriteLine("=====================================");
        
        var host = CreateHostBuilder(args).Build();
        
        try
        {
            using var scope = host.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            
            logger.LogInformation("Starting migration test...");
            
            // Test database context
            await TestDatabaseContext(scope.ServiceProvider, logger);
            
            // Test authentication service
            await TestAuthenticationService(scope.ServiceProvider, logger);
            
            // Test export service
            await TestExportService(scope.ServiceProvider, logger);
            
            Console.WriteLine("✅ All tests completed successfully!");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
    
    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                // Use in-memory database for testing
                services.AddDbContext<DailyTaskManagerDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));
                
                // Register services
                services.AddScoped<IDatabaseService, DatabaseService>();
                services.AddScoped<IAuthenticationService, AuthenticationService>();
                services.AddScoped<IExportService, ExportService>();
                services.AddScoped<ISettingsService, SettingsService>();
                
                services.AddLogging(builder => builder.AddConsole());
            });
    
    static async Task TestDatabaseContext(IServiceProvider services, ILogger logger)
    {
        logger.LogInformation("Testing database context...");
        
        var context = services.GetRequiredService<DailyTaskManagerDbContext>();
        await context.EnsureDatabaseCreatedAsync();
        
        // Create test user
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordHash = "test_hash"
        };
        
        context.Users.Add(user);
        await context.SaveChangesAsync();
        
        logger.LogInformation("✅ Database context test passed");
    }
    
    static async Task TestAuthenticationService(IServiceProvider services, ILogger logger)
    {
        logger.LogInformation("Testing authentication service...");
        
        var authService = services.GetRequiredService<IAuthenticationService>();
        
        // Test registration
        var result = await authService.RegisterAsync("testuser2", "test2@example.com", "password123");
        
        if (result.IsSuccess)
        {
            logger.LogInformation("✅ User registration test passed");
            logger.LogInformation($"   Created user: {result.User?.Username}");
        }
        else
        {
            logger.LogWarning($"⚠️ Registration failed: {result.ErrorMessage}");
        }
        
        // Test login
        var loginRequest = new LoginRequest
        {
            Username = "testuser2",
            Password = "password123"
        };
        
        var loginResult = await authService.LoginAsync(loginRequest);
        
        if (loginResult.IsSuccess)
        {
            logger.LogInformation("✅ User login test passed");
            logger.LogInformation($"   Authenticated user: {loginResult.User?.Username}");
        }
        else
        {
            logger.LogWarning($"⚠️ Login failed: {loginResult.ErrorMessage}");
        }
    }
    
    static async Task TestExportService(IServiceProvider services, ILogger logger)
    {
        logger.LogInformation("Testing export service...");
        
        var exportService = services.GetRequiredService<IExportService>();
        
        // Test available formats
        var formats = exportService.GetAvailableFormats();
        logger.LogInformation($"✅ Available export formats: {string.Join(", ", formats)}");
        
        // Test validation
        var tasks = new List<DailyTask>
        {
            new DailyTask { Description = "Test Task 1", Hours = 8, Date = DateTime.Today },
            new DailyTask { Description = "Test Task 2", Hours = 4, Date = DateTime.Today.AddDays(-1) }
        };
        
        var isValid = exportService.ValidateExportParameters(tasks, "Excel");
        logger.LogInformation($"✅ Export validation test: {isValid}");
        
        // Test CSV export
        try
        {
            var csvPath = await exportService.ExportTasksToCsvAsync(tasks);
            logger.LogInformation($"✅ CSV export test passed: {csvPath}");
        }
        catch (Exception ex)
        {
            logger.LogWarning($"⚠️ CSV export test failed: {ex.Message}");
        }
    }
}