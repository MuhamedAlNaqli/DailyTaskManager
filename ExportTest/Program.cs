using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DailyTaskManager.Core.Models;
using DailyTaskManager.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    static async Task Main(string[] args)
    {
        // Setup logging
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetService<ILogger<ExportService>>();

        // Create export service
        var exportService = new ExportService(logger);

        // Create sample test data
        var testTasks = new List<DailyTask>
        {
            new DailyTask
            {
                Id = 1,
                Description = "Implementacija nove funkcionalnosti za upravljanje korisnicima",
                Date = new DateTime(2024, 7, 1),
                Hours = 8.0m,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new DailyTask
            {
                Id = 2,
                Description = "Kreiranje unit testova za servisni sloj",
                Date = new DateTime(2024, 7, 2),
                Hours = 4.5m,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new DailyTask
            {
                Id = 3,
                Description = "Optimizacija performansi baze podataka",
                Date = new DateTime(2024, 7, 3),
                Hours = 6.0m,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new DailyTask
            {
                Id = 4,
                Description = "Code review i refactoring legacy koda",
                Date = new DateTime(2024, 7, 4),
                Hours = 5.5m,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new DailyTask
            {
                Id = 5,
                Description = "Dokumentacija API specifikacije",
                Date = new DateTime(2024, 7, 5),
                Hours = 3.0m,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            }
        };

        try
        {
            Console.WriteLine("Testing current Excel export format...");

            var exportPath = Path.Combine(@"D:\Internal Projects\DailyTaskManager", "current_export_test.xlsx");
            var result = await exportService.ExportTasksToExcelAsync(testTasks, exportPath);

            Console.WriteLine($"✅ Export successful! File created at: {result}");
            Console.WriteLine($"📊 Exported {testTasks.Count} tasks");

            // Check if file exists and show basic info
            if (File.Exists(result))
            {
                var fileInfo = new FileInfo(result);
                Console.WriteLine($"📄 File size: {fileInfo.Length} bytes");
                Console.WriteLine($"📅 Created: {fileInfo.CreationTime}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Export failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}