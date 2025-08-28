using System.Windows;
using DailyTaskManager.App.Views;
using DailyTaskManager.Core.Services;

namespace DailyTaskManager.App
{
    public partial class MainWindow : Window
    {
        private DailyTasksView _dailyTasksView;
        private SettingsView _settingsView;

        public MainWindow()
        {
            InitializeComponent();
            UpdateUserDisplay();
            ShowDailyTasks();
        }

        private void BtnDailyTasks_Click(object sender, RoutedEventArgs e)
        {
            ShowDailyTasks();
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            ShowSettings();
        }
        private void ShowDailyTasks()
        {
            if (_dailyTasksView == null)
            {
                _dailyTasksView = new DailyTasksView();
            }
            ContentRegion.Content = _dailyTasksView;
        }

        private void ShowSettings()
        {
            if (_settingsView == null)
            {
                _settingsView = new SettingsView();
            }
            ContentRegion.Content = _settingsView;
        }
        
        private void UpdateUserDisplay()
        {
            var authService = ServiceContainer.Instance.Resolve<IAuthenticationService>();
            if (authService.IsAuthenticated && authService.CurrentUser != null)
            {
                var user = authService.CurrentUser;
                var displayName = !string.IsNullOrEmpty(user.DisplayName) ? user.DisplayName : user.Username;
                UserDisplayName.Text = $"Welcome, {displayName}";
            }
            else
            {
                UserDisplayName.Text = "Welcome, User";
            }
        }
        
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to logout?",
                "Confirm Logout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                var authService = ServiceContainer.Instance.Resolve<IAuthenticationService>();
                authService.LogoutAsync().Wait();
                
                Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                this.Close();
                
                var loginWindow = new LoginWindow();
                var loginResult = loginWindow.ShowDialog();
                
                if (loginResult == true)
                {
                    var newMainWindow = new MainWindow();
                    Application.Current.MainWindow = newMainWindow;
                    Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                    newMainWindow.Show();
                }
                else
                {
                    Application.Current.Shutdown();
                }
            }
        }
    }
}
