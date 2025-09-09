using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using System.Windows.Forms;
using System.Linq;

namespace DailyTaskManager.UI.Tests.Infrastructure;

/// <summary>
/// Base class for all UI tests providing common functionality
/// </summary>
[TestClass]
public abstract class BaseUITest
{
    protected AppLauncher? AppLauncher { get; private set; }
    protected Window? MainWindow { get; set; }

    [TestInitialize]
    public async Task TestInitialize()
    {
        Console.WriteLine("🚀 Setting up UI test...");
        
        // Kill any existing instances with enhanced cleanup
        await AppLauncher.KillAllInstancesAsync();
        await Task.Delay(1000);

        // Launch application
        AppLauncher = new AppLauncher();
        MainWindow = await AppLauncher.LaunchAsync();
        
        Console.WriteLine($"✅ Application launched. Main window: {MainWindow.Title}");
        
        // Check if we landed on login window and auto-login
        if (MainWindow.Title.Contains("Login", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("🔐 Login window detected, performing automatic login...");
            await LoginAsync("admin", "admin123");
            
            // Wait longer for the window transition and add more detailed logging
            Console.WriteLine("⏳ Waiting for main application window to appear...");
            
            // First, wait a bit for the login to process
            await Task.Delay(2000);
            
            // Update MainWindow reference to the main app window - be more flexible
            MainWindow = await WaitForWindowAsync(w => 
            {
                Console.WriteLine($"🔍 Checking window: '{w.Title}'");
                return w.Title == "Daily Task Manager" || // Exact match
                       (w.Title.Contains("Daily Task Manager") && !w.Title.Contains("Login")); // Contains but not login
            }, 15000); // Increased timeout to 15 seconds
            
            Console.WriteLine($"✅ Login completed. Main window: {MainWindow.Title}");
        }
        
        // Take initial screenshot
        AppLauncher.TakeScreenshot($"test-start-{TestContext?.TestName}.png");
        
        // Perform any additional setup
        await AdditionalSetup();
    }

    [TestCleanup]
    public virtual void TestCleanup()
    {
        Console.WriteLine("🧹 Cleaning up UI test...");
        
        try
        {
            // Take final screenshot
            AppLauncher?.TakeScreenshot($"test-end-{TestContext?.TestName}.png");
            
            // Perform additional cleanup
            AdditionalCleanup();
        }
        finally
        {
            // Always dispose resources
            AppLauncher?.Dispose();
            AppLauncher = null;
            MainWindow = null;
            
            // Additional cleanup for any leftover processes
            try
            {
                Task.Run(async () => await AppLauncher.KillAllInstancesAsync()).Wait(5000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Warning during final process cleanup: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Override this method for additional test setup
    /// </summary>
    protected virtual async Task AdditionalSetup()
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Override this method for additional test cleanup
    /// </summary>
    protected virtual void AdditionalCleanup()
    {
        // Override in derived classes
    }

    public TestContext? TestContext { get; set; }

    #region Helper Methods

    /// <summary>
    /// Performs login with the specified credentials
    /// </summary>
    protected async Task<Window> LoginAsync(string username = "admin", string password = "admin123")
    {
        Console.WriteLine($"🔐 Attempting login with username: {username}");
        
        var loginWindow = AppLauncher!.GetLoginWindow();
        if (loginWindow == null)
        {
            throw new InvalidOperationException("Login window not found");
        }

        Console.WriteLine("📋 Looking for login controls...");
        AppLauncher.PrintUITree(loginWindow);

        // Find username and password input fields (Edit controls, not text labels)
        var cf = new ConditionFactory(new UIA3PropertyLibrary());
        var allEditControls = loginWindow.FindAllDescendants(cf.ByControlType(ControlType.Edit));
        Console.WriteLine($"🔍 Found {allEditControls.Length} Edit controls");
        
        AutomationElement? usernameBox = null;
        AutomationElement? passwordBox = null;
        
        // The first Edit control should be username, second should be password
        if (allEditControls.Length >= 2)
        {
            usernameBox = allEditControls[0]; // First edit control = username
            passwordBox = allEditControls[1]; // Second edit control = password
            Console.WriteLine($"✅ Using first Edit control as username field");
            Console.WriteLine($"✅ Using second Edit control as password field");
        }
        else
        {
            // Fallback: try to find them by position or other means
            foreach (var editControl in allEditControls)
            {
                Console.WriteLine($"🔹 Edit control: Name='{editControl.Name}', AutomationId='{editControl.AutomationId}'");
            }
            
            throw new InvalidOperationException($"Expected 2 Edit controls but found {allEditControls.Length}");
        }

        if (usernameBox == null)
        {
            throw new InvalidOperationException("Username textbox not found");
        }

        if (passwordBox == null)
        {
            throw new InvalidOperationException("Password textbox not found");
        }

        // Enter credentials using multiple approaches for maximum reliability
        Console.WriteLine($"📝 Entering username: {username}");
        
        // Focus and enter username
        usernameBox.Click();
        await Task.Delay(500);
        
        // Try Value pattern first (most reliable for automation)
        bool usernameSet = false;
        try
        {
            if (usernameBox.Patterns.Value.IsSupported)
            {
                var valuePattern = usernameBox.Patterns.Value.Pattern;
                valuePattern.SetValue(username);
                usernameSet = true;
                Console.WriteLine($"✅ Username set via Value pattern: {username}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Value pattern failed: {ex.Message}");
        }
        
        // Fallback to text assignment if Value pattern not available
        if (!usernameSet)
        {
            try
            {
                var usernameTextBox = usernameBox.AsTextBox();
                usernameTextBox.Text = username;
                Console.WriteLine($"✅ Username set via direct assignment: {username}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Direct assignment failed: {ex.Message}");
            }
        }
        
        await Task.Delay(300);
        
        Console.WriteLine($"📝 Entering password: {password}");
        
        // Focus and enter password
        passwordBox.Click();
        await Task.Delay(500);
        
        // Try Value pattern first
        bool passwordSet = false;
        try
        {
            if (passwordBox.Patterns.Value.IsSupported)
            {
                var valuePattern = passwordBox.Patterns.Value.Pattern;
                valuePattern.SetValue(password);
                passwordSet = true;
                Console.WriteLine($"✅ Password set via Value pattern: {password}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Value pattern failed: {ex.Message}");
        }
        
        // Fallback to text assignment
        if (!passwordSet)
        {
            try
            {
                var passwordTextBox = passwordBox.AsTextBox();
                passwordTextBox.Text = password;
                Console.WriteLine($"✅ Password set via direct assignment: {password}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Direct assignment failed: {ex.Message}");
            }
        }
        
        await Task.Delay(300);
        
        // Verify that the text actually got into the fields
        try
        {
            var usernameTextBox = usernameBox.AsTextBox();
            var passwordTextBox = passwordBox.AsTextBox();
            
            var usernameValue = usernameTextBox.Text ?? "";
            var passwordValue = passwordTextBox.Text ?? "";
            
            Console.WriteLine($"🔍 Verification - Username field contains: '{usernameValue}'");
            Console.WriteLine($"🔍 Verification - Password field contains: '{passwordValue}' (length: {passwordValue.Length})");
            
            if (usernameValue != username)
            {
                Console.WriteLine($"⚠️ Username mismatch! Expected: '{username}', Got: '{usernameValue}'");
            }
            
            if (passwordValue != password)
            {
                Console.WriteLine($"⚠️ Password mismatch! Expected: '{password}', Got: '{passwordValue}'");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error verifying field contents: {ex.Message}");
        }
        
        Console.WriteLine($"✅ Credentials entered successfully");

        // Find and click login button - handle Avalonia button structure
        var loginButton = await FindElementAsync(loginWindow,
            cf.ByControlType(ControlType.Button).And(cf.ByName("Sign In")),
            cf.ByName("Sign In"),
            cf.ByName("Login"),
            cf.ByAutomationId("LoginButton"),
            cf.ByControlType(ControlType.Button) // Fallback - any button if text search fails
        );

        // Alternative: Find button by looking for one that contains "Sign In" text
        if (loginButton == null)
        {
            var allButtons = loginWindow.FindAllDescendants(cf.ByControlType(ControlType.Button));
            Console.WriteLine($"🔍 Found {allButtons.Length} buttons, checking each for Sign In text...");
            
            foreach (var button in allButtons)
            {
                try
                {
                    var buttonText = button.FindFirstDescendant(cf.ByControlType(ControlType.Text));
                    if (buttonText != null && buttonText.Name?.Contains("Sign In") == true)
                    {
                        Console.WriteLine($"✅ Found Sign In button with text element: '{buttonText.Name}'");
                        loginButton = button;
                        break;
                    }
                    
                    // Also check if the button itself or its name contains Sign In
                    if (button.Name?.Contains("Sign In") == true)
                    {
                        Console.WriteLine($"✅ Found Sign In button by name: '{button.Name}'");
                        loginButton = button;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error checking button: {ex.Message}");
                }
            }
        }

        if (loginButton == null)
        {
            throw new InvalidOperationException("Login button not found");
        }

        Console.WriteLine($"🔘 Clicking login button: '{loginButton.Name}'");
        
        // Try multiple approaches to activate the button
        bool buttonActivated = false;
        
        try
        {
            // First, focus the button
            loginButton.Focus();
            await Task.Delay(500);
            Console.WriteLine("🔘 Button focused successfully");
            
            // Method 1: Try invoke pattern first (most reliable for complex Avalonia buttons)
            var invokePattern = loginButton.Patterns.Invoke.PatternOrDefault;
            if (invokePattern != null)
            {
                Console.WriteLine("🔘 Using invoke pattern on login button...");
                invokePattern.Invoke();
                buttonActivated = true;
                Console.WriteLine("✅ Invoke pattern executed successfully");
                await Task.Delay(2000);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Invoke pattern failed: {ex.Message}");
        }
        
        // Method 2: If invoke pattern didn't work, try keyboard activation
        if (!buttonActivated)
        {
            try
            {
                Console.WriteLine("🔘 Trying keyboard activation (Enter key)...");
                loginButton.Focus();
                await Task.Delay(500);
                
                // Send Enter key to activate the focused button
                System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                buttonActivated = true;
                Console.WriteLine("✅ Keyboard activation (Enter) executed");
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Keyboard activation failed: {ex.Message}");
            }
        }
        
        // Method 3: Try space key activation as another fallback
        if (!buttonActivated)
        {
            try
            {
                Console.WriteLine("🔘 Trying keyboard activation (Space key)...");
                loginButton.Focus();
                await Task.Delay(500);
                
                // Send Space key to activate the button
                System.Windows.Forms.SendKeys.SendWait(" ");
                buttonActivated = true;
                Console.WriteLine("✅ Keyboard activation (Space) executed");
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Space key activation failed: {ex.Message}");
            }
        }
        
        if (!buttonActivated)
        {
            Console.WriteLine("❌ All button activation methods failed");
            throw new InvalidOperationException("Unable to activate login button - all methods failed");
        }

        Console.WriteLine("✅ Button activation completed, waiting for login to process...");
        
        // Check for any error messages or loading indicators after button activation
        await Task.Delay(2000); // Give time for login processing
        
        // Look for error messages that might appear
        try
        {
            var errorMessages = loginWindow.FindAllDescendants(cf.ByControlType(ControlType.Text))
                .Where(t => !string.IsNullOrEmpty(t.Name) && 
                           (t.Name.ToLower().Contains("error") ||
                            t.Name.ToLower().Contains("invalid") ||
                            t.Name.ToLower().Contains("failed") ||
                            t.Name.ToLower().Contains("incorrect")))
                .ToArray();
                
            if (errorMessages.Length > 0)
            {
                Console.WriteLine($"⚠️ Found {errorMessages.Length} potential error messages:");
                foreach (var msg in errorMessages)
                {
                    Console.WriteLine($"  📋 Error: '{msg.Name}'");
                }
            }
            else
            {
                Console.WriteLine("✅ No error messages found after login attempt");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error checking for error messages: {ex.Message}");
        }
        
        // Check if any loading indicators are present
        try
        {
            var loadingIndicators = loginWindow.FindAllDescendants(cf.ByControlType(ControlType.Text))
                .Where(t => !string.IsNullOrEmpty(t.Name) && 
                           (t.Name.ToLower().Contains("loading") ||
                            t.Name.ToLower().Contains("wait") ||
                            t.Name.ToLower().Contains("processing")))
                .ToArray();
                
            if (loadingIndicators.Length > 0)
            {
                Console.WriteLine($"⏳ Found {loadingIndicators.Length} loading indicators:");
                foreach (var indicator in loadingIndicators)
                {
                    Console.WriteLine($"  📋 Loading: '{indicator.Name}'");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error checking for loading indicators: {ex.Message}");
        }

        // Wait for main window to appear (login transition takes time)
        await Task.Delay(2000); // Additional wait before checking windows
        
        Console.WriteLine("⏳ Waiting for login to complete and main window to open...");
        
        // Wait for main window specifically (not login window)
        var mainWindow = await WaitForWindowAsync(w => 
        {
            Console.WriteLine($"🔍 Login check - Window: '{w.Title}'");
            return w.Title == "Daily Task Manager" || // Exact match for main window
                   (w.Title.Contains("Daily Task Manager") && !w.Title.Contains("Login")); // Contains but not login
        }, 12000); // Increased timeout
        
        Console.WriteLine($"✅ Login successful! Main window: {mainWindow.Title}");
        return mainWindow;
    }

    /// <summary>
    /// Finds an element using multiple search strategies
    /// </summary>
    protected async Task<AutomationElement?> FindElementAsync(AutomationElement parent, params ConditionBase[] conditions)
    {
        foreach (var condition in conditions)
        {
            try
            {
                var element = await RetryAsync(async () => 
                {
                    var found = parent.FindFirstDescendant(condition);
                    return found?.IsAvailable == true ? found : null;
                });

                if (element != null)
                {
                    Console.WriteLine($"✅ Found element: {element.Name} (using condition: {condition})");
                    return element;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Search condition failed: {condition} - {ex.Message}");
            }
        }

        Console.WriteLine($"❌ Element not found with any of the {conditions.Length} search conditions");
        return null;
    }

    /// <summary>
    /// Waits for a window matching the specified condition
    /// </summary>
    protected async Task<Window> WaitForWindowAsync(Func<Window, bool> condition, int timeoutMs = 5000)
    {
        var startTime = DateTime.Now;

        while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
        {
            try
            {
                var windows = AppLauncher!.Application.GetAllTopLevelWindows(AppLauncher.Automation);
                Console.WriteLine($"🔍 WaitForWindow: Found {windows.Length} total windows at {DateTime.Now:HH:mm:ss.fff}");
                
                foreach (var window in windows)
                {
                    try
                    {
                        Console.WriteLine($"  📋 Checking window: '{window.Title}' (Available: {window.IsAvailable})");
                        
                        if (window.IsAvailable && condition(window))
                        {
                            Console.WriteLine($"✅ Window condition met: '{window.Title}'");
                            return window;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Error checking window: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error getting windows: {ex.Message}");
            }

            await Task.Delay(1000); // Increased delay for more stability
        }

        // Before throwing, log all current windows for debugging
        try
        {
            var windows = AppLauncher!.Application.GetAllTopLevelWindows(AppLauncher.Automation);
            Console.WriteLine($"❌ TIMEOUT: Final window state - found {windows.Length} windows:");
            foreach (var window in windows)
            {
                Console.WriteLine($"  📋 Final: '{window.Title}' (Available: {window.IsAvailable})");
            }
        }
        catch { /* ignore errors during final logging */ }

        throw new TimeoutException($"Window matching condition not found within {timeoutMs}ms");
    }

    /// <summary>
    /// Retries an async operation with exponential backoff
    /// </summary>
    protected async Task<T?> RetryAsync<T>(Func<Task<T?>> operation, int maxRetries = 3, int baseDelayMs = 500)
        where T : class
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var result = await operation();
                if (result != null)
                    return result;
            }
            catch (Exception ex) when (i < maxRetries - 1)
            {
                Console.WriteLine($"Retry {i + 1}/{maxRetries} failed: {ex.Message}");
            }

            await Task.Delay(baseDelayMs * (int)Math.Pow(2, i));
        }

        return null;
    }

    /// <summary>
    /// Asserts that an element exists and is visible
    /// </summary>
    protected void AssertElementExists(AutomationElement? element, string description)
    {
        Assert.IsNotNull(element, $"{description} should exist");
        Assert.IsTrue(element.IsAvailable, $"{description} should be available");
    }

    /// <summary>
    /// Prints detailed information about the current UI state
    /// </summary>
    protected void PrintUIState(string context = "")
    {
        Console.WriteLine($"\n=== UI State {context} ===");
        AppLauncher?.PrintUITree();
        Console.WriteLine("=========================\n");
    }

    #endregion
}