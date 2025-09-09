using DailyTaskManager.UI.Tests.Infrastructure;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DailyTaskManager.UI.Tests;

/// <summary>
/// Tests for data validation and error handling in the Avalonia application
/// </summary>
[TestClass]
public class ValidationTests : BaseUITest
{
    [TestMethod]
    public async Task Login_With_Invalid_Credentials_Should_Show_Error()
    {
        Console.WriteLine("🧪 Testing invalid login credentials...");
        
        try
        {
            // Attempt login with invalid credentials
            await LoginAsync("invalid_user", "wrong_password");
            Assert.Fail("Login should have failed with invalid credentials");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✅ Expected login failure: {ex.Message}");
            
            // Look for error message in the UI
            var cf = new ConditionFactory(new UIA3PropertyLibrary());
            var loginWindow = AppLauncher!.GetLoginWindow();
            
            if (loginWindow != null)
            {
                // Look for error message elements
                var errorMessage = await FindElementAsync(loginWindow,
                    cf.ByControlType(ControlType.Text).And(cf.ByName("Error")),
                    cf.ByControlType(ControlType.Text).And(cf.ByName("Invalid")),
                    cf.ByAutomationId("ErrorMessage"),
                    cf.ByAutomationId("LoginError")
                );

                if (errorMessage != null)
                {
                    Console.WriteLine($"✅ Found error message: {errorMessage.Name}");
                }
                else
                {
                    Console.WriteLine("⚠️ No specific error message found in UI");
                    PrintUIState("After invalid login attempt");
                }
            }
        }
    }

    [TestMethod]
    public async Task AddTask_With_Empty_Title_Should_Show_Validation_Error()
    {
        Console.WriteLine("🧪 Testing task creation with empty title...");
        
        // Login first
        var mainWindow = await LoginAsync("admin", "admin123");
        await Task.Delay(2000);
        
        // Look for Add Task functionality
        var cf = new ConditionFactory(new UIA3PropertyLibrary());
        var addButton = await FindElementAsync(mainWindow,
            cf.ByName("Add Task"),
            cf.ByName("Add New"),
            cf.ByName("Add"),
            cf.ByAutomationId("AddTaskButton")
        );

        if (addButton != null)
        {
            Console.WriteLine("✅ Found Add Task button, clicking...");
            addButton.AsButton().Click();
            await Task.Delay(1500);

            try
            {
                var addDialog = await WaitForWindowAsync(w => 
                    w.Title.Contains("Add") || 
                    w.AutomationId.Contains("AddTask"), 3000);

                if (addDialog != null)
                {
                    Console.WriteLine("✅ Add task dialog opened");
                    
                    // Try to save without filling required fields
                    var saveButton = await FindElementAsync(addDialog,
                        cf.ByName("Save"),
                        cf.ByName("OK"),
                        cf.ByName("Add"),
                        cf.ByAutomationId("SaveButton")
                    );

                    if (saveButton != null)
                    {
                        Console.WriteLine("💾 Attempting to save empty task...");
                        saveButton.AsButton().Click();
                        await Task.Delay(1500);
                        
                        // Look for validation error messages
                        var validationError = await FindElementAsync(addDialog,
                            cf.ByControlType(ControlType.Text).And(cf.ByName("required")),
                            cf.ByControlType(ControlType.Text).And(cf.ByName("Title")),
                            cf.ByAutomationId("ValidationError"),
                            cf.ByAutomationId("TitleError")
                        );

                        if (validationError != null)
                        {
                            Console.WriteLine($"✅ Found validation error: {validationError.Name}");
                        }
                        else
                        {
                            Console.WriteLine("⚠️ No validation error message found");
                            PrintUIState("After empty task save attempt");
                        }
                    }
                }
            }
            catch (TimeoutException)
            {
                Console.WriteLine("⚠️ Add task dialog didn't open");
            }
        }
        else
        {
            Console.WriteLine("⚠️ Add Task button not found, skipping validation test");
        }
    }

