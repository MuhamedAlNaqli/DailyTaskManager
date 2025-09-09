using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DailyTaskManager.UI.Tests.Infrastructure;
using FlaUI.UIA3;

namespace DailyTaskManager.UI.Tests
{
    /// <summary>
    /// Tests to validate login window layout and UI improvements:
    /// - Window dimensions (no scrolling required)  
    /// - Text box padding and spacing
    /// - Card layout as window structure
    /// - All interactive elements accessibility
    /// </summary>
    [TestClass]
    [TestCategory("UILayout")]
    public class LoginWindowLayoutTests : BaseUITest
    {
        [TestMethod]
        public async Task LoginWindow_Should_DisplayAllElementsWithoutScrolling()
        {
            // Arrange & Act - Login window should be available from base test setup
            Assert.IsNotNull(MainWindow, "Login window should be available");
            Assert.IsTrue(MainWindow.Title.Contains("Login"), "Should be on login window");

            // Take screenshot for visual verification
            AppLauncher?.TakeScreenshot($"login-window-full-layout-{TestContext?.TestName}.png");

            // Assert - Window dimensions should accommodate all content without scrolling
            var windowBounds = MainWindow.BoundingRectangle;
            Assert.AreEqual(400, windowBounds.Width, "Login window width should be 400px");
            Assert.AreEqual(580, windowBounds.Height, "Login window height should be 580px (increased to fit all content)");

            // Verify no ScrollViewer is present (removed to eliminate scrolling)
            var cf = new ConditionFactory(new UIA3PropertyLibrary());
            var scrollViewers = MainWindow.FindAllDescendants(cf.ByControlType(ControlType.ScrollBar));
            Assert.AreEqual(0, scrollViewers.Length, "No scroll bars should be present - all content should fit without scrolling");

            Console.WriteLine($"✅ Login window dimensions verified: {windowBounds.Width}x{windowBounds.Height}");
        }

        [TestMethod]
        public async Task LoginWindow_TextBoxes_Should_HaveProperPadding()
        {
            // Arrange & Act
            Assert.IsTrue(MainWindow.Title.Contains("Login"), "Should be on login window");
            
            var cf = new ConditionFactory(new UIA3PropertyLibrary());
            
            // Find username and password text boxes
            var usernameBox = MainWindow.FindFirstDescendant(cf.ByName("UsernameTextBox"));
            var passwordBox = MainWindow.FindFirstDescendant(cf.ByName("PasswordTextBox"));
            
            Assert.IsNotNull(usernameBox, "Username text box should be found");
            Assert.IsNotNull(passwordBox, "Password text box should be found");

            // Take screenshot showing text box layout
            AppLauncher?.TakeScreenshot($"login-textbox-padding-{TestContext?.TestName}.png");

            // Assert - Text boxes should have proper padding from window edges
            var windowBounds = MainWindow.BoundingRectangle;
            var usernameBoxBounds = usernameBox.BoundingRectangle;
            var passwordBoxBounds = passwordBox.BoundingRectangle;

            // Calculate padding from left edge (should be 32px margin + some padding)
            var leftPadding = usernameBoxBounds.X - windowBounds.X;
            Assert.IsTrue(leftPadding >= 30, $"Username box should have sufficient left padding. Actual: {leftPadding}px");

            // Calculate padding from right edge
            var rightPadding = windowBounds.Right - usernameBoxBounds.Right;
            Assert.IsTrue(rightPadding >= 30, $"Username box should have sufficient right padding. Actual: {rightPadding}px");

            // Verify internal padding of text boxes (text boxes should use Padding="12,10")
            // This is verified by checking the text box bounds are reasonable for the content
            Assert.IsTrue(usernameBoxBounds.Height >= 30, "Username box should have sufficient height for padding");
            Assert.IsTrue(passwordBoxBounds.Height >= 30, "Password box should have sufficient height for padding");

            Console.WriteLine($"✅ Text box padding verified - Left: {leftPadding}px, Right: {rightPadding}px");
        }

