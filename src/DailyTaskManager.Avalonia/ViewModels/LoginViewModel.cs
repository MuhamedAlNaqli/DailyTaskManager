using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Avalonia.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthenticationService _authService;
    private readonly IOAuthService _oauthService;
    private readonly ILogger<LoginViewModel> _logger;

    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private bool _rememberMe = false;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string? _errorMessage;

    public LoginViewModel(IAuthenticationService authService, IOAuthService oauthService, ILogger<LoginViewModel> logger)
    {
        _authService = authService;
        _oauthService = oauthService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter username and password";
                return;
            }

            var request = new LoginRequest
            {
                Username = Username,
                Password = Password,
                RememberMe = RememberMe
            };

            var result = await _authService.LoginAsync(request);
            
            if (!result.IsSuccess)
            {
                ErrorMessage = result.ErrorMessage;
                _logger.LogWarning("Login failed for user {Username}: {Error}", Username, result.ErrorMessage);
            }
            else
            {
                _logger.LogInformation("User {Username} logged in successfully", Username);
                await NavigateToMainWindowAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            ErrorMessage = "An unexpected error occurred. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter username and password to register";
                return;
            }

            var result = await _authService.RegisterAsync(Username, $"{Username}@example.com", Password);
            
            if (!result.IsSuccess)
            {
                ErrorMessage = result.ErrorMessage;
                _logger.LogWarning("Registration failed for user {Username}: {Error}", Username, result.ErrorMessage);
            }
            else
            {
                _logger.LogInformation("User {Username} registered successfully", Username);
                ErrorMessage = "SUCCESS: Registration successful! You can now login.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            ErrorMessage = "An unexpected error occurred during registration. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoginWithGoogleAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            _logger.LogInformation("Starting Google OAuth authentication");

            // Authenticate with Google OAuth
            var oauthResult = await _oauthService.AuthenticateGoogleAsync();

            if (!oauthResult.IsSuccess)
            {
                ErrorMessage = oauthResult.ErrorMessage;
                _logger.LogWarning("Google OAuth failed: {Error}", oauthResult.ErrorMessage);
                return;
            }

            // Use the OAuth result to login through authentication service
            var authResult = await _authService.LoginWithOAuthAsync(oauthResult);

            if (!authResult.IsSuccess)
            {
                ErrorMessage = authResult.ErrorMessage;
                _logger.LogWarning("OAuth login failed: {Error}", authResult.ErrorMessage);
            }
            else
            {
                _logger.LogInformation("User {Email} logged in successfully via Google OAuth", oauthResult.Email);
                await NavigateToMainWindowAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google OAuth login");
            ErrorMessage = "Google login failed. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task NavigateToMainWindowAsync()
    {
        try
        {
            // Get the current login window
            if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var currentWindow = desktop.MainWindow;
                
                // Create and show the main window
                var app = App.Current as App;
                var mainWindow = app?.CreateMainWindow();
                
                if (mainWindow != null)
                {
                    // Initialize the main window view
                    if (mainWindow.DataContext is MainWindowViewModel mainViewModel)
                    {
                        await mainViewModel.InitializeViewAsync();
                    }
                    
                    desktop.MainWindow = mainWindow;
                    mainWindow.Show();
                    
                    // Close the login window
                    currentWindow?.Close();
                    
                    _logger.LogInformation("Successfully navigated to main window");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to main window");
            ErrorMessage = "Failed to open main window. Please try again.";
        }
    }
}