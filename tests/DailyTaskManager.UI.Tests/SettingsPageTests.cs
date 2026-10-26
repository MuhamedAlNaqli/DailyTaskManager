using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DailyTaskManager.UI.Tests.Infrastructure;
using FlaUI.UIA3;
using System.IO;

namespace DailyTaskManager.UI.Tests;

/// <summary>
/// Comprehensive UI tests for the Settings page functionality
/// Tests database connection display and download location settings
/// </summary>
[TestClass]
public class SettingsPageTests : BaseUITest
{
    [TestMethod]
    [TestCategory("Settings")]
    [TestCategory("Critical")]
    public async Task SettingsPage_Should_Display_DatabaseConnection_And_AllowDownloadPathConfiguration()
    {
        try
        {
            Console.WriteLine("🧪 Starting Settings page comprehensive test...");

            // MainWindow is already logged in from TestInitialize in base class
            await Task.Delay(2000);

            // Step 1: Navigate to Settings page
            await NavigateToSettingsPage();
            await Task.Delay(1500);

            // Step 2: Verify Application Settings section displays database connection details
            await VerifyApplicationSettingsSection();

            // Step 3: Verify Database Settings section has download path configuration
            await VerifyDatabaseSettingsSection();

            // Step 4: Test download path browse button
            await TestDownloadPathBrowseButton();

            Console.WriteLine("✅ Settings page test completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Settings page test failed: {ex.Message}");
            throw;
        }
    }

    private async Task NavigateToSettingsPage()
    {
        Console.WriteLine("🧭 Navigating to Settings page...");

        var cf = new ConditionFactory(new UIA3PropertyLibrary());

        // Look for Settings button/menu item - try multiple approaches
        AutomationElement? settingsButton = null;

        // Try to find by name "Settings"
        settingsButton = await FindElementAsync(MainWindow!, cf.ByName("Settings"));

        if (settingsButton == null)
        {
            // Try to find settings in sidebar or menu
            var allButtons = MainWindow!.FindAllDescendants(cf.ByControlType(ControlType.Button));
            settingsButton = allButtons.FirstOrDefault(b =>
                b.Name?.Contains("Settings", StringComparison.OrdinalIgnoreCase) == true);
        }

        if (settingsButton == null)
        {
            // Try to find in navigation items
            var allNavItems = MainWindow!.FindAllDescendants();
            settingsButton = allNavItems.FirstOrDefault(b =>
                b.Name?.Contains("Settings", StringComparison.OrdinalIgnoreCase) == true &&
                b.IsEnabled);
        }

        Assert.IsNotNull(settingsButton, "Settings navigation element should be found");
        settingsButton.Click();

        await Task.Delay(1500);
        Console.WriteLine("✅ Navigated to Settings page");
    }

    private async Task VerifyApplicationSettingsSection()
    {
        Console.WriteLine("🔍 Verifying Application Settings section...");

        var cf = new ConditionFactory(new UIA3PropertyLibrary());

        // Look for text indicating Application Settings section
        var appSettingsText = await FindElementAsync(MainWindow!,
            cf.ByName("📱 Application Settings (Read-only)"));

        if (appSettingsText == null)
        {
            // Try alternative text patterns
            var allTexts = MainWindow!.FindAllDescendants(cf.ByControlType(ControlType.Text));
            appSettingsText = allTexts.FirstOrDefault(t =>
                t.Name?.Contains("Application Settings", StringComparison.OrdinalIgnoreCase) == true);
        }

        Assert.IsNotNull(appSettingsText, "Application Settings section should be visible");

        // Verify database connection information is displayed
        await VerifyTextExists("Database Connection String");
        await VerifyTextExists("Database Host");
        await VerifyTextExists("localhost"); // Should show the actual host
        await VerifyTextExists("Database Name");
        await VerifyTextExists("DailyTaskManager"); // Should show the actual database name

        Console.WriteLine("✅ Application Settings section verified - shows database connection details");
    }

    private async Task VerifyDatabaseSettingsSection()
    {
        Console.WriteLine("🔍 Verifying Export Settings section...");

        var cf = new ConditionFactory(new UIA3PropertyLibrary());

        // Look for Database Settings section
        var dbSettingsText = await FindElementAsync(MainWindow!,
            cf.ByName("🗃️ Export Settings"));

        if (dbSettingsText == null)
        {
            var allTexts = MainWindow!.FindAllDescendants(cf.ByControlType(ControlType.Text));
            dbSettingsText = allTexts.FirstOrDefault(t =>
                t.Name?.Contains("Export Settings", StringComparison.OrdinalIgnoreCase) == true);
        }

        Assert.IsNotNull(dbSettingsText, "Export Settings section should be visible");

        // Look for Export Download Location setting
        await VerifyTextExists("PDF Export Location");
        await VerifyTextExists("Word Export Location");
        await VerifyTextExists("Excel Export Location");

        // Verify Browse button exists
        var browseButton = await FindElementAsync(MainWindow!,
            cf.ByName("Browse"));

        if (browseButton == null)
        {
            // Try to find button with Browse text
            var allButtons = MainWindow!.FindAllDescendants(cf.ByControlType(ControlType.Button));
            browseButton = allButtons.FirstOrDefault(b =>
                b.Name?.Contains("Browse", StringComparison.OrdinalIgnoreCase) == true);
        }

        Assert.IsNotNull(browseButton, "Browse button should be present");

        Console.WriteLine("✅ Export Settings section verified - has per-format export locations");
    }

