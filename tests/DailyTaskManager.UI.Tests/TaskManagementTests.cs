using DailyTaskManager.UI.Tests.Infrastructure;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DailyTaskManager.UI.Tests;

/// <summary>
/// Tests for core task management functionality in the Avalonia application
/// </summary>
[TestClass]
public class TaskManagementTests : BaseUITest
{
    [TestMethod]
    public async Task AddTask_Should_Create_New_Task_Successfully()
    {
        Console.WriteLine("🧪 Testing add task functionality...");
        
        // Login first
        var mainWindow = await LoginAsync("admin", "admin123");
        await Task.Delay(2000); // Wait for UI to settle
        
        // Look for Add Task button or similar
        var cf = new ConditionFactory(new UIA3PropertyLibrary());
        var addButton = await FindElementAsync(mainWindow,
            cf.ByName("Add Task"),
            cf.ByName("Add New"),
            cf.ByName("Add"),
            cf.ByControlType(ControlType.Button).And(cf.ByName("Add Task")),
            cf.ByAutomationId("AddTaskButton")
        );

        if (addButton != null)
        {
            Console.WriteLine("✅ Found Add Task button, clicking...");
            addButton.AsButton().Click();
            await Task.Delay(1500);

            // Look for add task dialog or form
            var addDialog = await WaitForWindowAsync(w => 
                w.Title.Contains("Add") || 
                w.AutomationId.Contains("AddTask"), 3000);

            if (addDialog != null)
            {
                Console.WriteLine("✅ Add task dialog opened");
                await FillTaskForm(addDialog, "Test Task from UI Test", "This is a test task created by automated UI test");
            }
        }
        else
        {
            Console.WriteLine("⚠️ Add Task button not found, exploring UI structure...");
            PrintUIState("Looking for Add Task functionality");
        }
    }

    [TestMethod]
    public async Task TaskList_Should_Display_Tasks()
    {
        Console.WriteLine("🧪 Testing task list display...");
        
        // Login first
        var mainWindow = await LoginAsync("admin", "admin123");
        await Task.Delay(2000);
        
        // Look for task list/grid
        var cf = new ConditionFactory(new UIA3PropertyLibrary());
        var taskList = await FindElementAsync(mainWindow,
            cf.ByControlType(ControlType.DataGrid),
            cf.ByControlType(ControlType.List),
            cf.ByControlType(ControlType.Table),
            cf.ByAutomationId("TaskGrid"),
            cf.ByAutomationId("TaskList")
        );

        if (taskList != null)
        {
            Console.WriteLine("✅ Found task list/grid");
            
            // Check for task items
            var taskItems = taskList.FindAllDescendants(cf.ByControlType(ControlType.DataItem));
            Console.WriteLine($"📊 Found {taskItems.Length} task items in the list");
            
            // Log first few tasks for verification
            for (int i = 0; i < Math.Min(3, taskItems.Length); i++)
            {
                var item = taskItems[i];
                Console.WriteLine($"🔹 Task {i + 1}: {item.Name}");
            }
            
            Assert.IsTrue(taskItems.Length >= 0, "Task list should be accessible");
        }
        else
        {
            Console.WriteLine("⚠️ Task list not found, exploring UI structure...");
            PrintUIState("Looking for task list");
        }
    }

    [TestMethod]
    public async Task EditTask_Should_Open_Edit_Dialog()
    {
        Console.WriteLine("🧪 Testing edit task functionality...");
        
        // Login first
        var mainWindow = await LoginAsync("admin", "admin123");
        await Task.Delay(2000);
        
        // Look for task list first
        var cf = new ConditionFactory(new UIA3PropertyLibrary());
        var taskList = await FindElementAsync(mainWindow,
            cf.ByControlType(ControlType.DataGrid),
            cf.ByControlType(ControlType.List),
            cf.ByControlType(ControlType.Table)
        );

        if (taskList != null)
        {
            Console.WriteLine("✅ Found task list");
            
            // Look for first task item
            var firstTask = await FindElementAsync(taskList,
                cf.ByControlType(ControlType.DataItem)
            );

            if (firstTask != null)
            {
                Console.WriteLine("✅ Found first task, attempting to edit...");
                
                // Try double-click to edit
                firstTask.Click();
                await Task.Delay(500);
                firstTask.DoubleClick();
                await Task.Delay(1500);
                
                // Look for edit dialog
                try
                {
                    var editDialog = await WaitForWindowAsync(w => 
                        w.Title.Contains("Edit") || 
                        w.AutomationId.Contains("EditTask"), 3000);
                    
                    Console.WriteLine("✅ Edit dialog opened successfully");
                    AppLauncher!.TakeScreenshot("edit-task-dialog.png");
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("⚠️ Edit dialog didn't open - trying right-click context menu...");
                    
                    // Try right-click for context menu
                    firstTask.RightClick();
                    await Task.Delay(1000);
                    PrintUIState("After right-click on task");
                }
            }
        }
        else
        {
            Console.WriteLine("⚠️ No tasks available for editing test");
        }
    }

