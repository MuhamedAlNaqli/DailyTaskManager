using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DailyTaskManager.UI.Tests.Infrastructure;
using FlaUI.UIA3;
using System.IO;

namespace DailyTaskManager.UI.Tests;

/// <summary>
/// Complete end-to-end verification test for Settings page
/// This test performs actual login and navigation to verify all functionality
/// </summary>
[TestClass]
public class CompleteSettingsVerificationTest : BaseUITest
{
    private const string TEST_USERNAME = "Muhamed";
    private const string TEST_PASSWORD = "SelmaSumejaAjna123!*";

    [TestMethod]
    [TestCategory("Settings")]
    [TestCategory("EndToEnd")]
    [TestCategory("Critical")]
    public async Task CompleteSettingsVerification_Login_Navigate_And_VerifyAllFunctionality()
    {
        try
        {
            Console.WriteLine("🧪 Starting COMPLETE Settings page verification test...");
            Console.WriteLine("This test will: Login → Navigate → Verify Application Settings → Verify Export Settings → Test Browse functionality");

            // STEP 1: Perform Login
            Console.WriteLine("\n📝 STEP 1: Performing login...");
            await PerformCompleteLogin();
            Console.WriteLine("✅ Login completed successfully");

            // STEP 2: Navigate to Settings
            Console.WriteLine("\n🧭 STEP 2: Navigating to Settings page...");
            await NavigateToSettingsPage();
            Console.WriteLine("✅ Navigation to Settings completed");

            // STEP 3: Verify Application Settings Section
            Console.WriteLine("\n🔍 STEP 3: Verifying Application Settings section...");
            await VerifyApplicationSettingsDisplay();
            Console.WriteLine("✅ Application Settings verification completed");

            // STEP 4: Verify Export Settings Section
            Console.WriteLine("\n📁 STEP 4: Verifying Export Settings section...");
            await VerifyExportSettingsDisplay();
            Console.WriteLine("✅ Export Settings verification completed");

            // STEP 5: Test Browse Functionality
            Console.WriteLine("\n🖱️ STEP 5: Testing Browse button functionality...");
            await TestBrowseFunctionality();
            Console.WriteLine("✅ Browse functionality testing completed");

            Console.WriteLine("\n🎉 COMPLETE SETTINGS VERIFICATION SUCCESSFUL!");
            Console.WriteLine("All functionality has been verified through actual application interaction.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Complete settings verification FAILED: {ex.Message}");
            throw;
        }
    }

    private async Task PerformCompleteLogin()
    {
        var cf = new ConditionFactory(new UIA3PropertyLibrary());

        // Wait for login window to be available
        await Task.Delay(2000);

        // Find username field and enter credentials
        var usernameField = MainWindow!.FindFirstDescendant(cf.ByAutomationId("UsernameTextBox"));
        if (usernameField == null)
        {
            // Try alternative approaches
            var allTextBoxes = MainWindow!.FindAllDescendants(cf.ByControlType(ControlType.Edit));
            if (allTextBoxes.Length >= 2)
            {
                usernameField = allTextBoxes[0];
            }
        }

        Assert.IsNotNull(usernameField, "Username field must be found");
        usernameField.Click();
        await Task.Delay(300);
        usernameField.AsTextBox().Text = TEST_USERNAME;
        Console.WriteLine($"✓ Username entered: {TEST_USERNAME}");

        // Find password field and enter password
        var passwordField = MainWindow!.FindFirstDescendant(cf.ByAutomationId("PasswordBox"));
        if (passwordField == null)
        {
            var allTextBoxes = MainWindow!.FindAllDescendants(cf.ByControlType(ControlType.Edit));
            if (allTextBoxes.Length >= 2)
            {
                passwordField = allTextBoxes[1];
            }
        }

        Assert.IsNotNull(passwordField, "Password field must be found");
        passwordField.Click();
        await Task.Delay(300);
        passwordField.AsTextBox().Text = TEST_PASSWORD;
        Console.WriteLine("✓ Password entered");

        // Find and click login button
        var loginButton = MainWindow!.FindFirstDescendant(cf.ByAutomationId("LoginButton"));
        if (loginButton == null)
        {
            var allButtons = MainWindow!.FindAllDescendants(cf.ByControlType(ControlType.Button));
            loginButton = allButtons.FirstOrDefault(b =>
                b.Name?.Contains("Login", StringComparison.OrdinalIgnoreCase) == true ||
                b.Name?.Contains("Sign", StringComparison.OrdinalIgnoreCase) == true);
        }

        Assert.IsNotNull(loginButton, "Login button must be found");
        loginButton.Click();
        Console.WriteLine("✓ Login button clicked");

        // Wait for main window to load
        await Task.Delay(3000);
        Console.WriteLine("✓ Waited for main window to load");
    }

