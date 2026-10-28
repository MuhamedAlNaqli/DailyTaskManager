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

public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly IAuthenticationService _authService;
    private readonly IOAuthService _oauthService;
    private readonly NavigationService _navigationService;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly IConfiguration _configuration;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private UserControl? _currentView;

    [ObservableProperty]
    private string _userDisplayName = "Welcome, User";

    [ObservableProperty]
    private string _userEmail = "";

    [ObservableProperty]
    private string _appVersion = "2.7.0";

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string _loadingMessage = "Loading...";

    private bool _isDarkTheme = false;
    
    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (SetProperty(ref _isDarkTheme, value))
            {
                _logger.LogInformation("IsDarkTheme property changed to: {IsDarkTheme}", value);
            }
        }
    }

    [ObservableProperty]
    private bool _isTasksViewSelected = true;

    [ObservableProperty]
    private bool _isSettingsViewSelected = false;


    public MainWindowViewModel(
        IAuthenticationService authService,
        IOAuthService oauthService,
        NavigationService navigationService,
        ILogger<MainWindowViewModel> logger,
        IConfiguration configuration,
        ISettingsService settingsService)
    {
        _settingsService = settingsService;
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

            (CurrentView?.DataContext as IDisposable)?.Dispose();
            CurrentView = await _navigationService.CreateViewAsync<DailyTasksView, DailyTasksViewModel>();
            
            IsTasksViewSelected = true;
            IsSettingsViewSelected = false;

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

            (CurrentView?.DataContext as IDisposable)?.Dispose();
            CurrentView = await _navigationService.CreateViewAsync<SettingsView, SettingsViewModel>();
            
            IsTasksViewSelected = false;
            IsSettingsViewSelected = true;

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
    private async Task ToggleThemeAsync()
    {
        try
        {
            IsDarkTheme = !IsDarkTheme;
            
            // Apply theme change
            if (App.Current != null)
            {
                var theme = IsDarkTheme ? "Dark" : "Light";
                App.Current.RequestedThemeVariant = IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
                
                _logger.LogInformation("Theme changed to {Theme}", theme);
                
                // Trigger property change notifications for theme-related properties
                OnPropertyChanged(nameof(ThemeIcon));
                OnPropertyChanged(nameof(ThemeText));
                
                // Force UI refresh by triggering property change for IsDarkTheme
                OnPropertyChanged(nameof(IsDarkTheme));
                
                // Save theme preference to settings
                await _settingsService.SetSettingAsync("Theme", theme);
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

            // Detach view-models from auth events so they don't reload on the next login
            Dispose();

            await _authService.LogoutAsync();
            
            // Clear loading state before showing login window
            IsLoading = false;
            
            // Navigate to login window
            ShowLoginWindow();
            
            _logger.LogInformation("User logged out successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            IsLoading = false;
        }
    }

    private void ShowLoginWindow()
    {
        try
        {
            // Get the current main window and close it
            if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var currentMainWindow = desktop.MainWindow;
                
                // Create a fresh login window with proper DI
                var app = App.Current as App;
                if (app != null)
                {
                    var loginWindow = app.CreateLoginWindow();
                    loginWindow.Show();
                    
                    // Set the new login window as the main window
                    desktop.MainWindow = loginWindow;
                    
                    // Close the current main window
                    currentMainWindow?.Close();
                    
                    _logger.LogInformation("Redirected to login window with fresh ViewModel");
                }
                else
                {
                    _logger.LogError("Could not cast App.Current to App type");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing login window");
        }
    }

    private void InitializePropertiesOnly()
    {
        try
        {
            // Set app version from configuration
            AppVersion = _configuration["AppSettings:Version"] ?? "2.7.0";

            // Initialize theme state based on current application theme
            InitializeThemeState();

            // Update user info if authenticated
            UpdateUserInfo();

            _logger.LogInformation("Main window properties initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing main window properties");
        }
    }

    private void InitializeThemeState()
    {
        try
        {
            if (App.Current != null)
            {
                // Sync ViewModel state with actual application theme
                IsDarkTheme = App.Current.RequestedThemeVariant == ThemeVariant.Dark;
                
                _logger.LogDebug("Theme state initialized: IsDarkTheme = {IsDarkTheme}", IsDarkTheme);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing theme state");
        }
    }

    public async Task InitializeViewAsync()
    {
        try
        {
            await ApplySavedThemeAsync();

            // Navigate to default view - this must be called from UI thread
            await NavigateToTasksAsync();

            _logger.LogInformation("Main window view initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing main window view");
        }
    }

    public void Dispose()
    {
        _authService.AuthenticationChanged -= OnAuthenticationChanged;
        (CurrentView?.DataContext as IDisposable)?.Dispose();
    }

    private async Task ApplySavedThemeAsync()
    {
        try
        {
            var theme = await _settingsService.GetSettingAsync<string>("Theme", "Light");
            if (App.Current != null && !string.IsNullOrEmpty(theme))
            {
                IsDarkTheme = string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase);
                App.Current.RequestedThemeVariant = IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
                OnPropertyChanged(nameof(ThemeIcon));
                OnPropertyChanged(nameof(ThemeText));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not apply saved theme");
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

}