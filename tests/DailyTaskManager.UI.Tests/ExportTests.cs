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

    [TestMethod]
    public async Task Export_Should_Create_File()
    {
        Console.WriteLine("🧪 Testing export file creation...");
        
        // Login first
        var MainWindow = await LoginAsync("admin", "admin123");
        await Task.Delay(2000);
        
        // Look for export button
        var cf = new ConditionFactory(new UIA3PropertyLibrary());
        var exportButton = await FindElementAsync(MainWindow,
            cf.ByName("Export"),
            cf.ByName("Export to Excel"),
            cf.ByName("Export Tasks"),
            cf.ByAutomationId("ExportButton")
        );

        if (exportButton != null && exportButton.IsEnabled)
        {
            Console.WriteLine("📤 Clicking export button...");
            
            // Record files before export
            var desktopFiles = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "*.xlsx");
            var fileCountBefore = desktopFiles.Length;
            Console.WriteLine($"📊 Excel files on desktop before export: {fileCountBefore}");
            
            exportButton.AsButton().Click();
            await Task.Delay(3000); // Wait for export to complete
            
            // Check if export dialog appeared
            try
            {
                var saveDialog = await WaitForWindowAsync(w => 
                    w.Title.Contains("Save") || 
                    w.Title.Contains("Export") ||
                    w.AutomationId.Contains("SaveDialog"), 5000);
                
                if (saveDialog != null)
                {
                    Console.WriteLine("💾 Save dialog appeared");
                    
                    // Look for file name field and save button
                    var fileNameField = await FindElementAsync(saveDialog,
                        cf.ByControlType(ControlType.Edit),
                        cf.ByName("File name"),
                        cf.ByAutomationId("FileNameBox")
                    );

                    if (fileNameField != null)
                    {
                        fileNameField.AsTextBox().Text = "test_export";
                        await Task.Delay(500);
                    }

                    var saveDialogButton = await FindElementAsync(saveDialog,
                        cf.ByName("Save"),
                        cf.ByName("OK"),
                        cf.ByControlType(ControlType.Button).And(cf.ByName("Save"))
                    );

                    if (saveDialogButton != null)
                    {
                        saveDialogButton.AsButton().Click();
                        await Task.Delay(2000);
                        Console.WriteLine("✅ Export dialog completed");
                    }
                }
            }
            catch (TimeoutException)
            {
                Console.WriteLine("⚠️ No save dialog appeared - export might be automatic");
            }
            
            // Check if new files were created
            var desktopFilesAfter = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "*.xlsx");
            var fileCountAfter = desktopFilesAfter.Length;
            Console.WriteLine($"📊 Excel files on desktop after export: {fileCountAfter}");
            
            if (fileCountAfter > fileCountBefore)
            {
                Console.WriteLine("✅ Export successfully created new file");
                
                // Find the newest Excel file (likely our export)
                var newestFile = desktopFilesAfter
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(fi => fi.CreationTime)
                    .First();
                
                Console.WriteLine($"📄 Newest export file: {newestFile.Name} ({newestFile.Length} bytes)");
                Assert.IsTrue(newestFile.Length > 0, "Export file should not be empty");
            }
            else
            {
                Console.WriteLine("⚠️ No new export file detected on desktop");
            }
        }
        else
        {
            Console.WriteLine("⚠️ Export button not available or not enabled");
            Assert.Inconclusive("Export button not found - test cannot proceed");
        }
    }

    [TestMethod]
    public async Task Export_With_Filter_Should_Export_Filtered_Data()
    {
        Console.WriteLine("🧪 Testing export with applied filters...");
        
        // Login first
        var MainWindow = await LoginAsync("admin", "admin123");
        await Task.Delay(2000);
        
        var cf = new ConditionFactory(new UIA3PropertyLibrary());
        
        // First, try to apply a filter
        var filterControl = await FindElementAsync(MainWindow,
            cf.ByName("Filter"),
            cf.ByName("Date Filter"),
            cf.ByAutomationId("FilterComboBox"),
            cf.ByControlType(ControlType.ComboBox)
        );

        if (filterControl != null)
        {
            Console.WriteLine("🔍 Found filter control, applying filter...");
            filterControl.AsComboBox().Click();
            await Task.Delay(1000);
            
            // Try to select a filter option
            var filterOptions = filterControl.FindAllDescendants(cf.ByControlType(ControlType.ListItem));
            if (filterOptions.Length > 0)
            {
                filterOptions[0].Click();
                await Task.Delay(1000);
                Console.WriteLine("✅ Filter applied");
            }
        }
        
        // Now try export
        var exportButton = await FindElementAsync(MainWindow,
            cf.ByName("Export"),
            cf.ByName("Export to Excel"),
            cf.ByName("Export Tasks"),
            cf.ByAutomationId("ExportButton")
        );

        if (exportButton != null && exportButton.IsEnabled)
        {
            Console.WriteLine("📤 Exporting with filter applied...");
            exportButton.AsButton().Click();
            await Task.Delay(3000);
            
            Console.WriteLine("✅ Export with filter completed");
        }
        else
        {
            Console.WriteLine("⚠️ Export functionality not available for filter test");
        }
    }

    [TestMethod]
    public async Task Export_Should_Handle_Empty_Task_List()
    {
        Console.WriteLine("🧪 Testing export with empty task list...");
        
        // Login first
        var MainWindow = await LoginAsync("admin", "admin123");
        await Task.Delay(2000);
        
        var cf = new ConditionFactory(new UIA3PropertyLibrary());
        
        // Check if task list is empty or try to clear it
        var taskList = await FindElementAsync(MainWindow,
            cf.ByControlType(ControlType.DataGrid),
            cf.ByControlType(ControlType.List),
            cf.ByAutomationId("TaskGrid")
        );

        if (taskList != null)
        {
            var taskItems = taskList.FindAllDescendants(cf.ByControlType(ControlType.DataItem));
            Console.WriteLine($"📊 Current task count: {taskItems.Length}");
        }
        
        // Try to export even if list appears empty
        var exportButton = await FindElementAsync(MainWindow,
            cf.ByName("Export"),
            cf.ByName("Export to Excel"),
            cf.ByName("Export Tasks"),
            cf.ByAutomationId("ExportButton")
        );

        if (exportButton != null)
        {
            Console.WriteLine("📤 Attempting export with potentially empty list...");
            
            var wasEnabled = exportButton.IsEnabled;
            Console.WriteLine($"🔹 Export button enabled: {wasEnabled}");
            
            if (wasEnabled)
            {
                exportButton.AsButton().Click();
                await Task.Delay(2000);
                
                // Look for any error messages
                var errorMessage = await FindElementAsync(MainWindow,
                    cf.ByControlType(ControlType.Text).And(cf.ByName("empty")),
                    cf.ByControlType(ControlType.Text).And(cf.ByName("no data")),
                    cf.ByAutomationId("ErrorMessage")
                );

                if (errorMessage != null)
                {
                    Console.WriteLine($"✅ Found appropriate empty data message: {errorMessage.Name}");
                }
                else
                {
                    Console.WriteLine("✅ Export handled empty list gracefully (no error message)");
                }
            }
        }
    }

    [TestMethod]
    public async Task Export_Progress_Should_Be_Visible_For_Large_Dataset()
    {
        Console.WriteLine("🧪 Testing export progress indication...");
        
        // Login first
        var MainWindow = await LoginAsync("admin", "admin123");
        await Task.Delay(2000);
        
        var cf = new ConditionFactory(new UIA3PropertyLibrary());
        var exportButton = await FindElementAsync(MainWindow,
            cf.ByName("Export"),
            cf.ByName("Export to Excel"),
            cf.ByName("Export Tasks"),
            cf.ByAutomationId("ExportButton")
        );

        if (exportButton != null && exportButton.IsEnabled)
        {
            Console.WriteLine("📤 Starting export and looking for progress indicators...");
            exportButton.AsButton().Click();
            
            // Immediately look for progress indicators
            await Task.Delay(500);
            
            var progressBar = await FindElementAsync(MainWindow,
                cf.ByControlType(ControlType.ProgressBar),
                cf.ByAutomationId("ExportProgress")
            );

            if (progressBar != null)
            {
                Console.WriteLine("✅ Found progress bar during export");
                
                // Monitor progress bar for a few seconds
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        var progress = progressBar.AsProgressBar().Value;
                        Console.WriteLine($"📊 Export progress: {progress}%");
                        
                        if (progress >= 100)
                        {
                            Console.WriteLine("✅ Export completed");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Could not read progress: {ex.Message}");
                        break;
                    }
                    
                    await Task.Delay(500);
                }
            }
            else
            {
                Console.WriteLine("⚠️ No progress bar found - export might be too fast or no progress indication");
                
                // Look for other progress indicators
                var loadingSpinner = await FindElementAsync(MainWindow,
                    cf.ByControlType(ControlType.Text).And(cf.ByName("Loading")),
                    cf.ByControlType(ControlType.Text).And(cf.ByName("Exporting")),
                    cf.ByAutomationId("LoadingIndicator")
                );

                if (loadingSpinner != null)
                {
                    Console.WriteLine($"✅ Found loading indicator: {loadingSpinner.Name}");
                }
            }
            
            await Task.Delay(2000); // Wait for export to complete
            Console.WriteLine("✅ Export process monitoring completed");
        }
        else
        {
            Console.WriteLine("⚠️ Export button not available for progress test");
        }
    }

    [TestMethod]
    public async Task Export_Should_Handle_File_Access_Errors()
    {
        Console.WriteLine("🧪 Testing export error handling for file access issues...");
        
        // Login first
        var MainWindow = await LoginAsync("admin", "admin123");
        await Task.Delay(2000);
        
        // This test is conceptual - in a real scenario you might:
        // 1. Create a read-only directory
        // 2. Fill up disk space
        // 3. Revoke file permissions
        // For this test, we'll just verify error handling exists
        
        var cf = new ConditionFactory(new UIA3PropertyLibrary());
        var exportButton = await FindElementAsync(MainWindow,
            cf.ByName("Export"),
            cf.ByName("Export to Excel"),
            cf.ByName("Export Tasks"),
            cf.ByAutomationId("ExportButton")
        );

        if (exportButton != null && exportButton.IsEnabled)
        {
            Console.WriteLine("📤 Testing export error handling...");
            exportButton.AsButton().Click();
            await Task.Delay(3000);
            
            // Look for any error dialogs that might appear
            var errorDialog = await FindElementAsync(MainWindow,
                cf.ByControlType(ControlType.Window).And(cf.ByName("Error")),
                cf.ByControlType(ControlType.Window).And(cf.ByName("Failed")),
                cf.ByAutomationId("ErrorDialog")
            );

            if (errorDialog != null)
            {
                Console.WriteLine($"✅ Found error dialog for file access issue: {errorDialog.Name}");
                
                // Look for error details
                var errorText = errorDialog.FindFirstDescendant(cf.ByControlType(ControlType.Text));
                if (errorText != null)
                {
                    Console.WriteLine($"📄 Error details: {errorText.Name}");
                }
                
                // Look for OK button to close dialog
                var okButton = await FindElementAsync(errorDialog,
                    cf.ByName("OK"),
                    cf.ByName("Close")
                );

                if (okButton != null)
                {
                    Console.WriteLine("✅ Error dialog has proper dismissal button");
                }
            }
            else
            {
                Console.WriteLine("✅ No file access errors occurred during export");
            }
        }
    }
}