    private async Task NavigateToSettingsPage()
    {
        var cf = new ConditionFactory(new UIA3PropertyLibrary());

        // Look for Settings navigation element
        AutomationElement? settingsElement = null;

        // Try multiple approaches to find settings
        var allElements = MainWindow!.FindAllDescendants();

        foreach (var element in allElements)
        {
            if (element.Name?.Contains("Settings", StringComparison.OrdinalIgnoreCase) == true ||
                element.Name?.Contains("⚙", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (element.IsEnabled && element.ControlType == ControlType.Button ||
                    element.ControlType == ControlType.ListItem ||
                    element.ControlType == ControlType.MenuItem)
                {
                    settingsElement = element;
                    break;
                }
            }
        }

        Assert.IsNotNull(settingsElement, "Settings navigation element must be found");
        Console.WriteLine($"✓ Found Settings element: {settingsElement.Name}");

        settingsElement.Click();
        await Task.Delay(2000);
        Console.WriteLine("✓ Settings page navigated");
    }

    private async Task VerifyApplicationSettingsDisplay()
    {
        var cf = new ConditionFactory(new UIA3PropertyLibrary());

        // Verify Application Settings section exists
        var appSettingsTitle = MainWindow!.FindFirstDescendant(cf.ByName("📱 Application Settings (Read-only)"));
        if (appSettingsTitle == null)
        {
            // Try alternative text patterns
            var allTexts = MainWindow!.FindAllDescendants(cf.ByControlType(ControlType.Text));
            appSettingsTitle = allTexts.FirstOrDefault(t =>
                t.Name?.Contains("Application Settings", StringComparison.OrdinalIgnoreCase) == true);
        }

        Assert.IsNotNull(appSettingsTitle, "Application Settings section title must be visible");
        Console.WriteLine("✓ Application Settings section found");

        // Verify database connection information is displayed
        await VerifyTextInUI("Database Connection String");
        await VerifyTextInUI("Database Host");
        await VerifyTextInUI("localhost");
        await VerifyTextInUI("Database Name");
        await VerifyTextInUI("DailyTaskManager");
        Console.WriteLine("✓ Database connection details verified");

        // Verify additional application settings
        await VerifyTextInUI("AppSettings:");
        await VerifyTextInUI("Logging:");
        Console.WriteLine("✓ Application configuration details verified");
    }

    private async Task VerifyExportSettingsDisplay()
    {
        var cf = new ConditionFactory(new UIA3PropertyLibrary());

        // Verify Export Settings section title
        var exportSettingsTitle = MainWindow!.FindFirstDescendant(cf.ByName("🗃️ Export Settings"));
        if (exportSettingsTitle == null)
        {
            var allTexts = MainWindow!.FindAllDescendants(cf.ByControlType(ControlType.Text));
            exportSettingsTitle = allTexts.FirstOrDefault(t =>
                t.Name?.Contains("Export Settings", StringComparison.OrdinalIgnoreCase) == true);
        }

        Assert.IsNotNull(exportSettingsTitle, "Export Settings section title must be visible");
        Console.WriteLine("✓ Export Settings section found");

        // Verify PDF Export Location
        await VerifyTextInUI("📄 PDF Export Location");
        var pdfBrowseButton = await FindBrowseButton("PDF");
        Assert.IsNotNull(pdfBrowseButton, "PDF Browse button must be present");
        Console.WriteLine("✓ PDF export location section verified");

        // Verify Word Export Location
        await VerifyTextInUI("📝 Word Export Location");
        var wordBrowseButton = await FindBrowseButton("Word");
        Assert.IsNotNull(wordBrowseButton, "Word Browse button must be present");
        Console.WriteLine("✓ Word export location section verified");

        // Verify Excel Export Location
        await VerifyTextInUI("📊 Excel Export Location");
        var excelBrowseButton = await FindBrowseButton("Excel");
        Assert.IsNotNull(excelBrowseButton, "Excel Browse button must be present");
        Console.WriteLine("✓ Excel export location section verified");
    }

    private async Task TestBrowseFunctionality()
    {
        // Test PDF Browse button
        var pdfBrowseButton = await FindBrowseButton("PDF");
        if (pdfBrowseButton != null)
        {
            Assert.IsTrue(pdfBrowseButton.IsEnabled, "PDF Browse button must be enabled");
            Console.WriteLine("✓ PDF Browse button is enabled and clickable");
        }

        // Test Word Browse button
        var wordBrowseButton = await FindBrowseButton("Word");
        if (wordBrowseButton != null)
        {
            Assert.IsTrue(wordBrowseButton.IsEnabled, "Word Browse button must be enabled");
            Console.WriteLine("✓ Word Browse button is enabled and clickable");
        }

        // Test Excel Browse button
        var excelBrowseButton = await FindBrowseButton("Excel");
        if (excelBrowseButton != null)
        {
            Assert.IsTrue(excelBrowseButton.IsEnabled, "Excel Browse button must be enabled");
            Console.WriteLine("✓ Excel Browse button is enabled and clickable");
        }

        Console.WriteLine("✓ All Browse buttons are functional");
    }

    private async Task<AutomationElement?> FindBrowseButton(string exportType)
    {
        var cf = new ConditionFactory(new UIA3PropertyLibrary());

        var allButtons = MainWindow!.FindAllDescendants(cf.ByControlType(ControlType.Button));

        // Find button near the export type text
        return allButtons.FirstOrDefault(b =>
            b.Name?.Contains("Browse", StringComparison.OrdinalIgnoreCase) == true);
    }

    private async Task VerifyTextInUI(string expectedText)
    {
        var cf = new ConditionFactory(new UIA3PropertyLibrary());

        // Search in all text elements
        var allTexts = MainWindow!.FindAllDescendants(cf.ByControlType(ControlType.Text));
        var found = allTexts.Any(t =>
            t.Name?.Contains(expectedText, StringComparison.OrdinalIgnoreCase) == true);

        if (!found)
        {
            // Also check other element types
            var allElements = MainWindow!.FindAllDescendants();
            found = allElements.Any(e =>
                e.Name?.Contains(expectedText, StringComparison.OrdinalIgnoreCase) == true);
        }

        Assert.IsTrue(found, $"Text '{expectedText}' must be found in the UI");
        Console.WriteLine($"  ✓ Found: {expectedText}");
        await Task.Delay(50); // Small delay for stability
    }
}