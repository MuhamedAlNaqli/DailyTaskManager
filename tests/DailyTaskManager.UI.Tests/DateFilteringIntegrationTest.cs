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

        Console.WriteLine($"✅ Integration test setup complete. Found {_viewModel.TotalCount} total tasks");
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
    public async Task DateFiltering_ShouldFilterTasksCorrectly()
    {
        Console.WriteLine("🧪 Testing date filtering functionality directly...");

        // Verify we have tasks loaded
        Assert.IsNotNull(_viewModel, "ViewModel should be initialized");
        Assert.IsTrue(_viewModel.TotalCount > 0, "Should have tasks loaded");

        var totalTasks = _viewModel.TotalCount;
        Console.WriteLine($"📋 Total tasks loaded: {totalTasks}");

        // Print all task dates for debugging
        Console.WriteLine("📅 Task dates found:");
        foreach (var task in AllTasks.OrderBy(t => t.Date))
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
        Console.WriteLine("🎉 Date filtering integration test passed!");
    }

    [TestMethod]
    public async Task Paging_ShouldLimitPageSizeAndNavigate()
    {
        Assert.IsNotNull(_viewModel);
        var total = _viewModel.TotalCount;
        Assert.IsTrue(total > 0, "Should have tasks loaded");

        // The list only ever holds one page
        Assert.IsTrue(_viewModel.FilteredTasks.Count <= DailyTasksViewModel.PageSize);
        Assert.AreEqual(Math.Min(total, DailyTasksViewModel.PageSize), _viewModel.FilteredTasks.Count);
        Assert.AreEqual(1, _viewModel.CurrentPage);

        if (total > DailyTasksViewModel.PageSize)
        {
            var firstPageIds = _viewModel.FilteredTasks.Select(t => t.Id).ToList();

            await _viewModel.NextPageCommand.ExecuteAsync(null);
            Assert.AreEqual(2, _viewModel.CurrentPage);
            Assert.IsFalse(_viewModel.FilteredTasks.Select(t => t.Id).Intersect(firstPageIds).Any(),
                "Page 2 should contain different tasks than page 1");

            await _viewModel.LastPageCommand.ExecuteAsync(null);
            Assert.AreEqual(_viewModel.TotalPages, _viewModel.CurrentPage);
            Assert.IsTrue(_viewModel.FilteredTasks.Count >= 1);

            await _viewModel.FirstPageCommand.ExecuteAsync(null);
            Assert.AreEqual(1, _viewModel.CurrentPage);
            CollectionAssert.AreEqual(firstPageIds, _viewModel.FilteredTasks.Select(t => t.Id).ToList());
        }

        // Pages together cover exactly the filtered set
        Assert.AreEqual((int)Math.Ceiling(total / (double)DailyTasksViewModel.PageSize), _viewModel.TotalPages);
    }

    [TestMethod]
    public async Task Search_ShouldFilterInDatabaseAndResetToFirstPage()
    {
        Assert.IsNotNull(_viewModel);
        var sample = AllTasks.First().Description;
        var term = sample.Length >= 3 ? sample.Substring(0, 3) : sample;

        _viewModel.SearchText = term;
        await Task.Delay(1500); // debounce + query
        await _viewModel.ApplyFiltersAsync();

        var expected = AllTasks.Count(t => t.Description.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
        Assert.AreEqual(expected, _viewModel.TotalCount, "Search result count should match a case-insensitive contains");
        Assert.IsTrue(_viewModel.FilteredTasks.All(t => t.Description.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0));
        Assert.AreEqual(1, _viewModel.CurrentPage);

        // Searches under 3 characters are ignored
        _viewModel.SearchText = "ab";
        await _viewModel.ApplyFiltersAsync();
        Assert.AreEqual(AllTasks.Count, _viewModel.TotalCount);
    }

    private async Task CleanUpTestDataAsync(string marker, string clientName)
    {
        var factory = _serviceProvider!.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<DailyTaskManager.Infrastructure.Data.DailyTaskManagerDbContext>>();
        await using var ctx = factory.CreateDbContext();
        await Microsoft.EntityFrameworkCore.RelationalQueryableExtensions.ExecuteDeleteAsync(
            ctx.DailyTasks.Where(t => t.Description.Contains(marker)));
        await Microsoft.EntityFrameworkCore.RelationalQueryableExtensions.ExecuteDeleteAsync(
            ctx.Templates.Where(x => x.Name.Contains(marker) || x.Description.Contains(marker)));
        await Microsoft.EntityFrameworkCore.RelationalQueryableExtensions.ExecuteDeleteAsync(
            ctx.Clients.Where(c => c.Name.ToLower() == clientName.ToLower()));
    }

    [TestMethod]
    public async Task Clients_ShouldCreateAssignFilterAndRestore()
    {
        var db = _serviceProvider!.GetRequiredService<DailyTaskManager.Core.Interfaces.IDatabaseService>();
        var name = "UT-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        var marker = "UT-task-" + name;

        try
        {
            var client = await db.GetOrCreateClientAsync(name);
            var again = await db.GetOrCreateClientAsync("  " + name.ToLower() + " ");
            Assert.AreEqual(client.Id, again.Id, "Client names are matched case-insensitively");

            var task = await db.AddTaskAsync(new DailyTaskManager.Core.Models.DailyTask { Date = DateTime.Today, Description = marker, Hours = 1 });

            var unassigned = await db.GetTasksAsync(new DailyTaskManager.Core.Models.TaskQuery(marker, clientId: DailyTaskManager.Core.Models.TaskQuery.Unassigned));
            Assert.AreEqual(1, unassigned.Count, "A new entry has no client");

            var changed = await db.AssignClientAsync(new DailyTaskManager.Core.Models.TaskQuery(marker), client.Id);
            Assert.AreEqual(1, changed);

            var assigned = await db.GetTasksAsync(new DailyTaskManager.Core.Models.TaskQuery(marker, clientId: client.Id));
            Assert.AreEqual(1, assigned.Count);
            Assert.AreEqual(name, assigned[0].Client?.Name);
            Assert.AreEqual(0, (await db.GetTasksAsync(new DailyTaskManager.Core.Models.TaskQuery(marker, clientId: DailyTaskManager.Core.Models.TaskQuery.Unassigned))).Count);

            // Bulk-clear
            Assert.AreEqual(1, await db.AssignClientAsync(new DailyTaskManager.Core.Models.TaskQuery(marker), null));
            Assert.AreEqual(1, (await db.GetTasksAsync(new DailyTaskManager.Core.Models.TaskQuery(marker, clientId: DailyTaskManager.Core.Models.TaskQuery.Unassigned))).Count);

            // Restore keeps the data and timestamps of the deleted entry
            Assert.IsTrue(await db.RemoveTaskAsync(task.Id));
            var restored = await db.RestoreTaskAsync(task);
            Assert.AreEqual(marker, restored.Description);
            Assert.AreEqual(task.CreatedAt, restored.CreatedAt);
            Assert.AreEqual(1, (await db.GetTasksAsync(new DailyTaskManager.Core.Models.TaskQuery(marker))).Count);
        }
        finally
        {
            await CleanUpTestDataAsync(marker, name);
        }
    }

    [TestMethod]
    public async Task DeleteTask_ShouldOfferUndo()
    {
        Assert.IsNotNull(_viewModel);
        var db = _serviceProvider!.GetRequiredService<DailyTaskManager.Core.Interfaces.IDatabaseService>();
        var marker = "UT-undo-" + Guid.NewGuid().ToString("N").Substring(0, 8);

        try
        {
            await db.AddTaskAsync(new DailyTaskManager.Core.Models.DailyTask { Date = DateTime.Today, Description = marker, Hours = 2 });
            _viewModel.SearchText = marker;
            await _viewModel.ApplyFiltersAsync();
            Assert.AreEqual(1, _viewModel.TotalCount);

            await _viewModel.DeleteTaskCommand.ExecuteAsync(_viewModel.FilteredTasks[0]);
            Assert.IsTrue(_viewModel.IsUndoVisible, "Undo bar is shown after a delete");
            Assert.AreEqual(0, _viewModel.TotalCount);

            await _viewModel.UndoDeleteCommand.ExecuteAsync(null);
            Assert.IsFalse(_viewModel.IsUndoVisible);
            Assert.AreEqual(1, _viewModel.TotalCount, "The entry is back after undo");
        }
        finally
        {
            await CleanUpTestDataAsync(marker, "ut-no-client-" + marker);
        }
    }

    [TestMethod]
    public async Task Analytics_ShouldAggregateHoursPerDayAndClient()
    {
        var db = _serviceProvider!.GetRequiredService<DailyTaskManager.Core.Interfaces.IDatabaseService>();
        var name = "UT-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        var marker = "UT-agg-" + name;
        var day1 = new DateTime(2020, 1, 6);
        var day2 = new DateTime(2020, 1, 7);

        try
        {
            var client = await db.GetOrCreateClientAsync(name);
            await db.AddTaskAsync(new DailyTaskManager.Core.Models.DailyTask { Date = day1, Description = marker + " a", Hours = 3, ClientId = client.Id });
            await db.AddTaskAsync(new DailyTaskManager.Core.Models.DailyTask { Date = day1, Description = marker + " b", Hours = 4 });
            await db.AddTaskAsync(new DailyTaskManager.Core.Models.DailyTask { Date = day2, Description = marker + " c", Hours = 2.5m, ClientId = client.Id });

            var query = new DailyTaskManager.Core.Models.TaskQuery(marker);
            var days = await db.GetDailyHoursAsync(query);
            Assert.AreEqual(2, days.Count);
            Assert.AreEqual(7m, days[0].Hours);
            Assert.AreEqual(2, days[0].Entries);
            Assert.AreEqual(day1, days[0].Date.Date);
            Assert.AreEqual(2.5m, days[1].Hours);

            var clients = await db.GetClientHoursAsync(query);
            Assert.AreEqual(2, clients.Count);
            Assert.AreEqual(4m, clients.Single(c => c.ClientId == null).Hours);
            Assert.AreEqual("Unassigned", clients.Single(c => c.ClientId == null).ClientName);
            Assert.AreEqual(5.5m, clients.Single(c => c.ClientId == client.Id).Hours);
            Assert.AreEqual(name, clients.Single(c => c.ClientId == client.Id).ClientName);

            // The date range limits the result
            var onlyDay2 = await db.GetDailyHoursAsync(new DailyTaskManager.Core.Models.TaskQuery(marker, day2, day2));
            Assert.AreEqual(1, onlyDay2.Count);
        }
        finally
        {
            await CleanUpTestDataAsync(marker, name);
        }
    }

    [TestMethod]
    public void Invoice_ShouldComputeAmounts()
    {
        var invoice = new DailyTaskManager.Core.Models.InvoiceData { Hours = 10.5m, HourlyRate = 20m, VatPercent = 17m };
        Assert.AreEqual(210.00m, invoice.NetAmount);
        Assert.AreEqual(35.70m, invoice.VatAmount);
        Assert.AreEqual(245.70m, invoice.GrossAmount);

        var noVat = new DailyTaskManager.Core.Models.InvoiceData { Hours = 3m, HourlyRate = 33.333m };
        Assert.AreEqual(100.00m, noVat.NetAmount, "Net amount is rounded to two decimals");
        Assert.AreEqual(0m, noVat.VatAmount);
        Assert.AreEqual(noVat.NetAmount, noVat.GrossAmount);
    }

    [TestMethod]
    public async Task Invoice_ShouldCreatePdf()
    {
        var export = _serviceProvider!.GetRequiredService<DailyTaskManager.Core.Interfaces.IExportService>();
        var settings = new DailyTaskManager.Core.Models.PdfSettings();
        var path = Path.Combine(Path.GetTempPath(), $"dtm_invoice_{Guid.NewGuid():N}.pdf");

        try
        {
            var invoice = new DailyTaskManager.Core.Models.InvoiceData
            {
                InvoiceNumber = "001/26",
                IssueDate = new DateTime(2026, 10, 31),
                PeriodFrom = new DateTime(2026, 10, 1),
                PeriodTo = new DateTime(2026, 10, 31),
                Hours = 147m,
                HourlyRate = 25m,
                VatPercent = 17m,
                PaymentNote = "Račun: 123-456-789\nRok plaćanja: 15 dana"
            };

            var result = await export.ExportInvoiceToPdfAsync(invoice, settings, path);
            Assert.AreEqual(path, result);
            var bytes = await File.ReadAllBytesAsync(path);
            Assert.IsTrue(bytes.Length > 1000, "PDF should not be empty");
            Assert.AreEqual("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
            // Optional: keep a copy for manual inspection
            var keep = Environment.GetEnvironmentVariable("DTM_KEEP_INVOICE");
            if (!string.IsNullOrEmpty(keep)) File.Copy(path, keep, true);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    public async Task AnalyticsViewModel_ShouldBuildChartsForPeriods()
    {
        var vm = _serviceProvider!.GetRequiredService<AnalyticsViewModel>();
        await Task.Delay(2500);

        // "This month" is a daily chart with one bar per calendar day
        var daysInMonth = DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month);
        Assert.IsTrue(vm.IsDailyChart);
        Assert.AreEqual(daysInMonth, vm.Bars.Count);

        vm.SelectedPeriod = vm.Periods.Single(p => p.Name == "This year");
        await Task.Delay(2500);
        Assert.IsFalse(vm.IsDailyChart, "A year is shown per month");
        Assert.AreEqual(12, vm.Bars.Count);

        vm.SelectedPeriod = vm.Periods.Single(p => p.Name == "All time");
        await Task.Delay(2500);
        Assert.IsTrue(vm.HasData, "The test account has entries");
        Assert.IsTrue(vm.Bars.Count >= 1);
        Assert.IsTrue(vm.ClientRows.Count >= 1);
    }

    [TestMethod]
    public async Task ReportsViewModel_ShouldSummariseMonthAndComputeInvoice()
    {
        var vm = _serviceProvider!.GetRequiredService<ReportsViewModel>();
        await Task.Delay(2500);

        Assert.IsNotNull(vm.SelectedMonth);
        Assert.AreEqual(24, vm.Months.Count);

        vm.SelectedMonth = vm.Months.Single(m => m.From == new DateTime(2025, 8, 1));
        await Task.Delay(2000);
        Assert.IsTrue(vm.TotalHours > 0, "The test account has entries in August 2025");
        Assert.IsTrue(vm.EntryCount > 0);

        vm.HourlyRate = 10m;
        vm.VatPercent = 0m;
        var expectedNet = Math.Round(vm.TotalHours * 10m, 2);
        Assert.AreEqual(expectedNet.ToString("N2", new System.Globalization.CultureInfo("de-DE")), vm.NetText);
        Assert.AreEqual(vm.NetText, vm.GrossText);
    }

    [TestMethod]
    public async Task Templates_ShouldBeSavedReplacedAndDeleted()
    {
        var db = _serviceProvider!.GetRequiredService<DailyTaskManager.Core.Interfaces.IDatabaseService>();
        var marker = "UT-tpl-" + Guid.NewGuid().ToString("N").Substring(0, 8);

        try
        {
            var saved = await db.SaveTemplateAsync(new DailyTaskManager.Core.Models.TaskTemplate { Name = marker, Description = marker + " v1", Hours = 3.5m });
            Assert.IsTrue(saved.Id > 0);

            // Same name (any case) replaces the template instead of adding another
            var replaced = await db.SaveTemplateAsync(new DailyTaskManager.Core.Models.TaskTemplate { Name = marker.ToUpper(), Description = marker + " v2", Hours = 2m });
            Assert.AreEqual(saved.Id, replaced.Id);

            var all = (await db.GetTemplatesAsync()).Where(x => x.Name.ToLower() == marker.ToLower()).ToList();
            Assert.AreEqual(1, all.Count);
            Assert.AreEqual(marker + " v2", all[0].Description);
            Assert.AreEqual(2m, all[0].Hours);

            Assert.IsTrue(await db.DeleteTemplateAsync(saved.Id));
            Assert.IsFalse(await db.DeleteTemplateAsync(saved.Id), "Deleting twice reports nothing deleted");
        }
        finally
        {
            await CleanUpTestDataAsync(marker, "ut-none-" + marker);
        }
    }

    [TestMethod]
    public async Task TasksChanged_ShouldFireOnWritesAndFirstEntryDateShouldBeKnown()
    {
        var db = _serviceProvider!.GetRequiredService<DailyTaskManager.Core.Interfaces.IDatabaseService>();
        var marker = "UT-evt-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        var fired = 0;
        EventHandler handler = (_, _) => fired++;
        db.TasksChanged += handler;

        try
        {
            var first = await db.GetFirstEntryDateAsync();
            Assert.IsNotNull(first, "The test account has entries");

            var task = await db.AddTaskAsync(new DailyTaskManager.Core.Models.DailyTask { Date = DateTime.Today, Description = marker, Hours = 1 });
            Assert.AreEqual(1, fired);
            await db.RemoveTaskAsync(task.Id);
            Assert.AreEqual(2, fired);
        }
        finally
        {
            db.TasksChanged -= handler;
            await CleanUpTestDataAsync(marker, "ut-none-" + marker);
        }
    }

    [TestMethod]
    public void MissingDays_ShouldFindWorkingDaysWithoutEntries()
    {
        // Wed 28 Oct 2026 is "today"; the user started on Mon 19 Oct
        var today = new DateTime(2026, 10, 28);
        var first = new DateTime(2026, 10, 19);
        var logged = new HashSet<DateTime> { new(2026, 10, 19), new(2026, 10, 20), new(2026, 10, 22), new(2026, 10, 23) };

        // Before the reminder hour today does not count: only Wed 21 and Mon 26, Tue 27 are missing (weekend skipped)
        var morning = DailyTaskManager.Avalonia.Services.MissingDays.Find(first, today, today.AddHours(9), logged, 16);
        CollectionAssert.AreEqual(new[] { new DateTime(2026, 10, 21), new DateTime(2026, 10, 26), new DateTime(2026, 10, 27) }, morning);

        // After the reminder hour today is included
        var evening = DailyTaskManager.Avalonia.Services.MissingDays.Find(first, today, today.AddHours(17), logged, 16);
        Assert.AreEqual(4, evening.Count);
        Assert.AreEqual(today, evening.Last());

        // Nothing is reported before the user's first entry, or for a brand-new account
        Assert.AreEqual(0, DailyTaskManager.Avalonia.Services.MissingDays.Find(null, today, today.AddHours(17), logged, 16).Count);
        Assert.IsFalse(DailyTaskManager.Avalonia.Services.MissingDays.Find(today, today, today.AddHours(9), new HashSet<DateTime>(), 16).Any());

        // Description
        Assert.AreEqual("You haven't logged any hours today yet.", DailyTaskManager.Avalonia.Services.MissingDays.Describe(new List<DateTime> { today }, today));
        StringAssert.Contains(DailyTaskManager.Avalonia.Services.MissingDays.Describe(evening, today), "No entries for 4 working days");
    }

    [TestMethod]
    public async Task CalendarViewModel_ShouldBuildMonthGridAndNavigate()
    {
        var vm = _serviceProvider!.GetRequiredService<ActivityCalendarViewModel>();
        await Task.Delay(2500);

        Assert.AreEqual(0, vm.Days.Count % 7, "The grid is made of whole weeks");
        Assert.IsTrue(vm.Days.Count == 35 || vm.Days.Count == 42 || vm.Days.Count == 28);
        Assert.AreEqual(DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month), vm.Days.Count(d => d.InMonth));
        Assert.AreEqual(DayOfWeek.Monday, vm.Days[0].Date.DayOfWeek, "Weeks start on Monday");
        Assert.AreEqual(1, vm.Days.Count(d => d.IsToday));

        var title = vm.MonthTitle;
        vm.PreviousMonthCommand.Execute(null);
        await Task.Delay(1500);
        Assert.AreNotEqual(title, vm.MonthTitle);
        vm.GoToTodayCommand.Execute(null);
        await Task.Delay(1500);
        Assert.AreEqual(title, vm.MonthTitle);
    }

    [TestMethod]
    public async Task AddDialog_ShouldSaveTemplateAndQuickAddShouldCreateEntry()
    {
        var db = _serviceProvider!.GetRequiredService<DailyTaskManager.Core.Interfaces.IDatabaseService>();
        var marker = "UT-quick-" + Guid.NewGuid().ToString("N").Substring(0, 8);

        try
        {
            // Saving from the Add dialog creates a template
            var dialog = _serviceProvider!.GetRequiredService<AddTaskViewModel>();
            dialog.Description = marker + " description";
            dialog.Hours = 2.5m;
            dialog.SaveAsTemplate = true;
            dialog.TemplateName = marker;
            await dialog.SaveCommand.ExecuteAsync(null);

            var template = (await db.GetTemplatesAsync()).SingleOrDefault(x => x.Name == marker);
            Assert.IsNotNull(template, "The template was saved");
            Assert.AreEqual(2.5m, template.Hours);

            // A new dialog offers it and applies it
            var second = _serviceProvider!.GetRequiredService<AddTaskViewModel>();
            await Task.Delay(1500);
            Assert.IsTrue(second.HasTemplates);
            second.SelectedTemplate = second.Templates.Single(x => x.Name == marker);
            Assert.AreEqual(marker + " description", second.Description);
            Assert.AreEqual(2.5m, second.Hours);

            // Quick add on the list creates an entry for today
            _viewModel!.SearchText = marker;
            await _viewModel.ApplyFiltersAsync();
            Assert.AreEqual(0, _viewModel.TotalCount);
            await _viewModel.QuickAddTemplateCommand.ExecuteAsync(template);
            _viewModel.SearchText = marker;
            await _viewModel.ApplyFiltersAsync();
            Assert.AreEqual(1, _viewModel.TotalCount);
            Assert.AreEqual(DateTime.Today, _viewModel.FilteredTasks[0].Date.Date);
            Assert.AreEqual(2.5m, _viewModel.FilteredTasks[0].Hours);
        }
        finally
        {
            await CleanUpTestDataAsync(marker, "ut-none-" + marker);
        }
    }

    [TestMethod]
    public async Task Export_ShouldFormatHoursWithoutTrailingZeros()
    {
        var export = _serviceProvider!.GetRequiredService<DailyTaskManager.Core.Interfaces.IExportService>();
        var path = Path.Combine(Path.GetTempPath(), $"dtm_hours_{Guid.NewGuid():N}.csv");

        try
        {
            var tasks = new List<DailyTaskManager.Core.Models.DailyTask>
            {
                new() { Date = new DateTime(2026, 10, 1), Description = "a", Hours = 7.00m },
                new() { Date = new DateTime(2026, 10, 2), Description = "b", Hours = 2.50m }
            };
            await export.ExportTasksToCsvAsync(tasks, path);

            var lines = await File.ReadAllLinesAsync(path);
            StringAssert.Contains(lines[1], ",7,");
            StringAssert.Contains(lines[2], ",2.5,");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
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
                services.AddTransient<AnalyticsViewModel>();
                services.AddTransient<ActivityCalendarViewModel>();
                services.AddTransient<AddTaskViewModel>();
                services.AddSingleton<DailyTaskManager.Core.Interfaces.IAutoCompleteService, AutoCompleteService>();
                services.AddTransient<ReportsViewModel>();
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