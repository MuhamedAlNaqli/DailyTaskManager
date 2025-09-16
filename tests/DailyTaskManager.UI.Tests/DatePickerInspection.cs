using DailyTaskManager.UI.Tests.Infrastructure;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlaUI.UIA3;
using System.Linq;

namespace DailyTaskManager.UI.Tests;

/// <summary>
/// Focused inspection to find and understand DatePicker elements in Avalonia
/// </summary>
[TestClass]
[DoNotParallelize]
public class DatePickerInspection : BaseUITest
{
    protected override async Task AdditionalSetup()
    {
        await Task.Delay(3000);
        Console.WriteLine($"✅ DatePicker inspection setup completed. Window: {MainWindow?.Title}");
    }

    [TestMethod]
    public async Task InspectActualDatePickerElements()
    {
        Console.WriteLine("🔍 Inspecting actual DatePicker elements in the UI...");
        
        try
        {
            var allElements = MainWindow!.FindAllDescendants();
            Console.WriteLine($"📊 Total UI elements: {allElements.Length}");
            
            // Look for elements that could be DatePickers based on different criteria
            Console.WriteLine("\n🎯 STRATEGY 1: Looking for 'From' and 'To' labels first...");
            
            var fromLabel = allElements.FirstOrDefault(e => 
                !string.IsNullOrEmpty(e.Name) && 
                e.Name.Equals("From", StringComparison.OrdinalIgnoreCase));
                
            var toLabel = allElements.FirstOrDefault(e => 
                !string.IsNullOrEmpty(e.Name) && 
                e.Name.Equals("To", StringComparison.OrdinalIgnoreCase));
            
            if (fromLabel != null)
            {
                Console.WriteLine($"✅ Found 'From' label: {fromLabel.ControlType} - '{fromLabel.Name}'");
                await InspectNearbyElements(fromLabel, "From label");
            }
            else
            {
                Console.WriteLine("❌ No 'From' label found");
            }
            
            if (toLabel != null)
            {
                Console.WriteLine($"✅ Found 'To' label: {toLabel.ControlType} - '{toLabel.Name}'");
                await InspectNearbyElements(toLabel, "To label");
            }
            else
            {
                Console.WriteLine("❌ No 'To' label found");
            }
            
            // Look for any elements with watermark text we added
            Console.WriteLine("\n🎯 STRATEGY 2: Looking for watermark text...");
            
            var watermarkElements = allElements.Where(e => 
            {
                var name = e.Name ?? "";
                return name.Contains("Select start date") || name.Contains("Select end date");
            }).ToArray();
            
            Console.WriteLine($"Found {watermarkElements.Length} elements with watermark text");
            
            foreach (var element in watermarkElements)
            {
                Console.WriteLine($"  • {element.ControlType}: '{element.Name}'");
                await InspectElementDetails(element, "Watermark element");
            }
            
            // Look for ComboBox or similar controls that might represent DatePickers
            Console.WriteLine("\n🎯 STRATEGY 3: Looking for ComboBox/DatePicker-like controls...");
            
            var comboBoxes = allElements.Where(e => 
                e.ControlType == ControlType.ComboBox ||
                e.ControlType.ToString().Contains("DatePicker") ||
                e.ControlType == ControlType.Custom).ToArray();
                
            Console.WriteLine($"Found {comboBoxes.Length} ComboBox/DatePicker-like controls");
            
            foreach (var combo in comboBoxes.Take(10)) // Limit to first 10 to avoid spam
            {
                Console.WriteLine($"  • {combo.ControlType}: '{combo.Name}' (Available: {combo.IsAvailable})");
                
                try 
                {
                    var automationId = combo.AutomationId ?? "N/A";
                    Console.WriteLine($"    AutomationId: {automationId}");
                } 
                catch { }
                
                await InspectElementDetails(combo, "ComboBox-like element");
            }
            
            // Look for Edit controls (TextBoxes) that might be DatePicker inputs
            Console.WriteLine("\n🎯 STRATEGY 4: Looking for Edit/TextBox controls near date-related elements...");
            
            var editControls = allElements.Where(e => 
                e.ControlType == ControlType.Edit || 
                e.ControlType == ControlType.Document).ToArray();
                
            Console.WriteLine($"Found {editControls.Length} Edit/TextBox controls");
            
            // Look for edit controls with specific names or near date-related parents
            var relevantEdits = editControls.Where(edit => 
            {
                var name = edit.Name ?? "";
                var automationId = "";
                try { automationId = edit.AutomationId ?? ""; } catch { }
                
                // Check if this edit control has date-related identifiers
                return name.ToLower().Contains("date") || 
                       automationId.ToLower().Contains("date") ||
                       name.Contains("Select") ||
                       IsNearDateLabel(edit, allElements);
            }).ToArray();
            
            Console.WriteLine($"Found {relevantEdits.Length} potentially date-related Edit controls");
            
            foreach (var edit in relevantEdits)
            {
                Console.WriteLine($"  • {edit.ControlType}: '{edit.Name}'");
                await InspectElementDetails(edit, "Date-related Edit control");
            }
            
            // Final strategy: Look for elements with DateFromPicker/DateToPicker names
            Console.WriteLine("\n🎯 STRATEGY 5: Looking for elements with DateFromPicker/DateToPicker names...");
            
            var namedDatePickers = allElements.Where(e => 
            {
                var name = e.Name ?? "";
                var automationId = "";
                try { automationId = e.AutomationId ?? ""; } catch { }
                
                return name.Contains("DateFromPicker") || name.Contains("DateToPicker") ||
                       automationId.Contains("DateFromPicker") || automationId.Contains("DateToPicker");
            }).ToArray();
            
            Console.WriteLine($"Found {namedDatePickers.Length} elements with DatePicker names");
            
            foreach (var picker in namedDatePickers)
            {
                Console.WriteLine($"  • {picker.ControlType}: '{picker.Name}'");
                await InspectElementDetails(picker, "Named DatePicker element");
            }
            
            await TakeScreenshotAsync("datepicker_inspection", "DatePicker inspection completed");
            
            // Test passes if we found any potentially relevant elements
            var totalFound = watermarkElements.Length + comboBoxes.Length + relevantEdits.Length + namedDatePickers.Length;
            Console.WriteLine($"\n📊 SUMMARY: Found {totalFound} potentially relevant elements total");
            
            Assert.IsTrue(totalFound > 0, "Should find at least some date-related elements");
            
            Console.WriteLine("✅ DatePicker inspection completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during DatePicker inspection: {ex.Message}");
            await TakeScreenshotAsync("datepicker_inspection_error", "Error during inspection");
            throw;
        }
    }
    
