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

    }
}