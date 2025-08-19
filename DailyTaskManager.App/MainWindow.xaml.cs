using System.Windows;
using DailyTaskManager.App.Views;

namespace DailyTaskManager.App
{
    public partial class MainWindow : Window
    {
        private DailyTasksView _dailyTasksView;
        private SettingsView _settingsView;

        public MainWindow()
        {
            InitializeComponent();

            // Load default view
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
    }
}