    private async Task InspectNearbyElements(AutomationElement referenceElement, string context)
    {
        try
        {
            Console.WriteLine($"  📍 Inspecting elements near {context}...");
            
            // Get parent and siblings
            var parent = referenceElement.Parent;
            if (parent != null)
            {
                Console.WriteLine($"    Parent: {parent.ControlType} - '{parent.Name}'");
                
                var siblings = parent.FindAllChildren();
                Console.WriteLine($"    Found {siblings.Length} siblings");
                
                foreach (var sibling in siblings)
                {
                    if (sibling.Equals(referenceElement)) continue; // Skip the reference element itself
                    
                    var siblingInfo = $"{sibling.ControlType} - '{sibling.Name}'";
                    
                    // Check if this sibling could be a DatePicker
                    if (CouldBeDatePicker(sibling))
                    {
                        Console.WriteLine($"    🎯 POTENTIAL DATEPICKER: {siblingInfo}");
                        await InspectElementDetails(sibling, "Potential DatePicker sibling");
                    }
                    else
                    {
                        Console.WriteLine($"      Sibling: {siblingInfo}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    ⚠️ Error inspecting nearby elements: {ex.Message}");
        }
    }
    
    private async Task InspectElementDetails(AutomationElement element, string context)
    {
        try
        {
            Console.WriteLine($"    🔍 Detailed inspection of {context}:");
            
            // Basic properties
            Console.WriteLine($"      ControlType: {element.ControlType}");
            Console.WriteLine($"      Name: '{element.Name}'");
            
            try 
            { 
                Console.WriteLine($"      AutomationId: '{element.AutomationId}'");
            } 
            catch { }
            
            try
            {
                Console.WriteLine($"      ClassName: '{element.ClassName}'");
            }
            catch { }
            
            Console.WriteLine($"      IsAvailable: {element.IsAvailable}");
            Console.WriteLine($"      IsEnabled: {element.IsEnabled}");
            
            // Check for patterns that might indicate it's a DatePicker
            try
            {
                if (element.Patterns.Value.IsSupported)
                {
                    var value = element.Patterns.Value.Pattern.Value;
                    Console.WriteLine($"      Value: '{value}'");
                }
            }
            catch { }
            
            // Look at children if it's a container
            try
            {
                var children = element.FindAllChildren();
                if (children.Length > 0)
                {
                    Console.WriteLine($"      Children: {children.Length}");
                    foreach (var child in children.Take(3)) // Limit to first 3 children
                    {
                        Console.WriteLine($"        • {child.ControlType}: '{child.Name}'");
                    }
                    if (children.Length > 3)
                    {
                        Console.WriteLine($"        ... and {children.Length - 3} more");
                    }
                }
            }
            catch { }
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"      ⚠️ Error inspecting element details: {ex.Message}");
        }
    }
    
    private bool CouldBeDatePicker(AutomationElement element)
    {
        try
        {
            var name = element.Name ?? "";
            var controlType = element.ControlType;
            
            // Check various indicators that this might be a DatePicker
            return controlType == ControlType.ComboBox ||
                   controlType == ControlType.Edit ||
                   controlType == ControlType.Custom ||
                   controlType.ToString().Contains("DatePicker") ||
                   name.ToLower().Contains("date") ||
                   name.Contains("Select");
        }
        catch
        {
            return false;
        }
    }
    
    private bool IsNearDateLabel(AutomationElement element, AutomationElement[] allElements)
    {
        try
        {
            var parent = element.Parent;
            if (parent == null) return false;
            
            var parentName = parent.Name ?? "";
            return parentName.ToLower().Contains("from") || 
                   parentName.ToLower().Contains("to") ||
                   parentName.ToLower().Contains("date") ||
                   parentName.ToLower().Contains("filter");
        }
        catch
        {
            return false;
        }
    }
}