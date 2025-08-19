using System.Windows;
using DailyTaskManager.App.Views;

namespace DailyTaskManager.App
{
    public partial class MainWindow : Window
    {
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
            ContentRegion.Content = new DailyTasksView();
        }

        private void ShowSettings()
        {
            ContentRegion.Content = new SettingsView();
        }
    }
}
