using Avalonia.Controls;
using Avalonia.Interactivity;
using DailyTaskManager.Avalonia.ViewModels;

namespace DailyTaskManager.Avalonia.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        public LoginWindow(LoginViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}