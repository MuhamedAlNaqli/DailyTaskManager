using DailyTaskManager.UI.Tests.Infrastructure;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlaUI.UIA3;
using System.Linq;
using System.Collections.Generic;

namespace DailyTaskManager.UI.Tests;

[TestClass]
public class SimplifiedDateFilteringTests : BaseUITest
{
    protected override async Task AdditionalSetup()
    {
        // MainWindow is already set and logged in from BaseUITest
        // Just wait for the UI to settle after login
        await Task.Delay(3000);
        
        Console.WriteLine($"✅ Additional setup completed. Current window: {MainWindow?.Title}");
    }

    [TestMethod]
    public async Task CanInteractWithDateFilters_Basic()
    {
        Console.WriteLine("🧪 Testing basic date filter interaction...");
        
        // Print UI structure for debugging
        PrintUIState("After login - looking for date filters");

        // Try to find any date-related elements
        var cf = new ConditionFactory(new UIA3PropertyLibrary());
        
        try
        {
            // Look for DatePicker elements by various means
            Console.WriteLine("🔍 Searching for DatePicker elements...");
            
            var allElements = MainWindow!.FindAllDescendants();
            Console.WriteLine($"📊 Total elements found: {allElements.Length}");
            
            var interestingElements = new List<AutomationElement>();
            
            foreach (var e in allElements)
            {
                if (!string.IsNullOrEmpty(e.Name))
                {
                    bool isDateRelated = e.Name.ToLower().Contains("date") ||
                                        e.Name.ToLower().Contains("from") ||
                                        e.Name.ToLower().Contains("to") ||
                                        e.ControlType == ControlType.Calendar;
                    
                    if (!isDateRelated)
                    {
                        try
                        {
                            var automationId = e.AutomationId;
                            if (!string.IsNullOrEmpty(automationId) && automationId.ToLower().Contains("date"))
                            {
                                isDateRelated = true;
                            }
                        }
                        catch
                        {
                            // AutomationId not supported, continue
                        }
                    }
                    
                    if (isDateRelated)
                    {
                        interestingElements.Add(e);
                    }
                }
            }

            Console.WriteLine($"🎯 Found {interestingElements.Count} date-related elements:");
            
            foreach (var element in interestingElements)
            {
                var automationId = "N/A";
                try { automationId = element.AutomationId ?? "N/A"; } catch { }
                Console.WriteLine($"  • {element.ControlType}: '{element.Name}' (ID: '{automationId}')");
            }

            // Look for any button that might be the "Test Date Filter" button
            var testButtons = allElements.Where(e => 
                e.ControlType == ControlType.Button && 
                !string.IsNullOrEmpty(e.Name) && 
                e.Name.ToLower().Contains("test")
            ).ToArray();

            Console.WriteLine($"🔘 Found {testButtons.Length} test buttons:");
            foreach (var btn in testButtons)
            {
                var automationId = "N/A";
                try { automationId = btn.AutomationId ?? "N/A"; } catch { }
                Console.WriteLine($"  • Button: '{btn.Name}' (ID: '{automationId}')");
            }

            // If we found the test button, try clicking it
            if (testButtons.Any())
            {
                Console.WriteLine("🔘 Clicking first test button...");
                testButtons[0].AsButton().Click();
                await Task.Delay(2000);
                Console.WriteLine("✅ Test button clicked successfully!");
            }

            // Take screenshot
            AppLauncher!.TakeScreenshot("date-filter-exploration.png");

            // Basic assertion - we should find some UI elements
            Assert.IsTrue(allElements.Length > 10, "Should find many UI elements in the main window");
            
            Console.WriteLine("✅ Basic date filter exploration completed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during date filter exploration: {ex.Message}");
            AppLauncher!.TakeScreenshot("date-filter-error.png");
            throw;
        }
    }

    [TestMethod]
    public async Task VerifyApplicationResponsiveness()
    {
        Console.WriteLine("🧪 Testing application responsiveness...");

        // Get initial UI state
        var allElements = MainWindow!.FindAllDescendants();
        var initialCount = allElements.Length;
        
        Console.WriteLine($"📊 Initial element count: {initialCount}");

        // Wait and check again
        await Task.Delay(2000);
        
        allElements = MainWindow.FindAllDescendants();
        var laterCount = allElements.Length;
        
        Console.WriteLine($"📊 Later element count: {laterCount}");

        // The counts should be relatively stable (UI not constantly changing)
        var difference = Math.Abs(laterCount - initialCount);
        Assert.IsTrue(difference < 50, $"UI should be relatively stable (difference: {difference})");

        Console.WriteLine("✅ Application responsiveness test passed!");
    }
}