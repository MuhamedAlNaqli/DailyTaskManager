using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Avalonia.Services;
using DailyTaskManager.Avalonia.Views;

namespace DailyTaskManager.Avalonia.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IAuthenticationService _authService;
    private readonly IOAuthService _oauthService;
    private readonly NavigationService _navigationService;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly IConfiguration _configuration;

    [ObservableProperty]
    private UserControl? _currentView;

    [ObservableProperty]
    private string _userDisplayName = "Welcome, User";

    [ObservableProperty]
    private string _userEmail = "";

    [ObservableProperty]
    private string _appVersion = "2.0.0";

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string _loadingMessage = "Loading...";

    [ObservableProperty]
    private bool _isDarkTheme = false;

    [ObservableProperty]
    private bool _isTasksViewSelected = true;

    [ObservableProperty]
    private bool _isSettingsViewSelected = false;

    [ObservableProperty]
    private bool _isSpeechViewSelected = false;

    [ObservableProperty]
    private bool _speechRecognitionAvailable = false;

    public MainWindowViewModel(
        IAuthenticationService authService,
        IOAuthService oauthService,
        NavigationService navigationService,
        ILogger<MainWindowViewModel> logger,
        IConfiguration configuration)
    {
        _authService = authService;
        _oauthService = oauthService;
        _navigationService = navigationService;
        _logger = logger;
        _configuration = configuration;

        // Subscribe to authentication changes
        _authService.AuthenticationChanged += OnAuthenticationChanged;
        
        // Initialize non-UI properties only
        InitializePropertiesOnly();
    }

    public string ThemeIcon => IsDarkTheme ? "WeatherNight" : "WeatherSunny";
    public string ThemeText => IsDarkTheme ? "Dark Mode" : "Light Mode";

    [RelayCommand]
    private async Task NavigateToTasksAsync()
    {
        try
        {
            IsLoading = true;
            LoadingMessage = "Loading tasks...";

            CurrentView = await _navigationService.CreateViewAsync<DailyTasksView, DailyTasksViewModel>();
            
            IsTasksViewSelected = true;
            IsSettingsViewSelected = false;
            IsSpeechViewSelected = false;

            _logger.LogInformation("Navigated to Tasks view");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to Tasks view");
            // TODO: Show error dialog
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToSettingsAsync()
    {
        try
        {
            IsLoading = true;
            LoadingMessage = "Loading settings...";

            CurrentView = await _navigationService.CreateViewAsync<SettingsView, SettingsViewModel>();
            
            IsTasksViewSelected = false;
            IsSettingsViewSelected = true;
            IsSpeechViewSelected = false;

            _logger.LogInformation("Navigated to Settings view");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to Settings view");
            // TODO: Show error dialog
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToSpeechAsync()
    {
        try
        {
            if (!SpeechRecognitionAvailable)
                return;

            IsLoading = true;
            LoadingMessage = "Loading speech recognition...";

            CurrentView = await _navigationService.CreateViewAsync<SpeechToTextView, SpeechToTextViewModel>();
            
            IsTasksViewSelected = false;
            IsSettingsViewSelected = false;
            IsSpeechViewSelected = true;

            _logger.LogInformation("Navigated to Speech view");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to Speech view");
            // TODO: Show error dialog
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ToggleThemeAsync()
    {
        try
        {
            IsDarkTheme = !IsDarkTheme;
            
            // Apply theme change
            if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var theme = IsDarkTheme ? "Dark" : "Light";
                App.Current.RequestedThemeVariant = IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
                
                _logger.LogInformation("Theme changed to {Theme}", theme);
                
                // Save theme preference
                // TODO: Save to settings
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing theme");
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        try
        {
            IsLoading = true;
            LoadingMessage = "Logging out...";

            await _authService.LogoutAsync();
            
            // Navigate to login window
            // TODO: Show login window
            _logger.LogInformation("User logged out successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void InitializePropertiesOnly()
    {
        try
        {
            // Set app version from configuration
            AppVersion = _configuration["AppSettings:Version"] ?? "2.0.0";

            // Check speech recognition availability
            var speechService = App.GetOptionalService<ISpeechRecognitionService>();
            SpeechRecognitionAvailable = speechService?.IsSupported ?? false;

            // Update user info if authenticated
            UpdateUserInfo();

            _logger.LogInformation("Main window properties initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing main window properties");
        }
    }

    public async Task InitializeViewAsync()
    {
        try
        {
            // Navigate to default view - this must be called from UI thread
            await NavigateToTasksAsync();

            _logger.LogInformation("Main window view initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing main window view");
        }
    }

    private void OnAuthenticationChanged(object? sender, Core.Models.AuthResult e)
    {
        UpdateUserInfo();
    }

    private void UpdateUserInfo()
    {
        if (_authService.IsAuthenticated && _authService.CurrentUser != null)
        {
            var user = _authService.CurrentUser;
            UserDisplayName = $"Welcome, {user.GetDisplayName()}";
            UserEmail = user.Email;
        }
        else
        {
            UserDisplayName = "Welcome, Guest";
            UserEmail = "";
        }
    }

    [RelayCommand]
    private async Task TestGoogleLoginAsync()
    {
        try
        {
            IsLoading = true;
            LoadingMessage = "Testing Google OAuth...";

            _logger.LogInformation("Starting Google OAuth test");

            // Test Google OAuth
            var oauthResult = await _oauthService.AuthenticateGoogleAsync();

            if (!oauthResult.IsSuccess)
            {
                _logger.LogError("Google OAuth test failed: {Error}", oauthResult.ErrorMessage);
                // TODO: Show error dialog
                return;
            }

            // Use the OAuth result to login through authentication service
            var authResult = await _authService.LoginWithOAuthAsync(oauthResult);

            if (!authResult.IsSuccess)
            {
                _logger.LogError("OAuth login test failed: {Error}", authResult.ErrorMessage);
                // TODO: Show error dialog
            }
            else
            {
                _logger.LogInformation("Google OAuth test successful for user: {Email}", oauthResult.Email);
                // The authentication should automatically update the UI
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google OAuth test");
        }
        finally
        {
            IsLoading = false;
        }
    }
}