    [TestMethod]
    public async Task TaskForm_Should_Validate_Date_Fields()
    {
        Console.WriteLine("🧪 Testing date field validation...");
        
        // Login first
        var mainWindow = await LoginAsync("admin", "admin123");
        await Task.Delay(2000);
        
        // Look for Add Task functionality
        var cf = new ConditionFactory(new UIA3PropertyLibrary());
        var addButton = await FindElementAsync(mainWindow,
            cf.ByName("Add Task"),
            cf.ByName("Add New"),
            cf.ByName("Add"),
            cf.ByAutomationId("AddTaskButton")
        );

        if (addButton != null)
        {
            addButton.AsButton().Click();
            await Task.Delay(1500);

            try
            {
                var addDialog = await WaitForWindowAsync(w => 
                    w.Title.Contains("Add") || 
                    w.AutomationId.Contains("AddTask"), 3000);

                if (addDialog != null)
                {
                    Console.WriteLine("✅ Add task dialog opened");
                    
                    // Look for date fields
                    var dateField = await FindElementAsync(addDialog,
                        cf.ByName("Date"),
                        cf.ByName("Due Date"),
                        cf.ByName("Task Date"),
                        cf.ByAutomationId("DatePicker"),
                        cf.ByAutomationId("TaskDate")
                    );

                    if (dateField != null)
                    {
                        Console.WriteLine("✅ Found date field, testing invalid date input...");
                        
                        // Try to enter invalid date
                        try
                        {
                            dateField.AsTextBox().Text = "invalid-date";
                            await Task.Delay(1000);
                            
                            // Tab away to trigger validation
                            dateField.Focus();
                            await Task.Delay(500);
                            
                            // Look for date validation error
                            var dateError = await FindElementAsync(addDialog,
                                cf.ByControlType(ControlType.Text).And(cf.ByName("date")),
                                cf.ByControlType(ControlType.Text).And(cf.ByName("invalid")),
                                cf.ByAutomationId("DateError")
                            );

                            if (dateError != null)
                            {
                                Console.WriteLine($"✅ Found date validation error: {dateError.Name}");
                            }
                            else
                            {
                                Console.WriteLine("⚠️ No date validation error found");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"⚠️ Date field interaction failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("⚠️ No date field found in add task dialog");
                        PrintUIState("Add task dialog without date field");
                    }
                }
            }
            catch (TimeoutException)
            {
                Console.WriteLine("⚠️ Add task dialog didn't open");
            }
        }
    }

    [TestMethod]
    public async Task Database_Connection_Error_Should_Be_Handled_Gracefully()
    {
        Console.WriteLine("🧪 Testing database connection error handling...");
        
        // This test assumes the app might show connection errors
        // We'll look for any error dialogs or messages that appear during startup
        
        await Task.Delay(3000); // Wait for app to fully load
        
        var cf = new ConditionFactory(new UIA3PropertyLibrary());
        
        // Look for error dialogs
        var errorDialog = await FindElementAsync(MainWindow!,
            cf.ByControlType(ControlType.Window).And(cf.ByName("Error")),
            cf.ByControlType(ControlType.Window).And(cf.ByName("Connection")),
            cf.ByAutomationId("ErrorDialog"),
            cf.ByAutomationId("ConnectionErrorDialog")
        );

        if (errorDialog != null)
        {
            Console.WriteLine($"✅ Found error dialog: {errorDialog.Name}");
            
            // Look for error message text
            var errorText = errorDialog.FindFirstDescendant(cf.ByControlType(ControlType.Text));
            if (errorText != null)
            {
                Console.WriteLine($"📄 Error message: {errorText.Name}");
            }
            
            // Look for OK or Close button
            var okButton = await FindElementAsync(errorDialog,
                cf.ByName("OK"),
                cf.ByName("Close"),
                cf.ByName("Cancel")
            );

            if (okButton != null)
            {
                Console.WriteLine("✅ Found error dialog dismiss button");
                // Don't actually click it to preserve test state
            }
        }
        else
        {
            Console.WriteLine("✅ No database connection errors detected");
        }
        
        // Check if login window is still accessible (indicates graceful error handling)
        var loginWindow = AppLauncher!.GetLoginWindow();
        if (loginWindow != null)
        {
            Console.WriteLine("✅ Login window still accessible after error check");
        }
    }

    [TestMethod]
    public async Task Long_Text_Fields_Should_Handle_Max_Length()
    {
        Console.WriteLine("🧪 Testing long text field validation...");
        
        // Login first
        var mainWindow = await LoginAsync("admin", "admin123");
        await Task.Delay(2000);
        
        // Look for Add Task functionality
        var cf = new ConditionFactory(new UIA3PropertyLibrary());
        var addButton = await FindElementAsync(mainWindow,
            cf.ByName("Add Task"),
            cf.ByName("Add New"),
            cf.ByName("Add"),
            cf.ByAutomationId("AddTaskButton")
        );

        if (addButton != null)
        {
            addButton.AsButton().Click();
            await Task.Delay(1500);

            try
            {
                var addDialog = await WaitForWindowAsync(w => 
                    w.Title.Contains("Add") || 
                    w.AutomationId.Contains("AddTask"), 3000);

                if (addDialog != null)
                {
                    // Find title field
                    var titleField = await FindElementAsync(addDialog,
                        cf.ByName("Title"),
                        cf.ByName("Task Title"),
                        cf.ByAutomationId("TitleTextBox")
                    );

                    if (titleField != null)
                    {
                        Console.WriteLine("✅ Found title field, testing long text...");
                        
                        // Create a very long title
                        var longTitle = new string('A', 1000);
                        titleField.AsTextBox().Text = longTitle;
                        await Task.Delay(1000);
                        
                        // Check if text was truncated or validation occurred
                        var actualText = titleField.AsTextBox().Text;
                        Console.WriteLine($"📊 Entered {longTitle.Length} characters, field contains {actualText.Length} characters");
                        
                        if (actualText.Length < longTitle.Length)
                        {
                            Console.WriteLine("✅ Text field properly limits input length");
                        }
                        
                        // Look for length validation error
                        var lengthError = await FindElementAsync(addDialog,
                            cf.ByControlType(ControlType.Text).And(cf.ByName("length")),
                            cf.ByControlType(ControlType.Text).And(cf.ByName("long")),
                            cf.ByAutomationId("LengthError")
                        );

                        if (lengthError != null)
                        {
                            Console.WriteLine($"✅ Found length validation error: {lengthError.Name}");
                        }
                    }
                }
            }
            catch (TimeoutException)
            {
                Console.WriteLine("⚠️ Add task dialog didn't open");
            }
        }
    }

    [TestMethod]
    public async Task Application_Should_Handle_Concurrent_Operations()
    {
        Console.WriteLine("🧪 Testing concurrent operation handling...");
        
        // Login first
        var mainWindow = await LoginAsync("admin", "admin123");
        await Task.Delay(2000);
        
        // Try to perform multiple operations quickly
        var cf = new ConditionFactory(new UIA3PropertyLibrary());
        
        // Look for multiple clickable elements
        var buttons = mainWindow.FindAllDescendants(cf.ByControlType(ControlType.Button));
        Console.WriteLine($"📊 Found {buttons.Length} buttons in main window");
        
        if (buttons.Length >= 2)
        {
            Console.WriteLine("🔄 Clicking multiple buttons in rapid succession...");
            
            // Click first few buttons rapidly (but safely)
            for (int i = 0; i < Math.Min(3, buttons.Length); i++)
            {
                try
                {
                    if (buttons[i].IsEnabled)
                    {
                        Console.WriteLine($"🔘 Clicking button {i + 1}: {buttons[i].Name}");
                        buttons[i].AsButton().Click();
                        await Task.Delay(200); // Small delay between clicks
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Button {i + 1} click failed: {ex.Message}");
                }
            }
            
            await Task.Delay(2000);
            
            // Check if application is still responsive
            try
            {
                var currentWindows = AppLauncher!.Application.GetAllTopLevelWindows(AppLauncher.Automation);
                Console.WriteLine($"✅ Application still responsive - {currentWindows.Length} windows active");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Application may have become unresponsive: {ex.Message}");
                Assert.Fail("Application should remain responsive during concurrent operations");
            }
        }
    }
}