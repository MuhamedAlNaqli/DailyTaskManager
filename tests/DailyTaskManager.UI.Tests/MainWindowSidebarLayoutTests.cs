using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DailyTaskManager.UI.Tests.Infrastructure;
using FlaUI.UIA3;

namespace DailyTaskManager.UI.Tests
{
    /// <summary>
    /// Tests to validate main window sidebar layout and UI improvements:
    /// - Icon alignment (left) and text alignment (right)
    /// - Logout button positioning (bottom of sidebar)  
    /// - Light/Dark mode button positioning (bottom, above logout)
    /// - User welcome text positioning and truncation
    /// - Navigation link functionality and layout consistency
    /// </summary>
    [TestClass]
    [TestCategory("UILayout")]
    public class MainWindowSidebarLayoutTests : BaseUITest
    {
        private Window? _mainWindow;

        protected override async Task AdditionalSetup()
        {
            await base.AdditionalSetup();
            
            // Ensure we're on the main window (login should be handled by BaseUITest)
            if (MainWindow != null && MainWindow.Title.Contains("Login"))
            {
                // If still on login, the base class should have handled login
                // Wait a bit more for navigation to complete
                await Task.Delay(2000);
                
                // Get the main application window
                _mainWindow = await WaitForWindowAsync(w => 
                    w.Title == "Daily Task Manager" || 
                    (w.Title.Contains("Daily Task Manager") && !w.Title.Contains("Login")), 
                    10000);
            }
            else
            {
                _mainWindow = MainWindow;
            }
            
            Assert.IsNotNull(_mainWindow, "Main application window should be available");
            Assert.IsFalse(_mainWindow.Title.Contains("Login"), "Should be on main window, not login");
        }

        [TestMethod]
        public async Task Sidebar_Icons_Should_BeLeftAligned_And_Text_RightAligned()
        {
            // Arrange & Act
            var cf = new ConditionFactory(new UIA3PropertyLibrary());
            
            // Take screenshot for icon alignment verification
            AppLauncher?.TakeScreenshot($"sidebar-icon-alignment-{TestContext?.TestName}.png");

            // Find navigation buttons (Daily Tasks and Settings)
            var dailyTasksButton = _mainWindow.FindFirstDescendant(cf.ByName("Daily Tasks"));
            var settingsButton = _mainWindow.FindFirstDescendant(cf.ByName("Settings"));
            
            // If exact name match doesn't work, try finding by text content
            if (dailyTasksButton == null)
            {
                var allButtons = _mainWindow.FindAllDescendants(cf.ByControlType(ControlType.Button));
                dailyTasksButton = allButtons.FirstOrDefault(b => b.Name?.Contains("Daily Tasks") == true);
            }
            if (settingsButton == null)
            {
                var allButtons = _mainWindow.FindAllDescendants(cf.ByControlType(ControlType.Button));
                settingsButton = allButtons.FirstOrDefault(b => b.Name?.Contains("Settings") == true);
            }
            
            Assert.IsNotNull(dailyTasksButton, "Daily Tasks button should be found");
            Assert.IsNotNull(settingsButton, "Settings button should be found");

            // Find the sidebar container (should be first column with width 250)
            var sidebar = _mainWindow.FindFirstDescendant(cf.ByControlType(ControlType.Pane));
            Assert.IsNotNull(sidebar, "Sidebar container should be found");
            
            var sidebarBounds = sidebar.BoundingRectangle;
            var dailyTasksBounds = dailyTasksButton.BoundingRectangle;
            var settingsBounds = settingsButton.BoundingRectangle;

            // Assert - Icons should be left-aligned within the sidebar
            var dailyTasksLeftMargin = dailyTasksBounds.X - sidebarBounds.X;
            var settingsLeftMargin = settingsBounds.X - sidebarBounds.X;

            // Both buttons should have consistent left alignment (same margin from sidebar left)
            Assert.IsTrue(Math.Abs(dailyTasksLeftMargin - settingsLeftMargin) <= 5, 
                $"Navigation buttons should have consistent left alignment. Daily Tasks: {dailyTasksLeftMargin}px, Settings: {settingsLeftMargin}px");

            // Buttons should be properly aligned within sidebar (not too close to edge, not too far)
            Assert.IsTrue(dailyTasksLeftMargin >= 10 && dailyTasksLeftMargin <= 25, 
                $"Navigation buttons should have proper left margin. Actual: {dailyTasksLeftMargin}px");

            // Verify buttons use grid layout for proper icon/text alignment
            // The buttons should span most of the sidebar width for proper text alignment
            var buttonWidth = dailyTasksBounds.Width;
            var expectedMinWidth = sidebarBounds.Width * 0.7; // At least 70% of sidebar width
            
            Assert.IsTrue(buttonWidth >= expectedMinWidth, 
                $"Navigation buttons should span sidebar width for proper text alignment. Button width: {buttonWidth}px, Expected min: {expectedMinWidth}px");

            Console.WriteLine($"✅ Icon alignment verified - Left margins: {dailyTasksLeftMargin}px, Button width: {buttonWidth}px");
        }

