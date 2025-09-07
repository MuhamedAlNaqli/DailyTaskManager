using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using DailyTaskManager.Core.Entities;
using DailyTaskManager.Infrastructure.Data;
using DailyTaskManager.Core.Interfaces;

namespace CreateSampleTasks
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Creating sample tasks for authenticated user...");
            
            var host = CreateHostBuilder().Build();
            using var scope = host.Services.CreateScope();
            
            var context = scope.ServiceProvider.GetRequiredService<DailyTaskManagerDbContext>();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            var taskService = scope.ServiceProvider.GetRequiredService<IDailyTaskService>();
            
            try
            {
                // Get the authenticated user (muhamed.alnaqli@gmail.com)
                var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "muhamed.alnaqli@gmail.com");
                if (user == null)
                {
                    Console.WriteLine("User not found!");
                    return;
                }
                
                Console.WriteLine($"Found user: {user.DisplayName} ({user.Email})");
                
                // Check existing tasks
                var existingTasks = await context.DailyTasks.Where(t => t.UserId == user.Id).ToListAsync();
                Console.WriteLine($"Existing tasks for user: {existingTasks.Count}");
                
                if (existingTasks.Count == 0)
                {
                    Console.WriteLine("Creating sample tasks...");
                    
                    var today = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc);
                    var yesterday = DateTime.SpecifyKind(DateTime.Today.AddDays(-1), DateTimeKind.Utc);
                    var twoDaysAgo = DateTime.SpecifyKind(DateTime.Today.AddDays(-2), DateTimeKind.Utc);
                    
                    var sampleTasks = new[]
                    {
                        new DailyTask
                        {
                            Description = "Updated project documentation and README files",
                            TotalHours = 2.5m,
                            TaskDate = today,
                            UserId = user.Id,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        },
                        new DailyTask
                        {
                            Description = "Implemented OAuth authentication for Google sign-in",
                            TotalHours = 4.0m,
                            TaskDate = today,
                            UserId = user.Id,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        },
                        new DailyTask
                        {
                            Description = "Fixed database connection issues with PostgreSQL",
                            TotalHours = 3.5m,
                            TaskDate = yesterday,
                            UserId = user.Id,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        },
                        new DailyTask
                        {
                            Description = "Migrated UI from WPF to Avalonia framework",
                            TotalHours = 6.0m,
                            TaskDate = yesterday,
                            UserId = user.Id,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        },
                        new DailyTask
                        {
                            Description = "Designed modern UI inspired by Clockify interface",
                            TotalHours = 4.5m,
                            TaskDate = twoDaysAgo,
                            UserId = user.Id,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        }
                    };
                    
                    await context.DailyTasks.AddRangeAsync(sampleTasks);
                    await context.SaveChangesAsync();
                    
                    Console.WriteLine($"Successfully created {sampleTasks.Length} sample tasks!");
                }
                else
                {
                    Console.WriteLine("Tasks already exist, skipping creation.");
                }
                
                // Verify tasks were created
                var allTasks = await context.DailyTasks
                    .Where(t => t.UserId == user.Id)
                    .OrderByDescending(t => t.TaskDate)
                    .ToListAsync();
                
                Console.WriteLine($"\nTotal tasks for user: {allTasks.Count}");
                foreach (var task in allTasks)
                {
                    Console.WriteLine($"- {task.TaskDate:yyyy-MM-dd}: {task.Description} ({task.TotalHours}h)");
                }
                
                Console.WriteLine("\nSample task creation completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        static IHostBuilder CreateHostBuilder()
        {
            var connectionString = "Host=localhost;Port=5432;Database=DailyTaskManager;Username=postgres;Password=admin;";
            
            return Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddDbContext<DailyTaskManagerDbContext>(options =>
                        options.UseNpgsql(connectionString));
                    
                    services.AddScoped<IUserService, DailyTaskManager.Infrastructure.Services.UserService>();
                    services.AddScoped<IDailyTaskService, DailyTaskManager.Infrastructure.Services.DailyTaskService>();
                });
        }
    }
}