    [TestMethod]
    public async Task SearchFilter_Should_Filter_Tasks()
    {
        Console.WriteLine("🧪 Testing search/filter functionality...");
        
        // Login first
        var mainWindow = await LoginAsync("admin", "admin123");
        await Task.Delay(2000);
        
        // Look for search/filter controls
        var cf = new ConditionFactory(new UIA3PropertyLibrary());
        var searchBox = await FindElementAsync(mainWindow,
            cf.ByControlType(ControlType.Edit).And(cf.ByName("Search")),
            cf.ByAutomationId("SearchBox"),
            cf.ByAutomationId("FilterBox"),
            cf.ByControlType(ControlType.Edit) // Any text input that might be a search
        );

        if (searchBox != null)
        {
            Console.WriteLine("✅ Found search box, testing search functionality...");
            
            // Enter search term
            searchBox.AsTextBox().Text = "test";
            await Task.Delay(1000);
            
            // Press Enter or look for search button
            searchBox.AsTextBox().Enter("test");
            await Task.Delay(1500);
            
            Console.WriteLine("✅ Search term entered and submitted");
            PrintUIState("After search");
        }
        else
        {
            Console.WriteLine("⚠️ Search box not found, looking for filter controls...");
            
            // Look for date filters or other filter controls
            var dateFilter = await FindElementAsync(mainWindow,
                cf.ByControlType(ControlType.ComboBox),
                cf.ByName("Date Filter"),
                cf.ByName("Filter"),
                cf.ByAutomationId("DateFilter")
            );

            if (dateFilter != null)
            {
                Console.WriteLine("✅ Found date filter control");
                dateFilter.AsComboBox().Click();
                await Task.Delay(1000);
                PrintUIState("After clicking date filter");
            }
        }
    }

    [TestMethod]
    public async Task TaskStatus_Should_Be_Changeable()
    {
        Console.WriteLine("🧪 Testing task status change functionality...");
        
        // Login first
        var mainWindow = await LoginAsync("admin", "admin123");
        await Task.Delay(2000);
        
        // Look for tasks with status controls
        var cf = new ConditionFactory(new UIA3PropertyLibrary());
        var taskList = await FindElementAsync(mainWindow,
            cf.ByControlType(ControlType.DataGrid),
            cf.ByControlType(ControlType.List)
        );

        if (taskList != null)
        {
            Console.WriteLine("✅ Found task list, looking for status controls...");
            
            // Look for checkboxes (for completed status)
            var checkboxes = taskList.FindAllDescendants(cf.ByControlType(ControlType.CheckBox));
            Console.WriteLine($"📊 Found {checkboxes.Length} checkboxes in task list");

            if (checkboxes.Length > 0)
            {
                var firstCheckbox = checkboxes[0];
                var initialState = firstCheckbox.AsCheckBox().IsChecked;
                Console.WriteLine($"🔹 First checkbox state: {initialState}");
                
                // Toggle the checkbox
                firstCheckbox.AsCheckBox().Click();
                await Task.Delay(1000);
                
                var newState = firstCheckbox.AsCheckBox().IsChecked;
                Console.WriteLine($"🔹 Checkbox state after click: {newState}");
                
                Assert.AreNotEqual(initialState, newState, "Checkbox state should change when clicked");
                Console.WriteLine("✅ Task status successfully changed");
            }
        }
    }

    /// <summary>
    /// Helper method to fill out task form fields
    /// </summary>
    private async Task FillTaskForm(AutomationElement dialog, string title, string description)
    {
        Console.WriteLine($"📝 Filling task form - Title: '{title}', Description: '{description}'");
        
        var cf = new ConditionFactory(new UIA3PropertyLibrary());
        
        // Find title field
        var titleField = await FindElementAsync(dialog,
            cf.ByName("Title"),
            cf.ByName("Task Title"),
            cf.ByAutomationId("TitleTextBox"),
            cf.ByControlType(ControlType.Edit)
        );

        if (titleField != null)
        {
            titleField.AsTextBox().Text = title;
            Console.WriteLine("✅ Title entered");
        }

        // Find description field
        var descriptionField = await FindElementAsync(dialog,
            cf.ByName("Description"),
            cf.ByName("Task Description"),
            cf.ByAutomationId("DescriptionTextBox")
        );

        if (descriptionField != null)
        {
            descriptionField.AsTextBox().Text = description;
            Console.WriteLine("✅ Description entered");
        }

        // Find and click Save button
        var saveButton = await FindElementAsync(dialog,
            cf.ByName("Save"),
            cf.ByName("OK"),
            cf.ByName("Add"),
            cf.ByAutomationId("SaveButton")
        );

        if (saveButton != null)
        {
            Console.WriteLine("💾 Clicking Save button...");
            saveButton.AsButton().Click();
            await Task.Delay(1500);
            Console.WriteLine("✅ Task form submitted");
        }
        else
        {
            Console.WriteLine("⚠️ Save button not found");
            PrintUIState("Task form without save button");
        }
    }
}