        [TestMethod]
        public async Task LoginWindow_Should_UseCardAsWindowLayout()
        {
            // Arrange & Act
            Assert.IsTrue(MainWindow.Title.Contains("Login"), "Should be on login window");

            // Take screenshot for card layout verification
            AppLauncher?.TakeScreenshot($"login-card-layout-{TestContext?.TestName}.png");

            // Assert - Window should have white background and rounded corners (card as window)
            // The window itself should be the card, not contain a card
            var windowBounds = MainWindow.BoundingRectangle;

            // Verify window properties that indicate card-like behavior
            // The window should be reasonably sized and centered
            Assert.IsTrue(windowBounds.Width == 400, "Window width should match card design");
            Assert.IsTrue(windowBounds.Height == 520, "Window height should match card design");

            // Verify no nested card structure - should be direct content in window
            var cf = new ConditionFactory(new UIA3PropertyLibrary());
            
            // Look for the main content structure - should be direct StackPanel, not nested in Border/Card
            var mainStackPanel = MainWindow.FindFirstDescendant(cf.ByControlType(ControlType.Pane));
            Assert.IsNotNull(mainStackPanel, "Main content pane should be found");

            // Verify content is properly structured without unnecessary nesting
            var allPanes = MainWindow.FindAllDescendants(cf.ByControlType(ControlType.Pane));
            Console.WriteLine($"🔍 Found {allPanes.Length} panes in login window structure");

            // The layout should be clean - window -> grid -> stackpanel -> content
            Assert.IsTrue(allPanes.Length <= 5, "Should not have excessive nested panes (indicates clean card-as-window structure)");

            Console.WriteLine($"✅ Card-as-window layout verified with clean structure");
        }

        [TestMethod]
        public async Task LoginWindow_Should_ContainAllInteractiveElements()
        {
            // Arrange & Act
            Assert.IsTrue(MainWindow.Title.Contains("Login"), "Should be on login window");
            
            var cf = new ConditionFactory(new UIA3PropertyLibrary());

            // Take screenshot for interactive elements verification
            AppLauncher?.TakeScreenshot($"login-interactive-elements-{TestContext?.TestName}.png");

            // Assert - All required interactive elements should be present and accessible

            // 1. Username text box
            var usernameBox = MainWindow.FindFirstDescendant(cf.ByName("UsernameTextBox"));
            Assert.IsNotNull(usernameBox, "Username text box should be present");
            Assert.IsTrue(usernameBox.IsEnabled, "Username text box should be enabled");

            // 2. Password text box
            var passwordBox = MainWindow.FindFirstDescendant(cf.ByName("PasswordTextBox"));
            Assert.IsNotNull(passwordBox, "Password text box should be present");
            Assert.IsTrue(passwordBox.IsEnabled, "Password text box should be enabled");

            // 3. Sign In button
            var signInButton = MainWindow.FindFirstDescendant(cf.ByName("SignInButton"));
            Assert.IsNotNull(signInButton, "Sign In button should be present");
            Assert.IsTrue(signInButton.IsEnabled, "Sign In button should be enabled");

            // 4. Remember Me checkbox
            var rememberMeCheckbox = MainWindow.FindFirstDescendant(cf.ByControlType(ControlType.CheckBox));
            Assert.IsNotNull(rememberMeCheckbox, "Remember Me checkbox should be present");
            Assert.IsTrue(rememberMeCheckbox.IsEnabled, "Remember Me checkbox should be enabled");

            // 5. Google OAuth button
            var googleButton = MainWindow.FindFirstDescendant(cf.ByName("Continue with Google"));
            if (googleButton == null)
            {
                var allButtons = MainWindow.FindAllDescendants(cf.ByControlType(ControlType.Button));
                googleButton = allButtons.FirstOrDefault(b => b.Name?.Contains("Google") == true);
            }
            Assert.IsNotNull(googleButton, "Google OAuth button should be present");
            Assert.IsTrue(googleButton.IsEnabled, "Google OAuth button should be enabled");

            // 6. Create Account button
            var createAccountButton = MainWindow.FindFirstDescendant(cf.ByName("Create Account"));
            if (createAccountButton == null)
            {
                var allButtons = MainWindow.FindAllDescendants(cf.ByControlType(ControlType.Button));
                createAccountButton = allButtons.FirstOrDefault(b => b.Name?.Contains("Create") == true);
            }
            Assert.IsNotNull(createAccountButton, "Create Account button should be present");
            Assert.IsTrue(createAccountButton.IsEnabled, "Create Account button should be enabled");

            // Verify elements are positioned within window bounds
            var windowBounds = MainWindow.BoundingRectangle;
            var usernameBoxBounds = usernameBox.BoundingRectangle;
            var signInButtonBounds = signInButton.BoundingRectangle;

            Assert.IsTrue(usernameBoxBounds.Y > windowBounds.Y, "Username box should be below window top");
            Assert.IsTrue(signInButtonBounds.Y > usernameBoxBounds.Y, "Sign In button should be below username box");
            Assert.IsTrue(signInButtonBounds.Bottom < windowBounds.Bottom, "All elements should fit within window bounds");

            Console.WriteLine($"✅ All interactive elements verified and properly positioned");
        }

