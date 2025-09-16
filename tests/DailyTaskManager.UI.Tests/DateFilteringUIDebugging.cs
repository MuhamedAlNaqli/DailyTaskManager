using DailyTaskManager.UI.Tests.Infrastructure;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlaUI.UIA3;
using System.Linq;

namespace DailyTaskManager.UI.Tests;

/// <summary>
/// Debugging test to examine the UI structure and find DatePicker elements
/// </summary>
[TestClass]
[DoNotParallelize]
public class DateFilteringUIDebugging : BaseUITest
{
    protected override async Task AdditionalSetup()
    {
        await Task.Delay(3000);
        Console.WriteLine($"✅ UI debugging setup completed. Current window: {MainWindow?.Title}");
    }

    [TestMethod]
    public async Task DebugUIStructure_FindDatePickers()
    {
        Console.WriteLine("🧪 Debugging UI structure to locate DatePicker elements...");
        
        try
        {
            // Print the full UI structure
            PrintUIState("Full UI structure for DatePicker debugging");
            
            // Look for all elements that might be related to date filtering
            var allElements = MainWindow!.FindAllDescendants();
            Console.WriteLine($"📊 Total elements found: {allElements.Length}");
            
            var potentialDateElements = new List<(AutomationElement Element, string Reason)>();
            
            foreach (var element in allElements)
            {
                var name = element.Name ?? "";
                var automationId = "";
                var controlType = element.ControlType.ToString();
                
                try
                {
                    automationId = element.AutomationId ?? "";
                }
                catch { }
                
                // Check for various patterns that might indicate date-related elements
                bool isDateRelated = false;
                string reason = "";
                
                if (name.ToLower().Contains("date") || name.ToLower().Contains("from") || name.ToLower().Contains("to"))
                {
                    isDateRelated = true;
                    reason = $"Name contains date/from/to: '{name}'";
                }
                else if (automationId.ToLower().Contains("date"))
                {
                    isDateRelated = true;
                    reason = $"AutomationId contains date: '{automationId}'";
                }
                else if (controlType.Contains("Calendar") || controlType.Contains("DatePicker"))
                {
                    isDateRelated = true;
                    reason = $"ControlType is date-related: {controlType}";
                }
                else if (controlType.Contains("ComboBox") && 
                         (name.ToLower().Contains("select") || automationId.ToLower().Contains("picker")))
                {
                    isDateRelated = true;
                    reason = $"ComboBox with date-like properties: {controlType}, Name: '{name}', Id: '{automationId}'";
                }
                else if (controlType.Contains("Edit") || controlType.Contains("TextBox"))
                {
                    // Check if this textbox is near date-related labels
                    var parentName = "";
                    try
                    {
                        var parent = element.Parent;
                        if (parent != null)
                        {
                            parentName = parent.Name ?? "";
                            if (parentName.ToLower().Contains("date") || parentName.ToLower().Contains("from") || 
                                parentName.ToLower().Contains("to") || parentName.ToLower().Contains("filter"))
                            {
                                isDateRelated = true;
                                reason = $"TextBox with date-related parent: Parent: '{parentName}', Element: '{name}'";
                            }
                        }
                    }
                    catch { }
                }
                
                if (isDateRelated)
                {
                    potentialDateElements.Add((element, reason));
                }
            }
            
            Console.WriteLine($"\n🎯 Found {potentialDateElements.Count} potential date-related elements:");
            
            for (int i = 0; i < potentialDateElements.Count; i++)
            {
                var (element, reason) = potentialDateElements[i];
                var automationId = "";
                try { automationId = element.AutomationId ?? "N/A"; } catch { automationId = "N/A"; }
                
                Console.WriteLine($"  {i + 1}. {element.ControlType}: '{element.Name}' (ID: '{automationId}')");
                Console.WriteLine($"     Reason: {reason}");
                
                // Try to get more details about this element
                try
                {
                    var boundingRect = element.BoundingRectangle;
                    Console.WriteLine($"     Position: X={boundingRect.X}, Y={boundingRect.Y}, W={boundingRect.Width}, H={boundingRect.Height}");
                }
                catch { }
                
                // Check if it's enabled and visible
                try
                {
                    Console.WriteLine($"     Available: {element.IsAvailable}, Enabled: {element.IsEnabled}");
                }
                catch { }
                
                Console.WriteLine();
            }
            
            // Look specifically for elements with watermark text
            Console.WriteLine("\n🔍 Looking for elements with watermark-like text:");
            var watermarkElements = allElements.Where(e => 
            {
                var name = e.Name ?? "";
                return name.ToLower().Contains("select") && 
                       (name.ToLower().Contains("start") || name.ToLower().Contains("end") || 
                        name.ToLower().Contains("date"));
            }).ToArray();
            
            foreach (var element in watermarkElements)
            {
                var automationId = "";
                try { automationId = element.AutomationId ?? "N/A"; } catch { automationId = "N/A"; }
                Console.WriteLine($"  • {element.ControlType}: '{element.Name}' (ID: '{automationId}')");
            }
            
            // Test will pass if we found any potential date elements
            Assert.IsTrue(potentialDateElements.Count > 0 || watermarkElements.Length > 0, 
                "Should find at least some date-related elements for analysis");
                
            await TakeScreenshotAsync("ui_structure_debug", "UI structure debugging completed");
            Console.WriteLine("✅ UI structure debugging completed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during UI structure debugging: {ex.Message}");
            await TakeScreenshotAsync("ui_debug_error", "Error during UI debugging");
            throw;
        }
    }
}