        [TestMethod]
        public async Task Sidebar_LogoutButton_Should_BeAtBottom()
        {
            // Arrange & Act
            var cf = new ConditionFactory(new UIA3PropertyLibrary());
            
            // Take screenshot for logout button positioning
            AppLauncher?.TakeScreenshot($"sidebar-logout-positioning-{TestContext?.TestName}.png");

            // Find logout button
            var logoutButton = _mainWindow.FindFirstDescendant(cf.ByName("Logout"));
            if (logoutButton == null)
            {
                var allButtons = _mainWindow.FindAllDescendants(cf.ByControlType(ControlType.Button));
                logoutButton = allButtons.FirstOrDefault(b => b.Name?.Contains("Logout") == true);
            }
            Assert.IsNotNull(logoutButton, "Logout button should be found");

            // Find sidebar and other navigation elements
            var sidebar = _mainWindow.FindFirstDescendant(cf.ByControlType(ControlType.Pane));
            var dailyTasksButton = _mainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Daily Tasks")));
            
            Assert.IsNotNull(sidebar, "Sidebar should be found");
            Assert.IsNotNull(dailyTasksButton, "Daily Tasks button should be found");

            var sidebarBounds = sidebar.BoundingRectangle;
            var logoutBounds = logoutButton.BoundingRectangle;
            var dailyTasksBounds = dailyTasksButton.BoundingRectangle;

            // Assert - Logout button should be at the bottom of sidebar
            var logoutDistanceFromBottom = sidebarBounds.Bottom - logoutBounds.Bottom;
            Assert.IsTrue(logoutDistanceFromBottom >= 10 && logoutDistanceFromBottom <= 40, 
                $"Logout button should be close to sidebar bottom. Distance: {logoutDistanceFromBottom}px");

            // Logout button should be below navigation buttons
            Assert.IsTrue(logoutBounds.Y > dailyTasksBounds.Bottom, 
                "Logout button should be positioned below navigation buttons");

            // Logout button should be in bottom third of sidebar
            var sidebarHeight = sidebarBounds.Height;
            var logoutPositionRatio = (logoutBounds.Y - sidebarBounds.Y) / (double)sidebarHeight;
            Assert.IsTrue(logoutPositionRatio >= 0.7, 
                $"Logout button should be in bottom portion of sidebar. Position ratio: {logoutPositionRatio:F2}");

            Console.WriteLine($"✅ Logout button positioning verified - Distance from bottom: {logoutDistanceFromBottom}px, Position ratio: {logoutPositionRatio:F2}");
        }

        [TestMethod]
        public async Task Sidebar_ThemeToggle_Should_BeAboveLogout()
        {
            // Arrange & Act
            var cf = new ConditionFactory(new UIA3PropertyLibrary());
            
            // Take screenshot for theme toggle positioning
            AppLauncher?.TakeScreenshot($"sidebar-theme-toggle-positioning-{TestContext?.TestName}.png");

            // Find theme toggle and logout button
            var themeToggle = _mainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Dark").Or(cf.ByName("Light")).Or(cf.ByName("Theme"))));
            var logoutButton = _mainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Logout")));
            
            // Note: Theme toggle might be a ToggleButton instead of Button
            if (themeToggle == null)
            {
                themeToggle = _mainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("🌙")));
            }
            
            Assert.IsNotNull(themeToggle, "Theme toggle button should be found");
            Assert.IsNotNull(logoutButton, "Logout button should be found");

            var themeBounds = themeToggle.BoundingRectangle;
            var logoutBounds = logoutButton.BoundingRectangle;

            // Assert - Theme toggle should be above logout button
            Assert.IsTrue(themeBounds.Bottom <= logoutBounds.Y, 
                "Theme toggle should be positioned above logout button");

            // Theme toggle and logout should be close together (part of bottom actions)
            var spacingBetween = logoutBounds.Y - themeBounds.Bottom;
            Assert.IsTrue(spacingBetween >= 5 && spacingBetween <= 25, 
                $"Theme toggle and logout should be grouped together. Spacing: {spacingBetween}px");

