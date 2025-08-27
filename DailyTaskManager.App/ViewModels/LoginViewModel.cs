using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DailyTaskManager.App.ViewModels;
using DailyTaskManager.Core.Models;
using DailyTaskManager.Core.Services;

namespace DailyTaskManager.App.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IOAuthService _oAuthService;
        private bool _isLoginMode = true;
        private string _username = "";
        private string _email = "";
        private string _password = "";
        private string _confirmPassword = "";
        private string _displayName = "";
        private bool _rememberMe = false;
        private bool _isLoading = false;
        private string _errorMessage = "";

        public LoginViewModel()
        {
            _authenticationService = ServiceContainer.Instance.Resolve<IAuthenticationService>();
            _oAuthService = ServiceContainer.Instance.Resolve<IOAuthService>();
            
            LoginCommand = new RelayCommand(async _ => await LoginAsync(), _ => CanLogin());
            RegisterCommand = new RelayCommand(async _ => await RegisterAsync(), _ => CanRegister());
            ToggleModeCommand = new RelayCommand(_ => ToggleMode());
            GoogleLoginCommand = new RelayCommand(async _ => await GoogleLoginAsync());
            MicrosoftLoginCommand = new RelayCommand(async _ => await MicrosoftLoginAsync());
        }

        public event EventHandler LoginSuccessful;
        public event EventHandler RequestClose;

        #region Properties

        public bool IsLoginMode
        {
            get => _isLoginMode;
            set
            {
                _isLoginMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsRegisterMode));
                OnPropertyChanged(nameof(ModeToggleText));
                OnPropertyChanged(nameof(ModeToggleButtonText));
                ClearForm();
            }
        }

        public bool IsRegisterMode => !_isLoginMode;

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
                ClearError();
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged();
                ClearError();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
                ClearError();
            }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                _confirmPassword = value;
                OnPropertyChanged();
                ClearError();
            }
        }

        public string DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value;
                OnPropertyChanged();
            }
        }

        public bool RememberMe
        {
            get => _rememberMe;
            set
            {
                _rememberMe = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasError));
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(_errorMessage);

        public string ModeToggleText => _isLoginMode ? "Don't have an account?" : "Already have an account?";
        public string ModeToggleButtonText => _isLoginMode ? "Sign up" : "Sign in";

        #endregion

        #region Commands

        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }
        public ICommand ToggleModeCommand { get; }
        public ICommand GoogleLoginCommand { get; }
        public ICommand MicrosoftLoginCommand { get; }

        #endregion

        #region Methods

        private async Task LoginAsync()
        {
            if (!CanLogin() || IsLoading) return;

            try
            {
                IsLoading = true;
                ClearError();

                Debug.WriteLine($"LoginViewModel: Attempting login for user: {Username}");

                var loginRequest = new LoginRequest
                {
                    Username = Username.Trim(),
                    Password = Password,
                    RememberMe = RememberMe
                };

                var result = await _authenticationService.LoginAsync(loginRequest);

                if (result.IsSuccess)
                {
                    Debug.WriteLine("LoginViewModel: Login successful");
                    LoginSuccessful?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    Debug.WriteLine($"LoginViewModel: Login failed: {result.ErrorMessage}");
                    ErrorMessage = result.ErrorMessage;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoginViewModel: Login exception: {ex.Message}");
                ErrorMessage = "An unexpected error occurred during login. Please try again.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RegisterAsync()
        {
            if (!CanRegister() || IsLoading) return;

            try
            {
                IsLoading = true;
                ClearError();

                Debug.WriteLine($"LoginViewModel: Attempting registration for user: {Username}");

                var result = await _authenticationService.RegisterAsync(
                    Username.Trim(), 
                    Email.Trim(), 
                    Password, 
                    string.IsNullOrWhiteSpace(DisplayName) ? Username.Trim() : DisplayName.Trim());

                if (result.IsSuccess)
                {
                    Debug.WriteLine("LoginViewModel: Registration successful");
                    LoginSuccessful?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    Debug.WriteLine($"LoginViewModel: Registration failed: {result.ErrorMessage}");
                    ErrorMessage = result.ErrorMessage;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoginViewModel: Registration exception: {ex.Message}");
                ErrorMessage = "An unexpected error occurred during registration. Please try again.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task GoogleLoginAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                ClearError();

                Debug.WriteLine("LoginViewModel: Attempting Google OAuth authentication");

                var oAuthResult = await _oAuthService.AuthenticateGoogleAsync();
                
                if (oAuthResult.IsSuccess)
                {
                    var authResult = await _authenticationService.AuthenticateWithOAuthAsync(oAuthResult);
                    
                    if (authResult.IsSuccess)
                    {
                        Debug.WriteLine("LoginViewModel: Google OAuth authentication successful");
                        LoginSuccessful?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        Debug.WriteLine($"LoginViewModel: Google OAuth authentication failed: {authResult.ErrorMessage}");
                        ErrorMessage = authResult.ErrorMessage;
                    }
                }
                else
                {
                    Debug.WriteLine($"LoginViewModel: Google OAuth failed: {oAuthResult.ErrorMessage}");
                    ErrorMessage = oAuthResult.ErrorMessage;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoginViewModel: Google login exception: {ex.Message}");
                ErrorMessage = "An error occurred during Google authentication.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task MicrosoftLoginAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                ClearError();

                Debug.WriteLine("LoginViewModel: Attempting Microsoft OAuth authentication");

                var oAuthResult = await _oAuthService.AuthenticateMicrosoftAsync();
                
                if (oAuthResult.IsSuccess)
                {
                    var authResult = await _authenticationService.AuthenticateWithOAuthAsync(oAuthResult);
                    
                    if (authResult.IsSuccess)
                    {
                        Debug.WriteLine("LoginViewModel: Microsoft OAuth authentication successful");
                        LoginSuccessful?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        Debug.WriteLine($"LoginViewModel: Microsoft OAuth authentication failed: {authResult.ErrorMessage}");
                        ErrorMessage = authResult.ErrorMessage;
                    }
                }
                else
                {
                    Debug.WriteLine($"LoginViewModel: Microsoft OAuth failed: {oAuthResult.ErrorMessage}");
                    ErrorMessage = oAuthResult.ErrorMessage;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoginViewModel: Microsoft login exception: {ex.Message}");
                ErrorMessage = "An error occurred during Microsoft authentication.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ToggleMode()
        {
            IsLoginMode = !IsLoginMode;
        }

        private bool CanLogin()
        {
            return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }

        private bool CanRegister()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Email) || 
                string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword))
                return false;

            if (Password != ConfirmPassword)
                return false;

            if (!IsValidEmail(Email))
                return false;

            if (Password.Length < 6)
                return false;

            return true;
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        private void ClearForm()
        {
            Username = "";
            Email = "";
            Password = "";
            ConfirmPassword = "";
            DisplayName = "";
            ClearError();
        }

        private void ClearError()
        {
            ErrorMessage = "";
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}