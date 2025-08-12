using DailyTaskManager.App.ViewModels;
using DailyTaskManager.Core.Models;
using System.Windows;

namespace DailyTaskManager.App
{
    /// <summary>
    /// Interaction logic for EditTaskWindow.xaml
    /// </summary>
    public partial class EditTaskWindow : Window
    {
        public EditTaskWindow(DailyTask task)
        {
            InitializeComponent();
            var vm = new EditTaskViewModel(task);
            vm.RequestClose += (s, result) =>
            {
                DialogResult = result;
                Close();
            };
            DataContext = vm;
        }
    }
}