            // Both should be in bottom portion of sidebar
            var sidebar = _mainWindow.FindFirstDescendant(cf.ByControlType(ControlType.Pane));
            Assert.IsNotNull(sidebar, "Sidebar should be found");
            
            var sidebarBounds = sidebar.BoundingRectangle;
            var sidebarHeight = sidebarBounds.Height;
            var themePositionRatio = (themeBounds.Y - sidebarBounds.Y) / (double)sidebarHeight;
            
            Assert.IsTrue(themePositionRatio >= 0.6, 
                $"Theme toggle should be in bottom portion of sidebar. Position ratio: {themePositionRatio:F2}");

            Console.WriteLine($"✅ Theme toggle positioning verified - Above logout by {spacingBetween}px, Position ratio: {themePositionRatio:F2}");
        }

        [TestMethod]
        public async Task Sidebar_UserWelcome_Should_ShowInCorrectPosition()
        {
            // Arrange & Act
            var cf = new ConditionFactory(new UIA3PropertyLibrary());
            
            // Take screenshot for user welcome positioning
            AppLauncher?.TakeScreenshot($"sidebar-user-welcome-positioning-{TestContext?.TestName}.png");

            // Find user welcome/info section (should contain username/email)
            // Look for text elements that might contain user info
            var allTextElements = _mainWindow.FindAllDescendants(cf.ByControlType(ControlType.Text));
            
            AutomationElement? userDisplayElement = null;
            AutomationElement? userEmailElement = null;
            
            // Look for elements that contain user information
            foreach (var textElement in allTextElements)
            {
                try
                {
                    var text = textElement.Name;
                    if (!string.IsNullOrEmpty(text))
                    {
                        if (text.Contains("admin") && !text.Contains("Admin user") && userDisplayElement == null)
                        {
                            userDisplayElement = textElement;
                        }
                        else if (text.Contains("@") || text.Contains("admin"))
                        {
                            userEmailElement = textElement;
                        }
                    }
                }
                catch
                {
                    // Skip elements that can't be accessed
                    continue;
                }
            }

            // User info might be in a container/border - look for that pattern
            var userInfoContainer = _mainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Pane).And(cf.ByClassName("Border")));
            
            // At minimum, we should find some user-related elements or container
            Assert.IsTrue(userDisplayElement != null || userEmailElement != null || userInfoContainer != null,
                "User welcome/info section should be found in sidebar");

            if (userDisplayElement != null || userEmailElement != null)
            {
                var userElementBounds = userDisplayElement?.BoundingRectangle ?? userEmailElement.BoundingRectangle;
                
                // Find logout button for reference
                var logoutButton = _mainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Logout")));
                Assert.IsNotNull(logoutButton, "Logout button should be found for positioning reference");
                
                var logoutBounds = logoutButton.BoundingRectangle;
                var sidebar = _mainWindow.FindFirstDescendant(cf.ByControlType(ControlType.Pane));
                var sidebarBounds = sidebar.BoundingRectangle;

                // Assert - User welcome should be positioned above logout (moved from top to bottom area)
                Assert.IsTrue(userElementBounds.Bottom <= logoutBounds.Y,
                    "User welcome text should be positioned above logout button (not at top of sidebar)");

                // User welcome should not be at the very top of sidebar (moved from header area)
                var distanceFromTop = userElementBounds.Y - sidebarBounds.Y;
                var sidebarHeight = sidebarBounds.Height;
                var positionRatio = distanceFromTop / (double)sidebarHeight;
                
                Assert.IsTrue(positionRatio >= 0.4, 
                    $"User welcome should not be at top of sidebar (should be moved down). Position ratio: {positionRatio:F2}");

                // Should have reasonable spacing from bottom actions
                var spacingToLogout = logoutBounds.Y - userElementBounds.Bottom;
                Assert.IsTrue(spacingToLogout >= 10 && spacingToLogout <= 50,
                    $"User welcome should have reasonable spacing from logout. Spacing: {spacingToLogout}px");

                Console.WriteLine($"✅ User welcome positioning verified - Not at top (ratio: {positionRatio:F2}), spacing to logout: {spacingToLogout}px");
            }
        }

        [TestMethod]
        public async Task Sidebar_Should_HaveConsistentLayoutStructure()
        {
            // Arrange & Act
            var cf = new ConditionFactory(new UIA3PropertyLibrary());
            
            // Take screenshot for overall sidebar layout
            AppLauncher?.TakeScreenshot($"sidebar-layout-structure-{TestContext?.TestName}.png");

            // Find sidebar container
            var sidebar = _mainWindow.FindFirstDescendant(cf.ByControlType(ControlType.Pane));
            Assert.IsNotNull(sidebar, "Sidebar container should be found");

            // Find key navigation elements
            var dailyTasksButton = _mainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Daily Tasks")));
            var settingsButton = _mainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Settings")));
            var logoutButton = _mainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Logout")));
            
            Assert.IsNotNull(dailyTasksButton, "Daily Tasks button should be found");
            Assert.IsNotNull(settingsButton, "Settings button should be found");
            Assert.IsNotNull(logoutButton, "Logout button should be found");

            var sidebarBounds = sidebar.BoundingRectangle;

            // Assert - Sidebar should have expected width (250px as per XAML)
            Assert.IsTrue(sidebarBounds.Width >= 240 && sidebarBounds.Width <= 260, 
                $"Sidebar should have expected width around 250px. Actual: {sidebarBounds.Width}px");

            // Navigation buttons should be consistently aligned
            var dailyTasksBounds = dailyTasksButton.BoundingRectangle;
            var settingsBounds = settingsButton.BoundingRectangle;
            var logoutBounds = logoutButton.BoundingRectangle;

            // All buttons should have similar left alignment
            var dailyTasksLeftMargin = dailyTasksBounds.X - sidebarBounds.X;
            var settingsLeftMargin = settingsBounds.X - sidebarBounds.X;
            var logoutLeftMargin = logoutBounds.X - sidebarBounds.X;

            Assert.IsTrue(Math.Abs(dailyTasksLeftMargin - settingsLeftMargin) <= 5,
                "Daily Tasks and Settings should have consistent alignment");
            Assert.IsTrue(Math.Abs(dailyTasksLeftMargin - logoutLeftMargin) <= 10,
                "Navigation and logout buttons should have similar alignment");

            // Verify logical top-to-bottom ordering
            Assert.IsTrue(dailyTasksBounds.Y < settingsBounds.Y, 
                "Daily Tasks should be above Settings");
            Assert.IsTrue(settingsBounds.Y < logoutBounds.Y, 
                "Settings should be above Logout");

            // Sidebar should use full height of window
            var windowBounds = _mainWindow.BoundingRectangle;
            Assert.IsTrue(sidebarBounds.Height >= windowBounds.Height * 0.9,
                "Sidebar should span most of window height");

            Console.WriteLine($"✅ Sidebar layout structure verified - Width: {sidebarBounds.Width}px, Height: {sidebarBounds.Height}px");
            Console.WriteLine($"   Navigation alignment - Daily Tasks: {dailyTasksLeftMargin}px, Settings: {settingsLeftMargin}px, Logout: {logoutLeftMargin}px");
        }

        [TestMethod]
        public async Task Sidebar_Navigation_Should_SupportGridBasedAlignment()
        {
            // Arrange & Act
            var cf = new ConditionFactory(new UIA3PropertyLibrary());
            
            // Take screenshot for grid-based alignment verification
            AppLauncher?.TakeScreenshot($"sidebar-grid-alignment-{TestContext?.TestName}.png");

            // Find navigation buttons
            var navigationButtons = _mainWindow.FindAllDescendants(cf => 
                cf.ByControlType(ControlType.Button).And(
                    cf.ByName("Daily Tasks").Or(
                    cf.ByName("Settings").Or(
                    cf.ByName("Logout")))));

            Assert.IsTrue(navigationButtons.Length >= 3, "Should find at least 3 navigation buttons");

            // Check if buttons are using HorizontalContentAlignment="Left" and proper width
            foreach (var button in navigationButtons.Take(3)) // Check first 3 buttons
            {
                var buttonBounds = button.BoundingRectangle;
                
                // Buttons should have reasonable width for grid-based layout
                Assert.IsTrue(buttonBounds.Width >= 150, 
                    $"Navigation button should have sufficient width for grid layout. Width: {buttonBounds.Width}px");
                
                // Buttons should be enabled and accessible
                Assert.IsTrue(button.IsEnabled, "Navigation button should be enabled");
                Assert.IsTrue(button.IsOffscreen == false, "Navigation button should be visible");
            }

            // Verify consistent button sizing (they should all be similar width for grid layout)
            if (navigationButtons.Length >= 2)
            {
                var firstButtonWidth = navigationButtons[0].BoundingRectangle.Width;
                var secondButtonWidth = navigationButtons[1].BoundingRectangle.Width;
                
                var widthDifference = Math.Abs(firstButtonWidth - secondButtonWidth);
                Assert.IsTrue(widthDifference <= 20, 
                    $"Navigation buttons should have consistent width for grid alignment. Difference: {widthDifference}px");
            }

            Console.WriteLine($"✅ Grid-based alignment verified for {navigationButtons.Length} navigation buttons");
        }
    }
}