        [TestMethod]
        public async Task LoginWindow_Should_HaveCompactSpacing()
        {
            // Arrange & Act
            Assert.IsTrue(MainWindow.Title.Contains("Login"), "Should be on login window");
            
            var cf = new ConditionFactory(new UIA3PropertyLibrary());

            // Take screenshot for spacing verification
            AppLauncher?.TakeScreenshot($"login-spacing-verification-{TestContext?.TestName}.png");

            // Find key elements for spacing verification
            var appIcon = MainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text).And(cf.ByName("📅")));
            var usernameBox = MainWindow.FindFirstDescendant(cf.ByName("UsernameTextBox"));
            var passwordBox = MainWindow.FindFirstDescendant(cf.ByName("PasswordTextBox"));
            var signInButton = MainWindow.FindFirstDescendant(cf.ByName("SignInButton"));
            
            Assert.IsNotNull(appIcon, "App icon should be found");
            Assert.IsNotNull(usernameBox, "Username box should be found");
            Assert.IsNotNull(passwordBox, "Password box should be found");
            Assert.IsNotNull(signInButton, "Sign In button should be found");

            // Assert - Verify compact but readable spacing
            var iconBounds = appIcon.BoundingRectangle;
            var usernameBounds = usernameBox.BoundingRectangle;
            var passwordBounds = passwordBox.BoundingRectangle;
            var buttonBounds = signInButton.BoundingRectangle;

            // Icon should be at top with reasonable spacing to form elements
            var iconToUsernameSpacing = usernameBounds.Y - iconBounds.Bottom;
            Assert.IsTrue(iconToUsernameSpacing >= 20 && iconToUsernameSpacing <= 60, 
                $"Icon to username spacing should be compact but readable. Actual: {iconToUsernameSpacing}px");

            // Username to password should be close but not cramped
            var usernameToPasswordSpacing = passwordBounds.Y - usernameBounds.Bottom;
            Assert.IsTrue(usernameToPasswordSpacing >= 10 && usernameToPasswordSpacing <= 25, 
                $"Username to password spacing should be compact. Actual: {usernameToPasswordSpacing}px");

            // Password to sign in button should be reasonable
            var passwordToButtonSpacing = buttonBounds.Y - passwordBounds.Bottom;
            Assert.IsTrue(passwordToButtonSpacing >= 15 && passwordToButtonSpacing <= 40, 
                $"Password to button spacing should be reasonable. Actual: {passwordToButtonSpacing}px");