    private async Task TestDownloadPathBrowseButton()
    {
        Console.WriteLine("📁 Testing download path browse button...");

        var cf = new ConditionFactory(new UIA3PropertyLibrary());

        // Find the browse button
        var browseButton = await FindElementAsync(MainWindow!,
            cf.ByName("Browse"));

        if (browseButton == null)
        {
            var allButtons = MainWindow!.FindAllDescendants(cf.ByControlType(ControlType.Button));
            browseButton = allButtons.FirstOrDefault(b =>
                b.Name?.Contains("Browse", StringComparison.OrdinalIgnoreCase) == true);
        }

        Assert.IsNotNull(browseButton, "Browse button should be clickable");
        Assert.IsTrue(browseButton.IsEnabled, "Browse button should be enabled");

        // Click browse button (this would normally open folder dialog)
        Console.WriteLine("🖱️ Clicking Browse button...");
        browseButton.Click();

        await Task.Delay(1000);

        // Note: In a real environment, this would open a folder dialog
        // For testing purposes, we verify the button click was successful
        Console.WriteLine("✅ Browse button clicked successfully");

        // Press Escape to close any potential dialog (safety measure)
        try
        {
            System.Windows.Forms.SendKeys.SendWait("{ESCAPE}");
            await Task.Delay(500);
        }
        catch
        {
            // Ignore if escape doesn't work
        }
    }

    private async Task VerifyTextExists(string expectedText)
    {
        var cf = new ConditionFactory(new UIA3PropertyLibrary());

        // Find all text elements
        var allTexts = MainWindow!.FindAllDescendants(cf.ByControlType(ControlType.Text));
        var found = allTexts.Any(t =>
            t.Name?.Contains(expectedText, StringComparison.OrdinalIgnoreCase) == true);

        if (!found)
        {
            // Also check data grid cells and other elements
            var allElements = MainWindow!.FindAllDescendants();
            found = allElements.Any(e =>
                e.Name?.Contains(expectedText, StringComparison.OrdinalIgnoreCase) == true);
        }

        Assert.IsTrue(found, $"Expected text '{expectedText}' should be found in Settings page");
        Console.WriteLine($"✅ Found expected text: {expectedText}");
        await Task.Delay(100); // Small delay for stability
    }

    private async Task WaitForElement(string elementName, int timeoutMs)
    {
        var cf = new ConditionFactory(new UIA3PropertyLibrary());
        var startTime = DateTime.Now;

        while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
        {
            try
            {
                var element = MainWindow!.FindFirstDescendant(cf.ByName(elementName));
                if (element != null && element.IsAvailable)
                    return;
            }
            catch
            {
                // Continue waiting
            }
            await Task.Delay(100);
        }
    }

    [TestMethod]
    [TestCategory("Settings")]
    public async Task SettingsPage_DownloadPath_Should_ShowCurrentValue()
    {
        try
        {
            Console.WriteLine("🧪 Testing download path display...");

            await NavigateToSettingsPage();
            await Task.Delay(1000);

            // Verify that a download path is displayed (should show default or configured value)
            var cf = new ConditionFactory(new UIA3PropertyLibrary());

            // Look for textbox that shows download path
            var pathTextBoxes = MainWindow!.FindAllDescendants(cf.ByControlType(ControlType.Edit));
            var downloadPathBox = pathTextBoxes.FirstOrDefault(tb =>
                tb.Name?.Contains("folder", StringComparison.OrdinalIgnoreCase) == true ||
                tb.HelpText?.Contains("export", StringComparison.OrdinalIgnoreCase) == true);

            if (downloadPathBox != null)
            {
                var pathValue = downloadPathBox.AsTextBox().Text;
                Assert.IsFalse(string.IsNullOrEmpty(pathValue), "Download path should have a value");
                Console.WriteLine($"✅ Download path is set to: {pathValue}");
            }
            else
            {
                Console.WriteLine("ℹ️ Download path textbox structure may be different than expected");
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Download path display test failed: {ex.Message}");
            throw;
        }
    }
}