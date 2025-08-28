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
            
            // Initialize database using Entity Framework migrations
            try
            {
                using (var context = new DailyTaskManager.Core.DailyTaskManagerDbContext())
                {
                    // This will apply any pending migrations
                    System.Data.Entity.Database.SetInitializer<DailyTaskManager.Core.DailyTaskManagerDbContext>(null);
                    
                    // Check if database is accessible
                    var canConnect = context.Database.Exists();
                    if (!canConnect)
                    {
                        throw new System.Exception("Cannot connect to the database. Please ensure PostgreSQL is running and the DailyTaskManager database exists.");
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Database connection failed: {ex.Message}\n\nPlease run EF migrations first:\n1. Open Package Manager Console in Visual Studio\n2. Run: Update-Database", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
