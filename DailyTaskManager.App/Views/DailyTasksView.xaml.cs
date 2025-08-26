using System.Windows;
using System.Windows.Controls;

namespace DailyTaskManager.App.Views
{
    /// <summary>
    /// Interaction logic for DailyTasksView1.xaml
    /// </summary>
    public partial class DailyTasksView : UserControl
    {
        public DailyTasksView()
        {
            InitializeComponent();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                button.ContextMenu.IsOpen = true;
            }
        }
    }
}
