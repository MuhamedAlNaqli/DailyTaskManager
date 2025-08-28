using System.Windows;
using DailyTaskManager.App.ViewModels;

namespace DailyTaskManager.App
{
    public partial class LoginWindow : Window
    {
        private LoginViewModel _viewModel;

        public LoginWindow()
        {
            InitializeComponent();
            _viewModel = (LoginViewModel)DataContext;
            _viewModel.LoginSuccessful += OnLoginSuccessful;
        }

        private void OnLoginSuccessful(object sender, System.EventArgs e)
        {
            DialogResult = true;
            Close();
        }


        private void LoginModeButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.IsLoginMode = true;
            LoginForm.Visibility = Visibility.Visible;
            RegisterForm.Visibility = Visibility.Collapsed;
            ActionButton.Content = "Login";
            ActionButton.Command = _viewModel.LoginCommand;
        }

        private void RegisterModeButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.IsLoginMode = false;
            LoginForm.Visibility = Visibility.Collapsed;
            RegisterForm.Visibility = Visibility.Visible;
            ActionButton.Content = "Register";
            ActionButton.Command = _viewModel.RegisterCommand;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var passwordBox = sender as System.Windows.Controls.PasswordBox;
            if (passwordBox != null)
            {
                if (passwordBox.Name == "LoginPasswordBox" || passwordBox.Name == "RegisterPasswordBox")
                {
                    _viewModel.Password = passwordBox.Password;
                }
                else if (passwordBox.Name == "ConfirmPasswordBox")
                {
                    _viewModel.ConfirmPassword = passwordBox.Password;
                }
            }
        }

        protected override void OnClosed(System.EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.LoginSuccessful -= OnLoginSuccessful;
            }
            base.OnClosed(e);
        }
    }
}