            // Overall window should be efficiently used - button should not be too far from bottom
            var windowBounds = MainWindow.BoundingRectangle;
            var buttonToBottomSpacing = windowBounds.Bottom - buttonBounds.Bottom;
            Assert.IsTrue(buttonToBottomSpacing >= 30 && buttonToBottomSpacing <= 150, 
                $"Button should be reasonably positioned from window bottom. Actual: {buttonToBottomSpacing}px");

            Console.WriteLine($"✅ Compact spacing verified - Icon→Username: {iconToUsernameSpacing}px, Username→Password: {usernameToPasswordSpacing}px, Password→Button: {passwordToButtonSpacing}px");
        }

        [TestMethod]
        public async Task LoginWindow_BottomElements_Should_BeFullyVisible()
        {
            // Arrange & Act - Ensure we're on login window
            Assert.IsTrue(MainWindow.Title.Contains("Login"), "Should be on login window");

            // Take screenshot for bottom element visibility verification
            AppLauncher?.TakeScreenshot($"login-bottom-elements-visibility-{TestContext?.TestName}.png");

            var cf = new ConditionFactory(new UIA3PropertyLibrary());

            // Find the bottom-most elements: "Don't have an account?" text and "Create Account" button
            var createAccountButton = MainWindow.FindFirstDescendant(cf.ByName("Create Account"));
            if (createAccountButton == null)
            {
                var allButtons = MainWindow.FindAllDescendants(cf.ByControlType(ControlType.Button));
                createAccountButton = allButtons.FirstOrDefault(b => b.Name?.Contains("Create") == true);
            }

            // Also look for the "Don't have an account?" text
            var allTextElements = MainWindow.FindAllDescendants(cf.ByControlType(ControlType.Text));
            var dontHaveAccountText = allTextElements.FirstOrDefault(t => t.Name?.Contains("Don't have") == true);

            // Assert - Create Account button should be present and fully visible
            Assert.IsNotNull(createAccountButton, "Create Account button should be found");

            var windowBounds = MainWindow.BoundingRectangle;
            var createAccountBounds = createAccountButton.BoundingRectangle;

            // The Create Account button should be fully within the window bounds
            Assert.IsTrue(createAccountBounds.Y >= windowBounds.Y, 
                "Create Account button should be below window top");
            Assert.IsTrue(createAccountBounds.Bottom <= windowBounds.Bottom, 
                $"Create Account button should be fully visible within window. Button bottom: {createAccountBounds.Bottom}, Window bottom: {windowBounds.Bottom}");

            // Check that there's reasonable spacing from window bottom
            var bottomSpacing = windowBounds.Bottom - createAccountBounds.Bottom;
            Assert.IsTrue(bottomSpacing >= 5, 
                $"Create Account button should have some spacing from window bottom. Actual spacing: {bottomSpacing}px");
            Assert.IsTrue(bottomSpacing <= 40, 
                $"Create Account button should not be too far from window bottom. Actual spacing: {bottomSpacing}px");

            // If "Don't have an account?" text is found, verify it's also visible
            if (dontHaveAccountText != null)
            {
                var dontHaveAccountBounds = dontHaveAccountText.BoundingRectangle;
                Assert.IsTrue(dontHaveAccountBounds.Bottom <= windowBounds.Bottom, 
                    "Don't have an account text should be fully visible within window");
                
                // Should be above the Create Account button
                Assert.IsTrue(dontHaveAccountBounds.Bottom <= createAccountBounds.Y, 
                    "Don't have an account text should be above Create Account button");
            }

            Console.WriteLine($"✅ Bottom elements visibility verified - Create Account button at Y:{createAccountBounds.Y}, Bottom spacing: {bottomSpacing}px");
            
            if (dontHaveAccountText != null)
            {
                Console.WriteLine($"   Don't have account text also verified at Y:{dontHaveAccountText.BoundingRectangle.Y}");
            }
        }
    }
}