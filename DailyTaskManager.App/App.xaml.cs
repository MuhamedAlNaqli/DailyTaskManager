using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DailyTaskManager.Core.Services;

namespace DailyTaskManager.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Set shutdown mode to explicit - app won't close when windows close
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            
            // Initialize database schema
            try
            {
                DailyTaskManager.Core.DailyTaskManagerDbContext.InitializeDatabase();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Database initialization failed: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }
            
            // Configure dependency injection container
            ServiceContainer.ConfigureServices();
            
            // Check authentication and show appropriate window
            ShowLoginOrMainWindow();
            
            base.OnStartup(e);
        }
        
        private void ShowLoginOrMainWindow()
        {
            var authService = ServiceContainer.Instance.Resolve<IAuthenticationService>();
            
            if (!authService.IsAuthenticated)
            {
                // Show login window
                var loginWindow = new LoginWindow();
                var loginResult = loginWindow.ShowDialog();
                
                if (loginResult != true)
                {
                    // User canceled login, exit application
                    Shutdown();
                    return;
                }
                
                // Check authentication status again after dialog closes
                if (!authService.IsAuthenticated)
                {
                    // Authentication failed, exit application
                    MessageBox.Show("Authentication failed. Application will close.", "Authentication Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown();
                    return;
                }
            }
            
            // User is authenticated, show main window
            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            
            // Change shutdown mode back to OnMainWindowClose now that we have a main window
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            
            mainWindow.Show();
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            // Clean up services - don't call async operations during shutdown
            try
            {
                ServiceContainer.Instance.Clear();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during application exit cleanup: {ex.Message}");
            }
            
            base.OnExit(e);
        }
    }
}
