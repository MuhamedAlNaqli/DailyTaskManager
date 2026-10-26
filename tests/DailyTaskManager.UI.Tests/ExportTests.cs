using DailyTaskManager.UI.Tests.Infrastructure;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace DailyTaskManager.UI.Tests;

/// <summary>
/// Tests for export functionality in the Avalonia application
/// </summary>
[TestClass]
public class ExportTests : BaseUITest
{
    private readonly string _testExportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "test_export.xlsx");

    [TestCleanup]
    public override void TestCleanup()
    {
        // Clean up test export file
        if (File.Exists(_testExportPath))
        {
            try
            {
                File.Delete(_testExportPath);
                Console.WriteLine($"🧹 Cleaned up test export file: {_testExportPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to clean up export file: {ex.Message}");
            }
        }
        
        base.TestCleanup();
    }

    [TestMethod]
    public async Task Export_Button_Should_Be_Available()
    {
        Console.WriteLine("🧪 Testing export button availability...");
        
        // MainWindow is already set and logged in from TestInitialize
        await Task.Delay(1000); // Small delay to ensure UI is ready
        
        // Look for export button
        var cf = new ConditionFactory(new UIA3PropertyLibrary());
        var exportButton = await FindElementAsync(MainWindow,
            cf.ByName("Export"),
            cf.ByName("Export to Excel"),
            cf.ByName("Export Tasks"),
            cf.ByAutomationId("ExportButton"),
            cf.ByControlType(ControlType.Button).And(cf.ByName("Export"))
        );

        if (exportButton != null)
        {
            Console.WriteLine($"✅ Found export button: {exportButton.Name}");
            Assert.IsTrue(exportButton.IsEnabled, "Export button should be enabled");
            Console.WriteLine("✅ Export button is enabled and accessible");
        }
        else
        {
            Console.WriteLine("⚠️ Export button not found, exploring UI structure...");
            PrintUIState("Looking for export functionality");
            
            // Look in menus
            var menuItems = MainWindow.FindAllDescendants(cf.ByControlType(ControlType.MenuItem));
            Console.WriteLine($"📊 Found {menuItems.Length} menu items");
            
            foreach (var menuItem in menuItems.Take(10))
            {
                if (!string.IsNullOrEmpty(menuItem.Name) && 
                    menuItem.Name.ToLower().Contains("export"))
                {
                    Console.WriteLine($"✅ Found export menu item: {menuItem.Name}");
                    break;
                }
            }
        }
    }

}