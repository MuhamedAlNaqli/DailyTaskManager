using DailyTaskManager.UI.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace DailyTaskManager.UI.Tests;

[TestClass]
public class SmokeTests : BaseUITest
{
    [TestMethod]
    public async Task Application_Should_Launch_Successfully()
    {
        Console.WriteLine("🧪 Testing application launch...");
        
        // The application should already be launched by BaseUITest setup
        Assert.IsNotNull(MainWindow, "Main window should be available");
        Assert.IsNotNull(AppLauncher, "AppLauncher should be available");
        
        Console.WriteLine($"✅ Application launched successfully. Window title: {MainWindow.Title}");
        
        // Print UI structure for debugging
        PrintUIState("Application Launch");
        
        // Take a screenshot
        AppLauncher.TakeScreenshot("smoke-test-launch.png");
        
        Console.WriteLine("✅ Smoke test completed!");
    }

    [TestMethod]
    public async Task Login_Should_Work_Successfully()
    {
        Console.WriteLine("🧪 Testing login functionality...");
        
        // BaseUITest already performed login during TestInitialize
        // Just verify the login was successful by checking MainWindow
        Assert.IsNotNull(MainWindow, "Main window should be available after login");
        Assert.IsTrue(MainWindow.Title.Contains("Daily Task Manager"), 
            "Main window should have correct title after login");
        
        Console.WriteLine($"✅ Login test completed successfully! Main window: {MainWindow.Title}");
    }

    [TestMethod]
    public async Task UI_Elements_Should_Be_Present_After_Login()
    {
        Console.WriteLine("🧪 Testing UI elements presence after login...");
        
        // BaseUITest already performed login during TestInitialize
        // Just wait for UI to settle
        await Task.Delay(2000);
        
        // Print full UI tree for analysis
        PrintUIState("After Login - Full UI Tree");
        
        // This test is mainly for discovering the UI structure
        // We'll look for common elements that should exist
        var elementsFound = 0;
        
        try
        {
            // Look for various UI elements (we'll discover what's available)
            var allElements = MainWindow!.FindAllDescendants();
            Console.WriteLine($"📊 Total UI elements found: {allElements.Length}");
            
            foreach (var element in allElements.Take(50)) // Limit output
            {
                var automationId = "N/A";
                try { automationId = element.AutomationId ?? "N/A"; } catch { }
                
                if (!string.IsNullOrEmpty(element.Name) || (automationId != "N/A" && !string.IsNullOrEmpty(automationId)))
                {
                    Console.WriteLine($"🔹 {element.ControlType}: '{element.Name}' (ID: '{automationId}')");
                    elementsFound++;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enumerating elements: {ex.Message}");
        }
        
        Assert.IsTrue(elementsFound > 0, "Should find at least some named UI elements");
        Console.WriteLine($"✅ Found {elementsFound} named UI elements");
    }
}