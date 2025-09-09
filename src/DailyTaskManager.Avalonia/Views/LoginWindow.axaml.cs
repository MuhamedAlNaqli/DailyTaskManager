using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DailyTaskManager.Avalonia.ViewModels;

namespace DailyTaskManager.Avalonia.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            
            // Handle Enter key presses for the entire window
            KeyDown += LoginWindow_KeyDown;
        }

        public LoginWindow(LoginViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        private void LoginWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            // Trigger login when Enter key is pressed
            if (e.Key == Key.Enter && DataContext is LoginViewModel viewModel)
            {
                // Only trigger if login command can execute (not already loading, has valid data, etc.)
                if (viewModel.LoginCommand.CanExecute(null))
                {
                    viewModel.LoginCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }
}