using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Threading;
using System.Linq;
using DailyTaskManager.Core.Models;
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
    private readonly IDatabaseService _databaseService;
    private readonly DialogService _dialogService;
    private DispatcherTimer? _reminderTimer;
    private string _reminderSignature = "";
    private string _dismissedSignature = "";
    private DateTime? _reminderTargetDate;

    [ObservableProperty]
    private UserControl? _currentView;

    [ObservableProperty]
    private string _userDisplayName = "Welcome, User";

    [ObservableProperty]
    private string _userEmail = "";

    [ObservableProperty]
    private string _appVersion = "2.9.0";

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

    [ObservableProperty]
    private bool _isAnalyticsViewSelected = false;

    [ObservableProperty]
    private bool _isCalendarViewSelected = false;

    [ObservableProperty]
    private bool _isReminderVisible;

    [ObservableProperty]
    private string _reminderText = "";

    [ObservableProperty]
    private string _reminderActionText = "";

    [ObservableProperty]
    private bool _isReportsViewSelected = false;


    public MainWindowViewModel(
        IAuthenticationService authService,
        IOAuthService oauthService,
        NavigationService navigationService,
        ILogger<MainWindowViewModel> logger,
        IConfiguration configuration,
        ISettingsService settingsService,
        IDatabaseService databaseService,
        DialogService dialogService)
    {
        _databaseService = databaseService;
        _dialogService = dialogService;
        _settingsService = settingsService;
        _authService = authService;
        _oauthService = oauthService;
        _navigationService = navigationService;
        _logger = logger;
        _configuration = configuration;

        // Subscribe to authentication changes
        _authService.AuthenticationChanged += OnAuthenticationChanged;
        _databaseService.TasksChanged += OnTasksChanged;
        _settingsService.SettingChanged += OnSettingChanged;

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

            SetSelectedView(tasks: true);

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

    private void SetSelectedView(bool tasks = false, bool analytics = false, bool reports = false, bool settings = false, bool calendar = false)
    {
        IsCalendarViewSelected = calendar;
        IsTasksViewSelected = tasks;
        IsAnalyticsViewSelected = analytics;
        IsReportsViewSelected = reports;
        IsSettingsViewSelected = settings;
    }

    [RelayCommand]
    private async Task NavigateToCalendarAsync()
    {
        try
        {
            IsLoading = true;
            LoadingMessage = "Loading calendar...";

            (CurrentView?.DataContext as IDisposable)?.Dispose();
            CurrentView = await _navigationService.CreateViewAsync<ActivityCalendarView, ActivityCalendarViewModel>();
            SetSelectedView(calendar: true);

            _logger.LogInformation("Navigated to Calendar view");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to Calendar view");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToAnalyticsAsync()
    {
        try
        {
            IsLoading = true;
            LoadingMessage = "Loading analytics...";

            (CurrentView?.DataContext as IDisposable)?.Dispose();
            CurrentView = await _navigationService.CreateViewAsync<AnalyticsView, AnalyticsViewModel>();
            SetSelectedView(analytics: true);

            _logger.LogInformation("Navigated to Analytics view");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to Analytics view");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToReportsAsync()
    {
        try
        {
            IsLoading = true;
            LoadingMessage = "Loading reports...";

            (CurrentView?.DataContext as IDisposable)?.Dispose();
            CurrentView = await _navigationService.CreateViewAsync<ReportsView, ReportsViewModel>();
            SetSelectedView(reports: true);

            _logger.LogInformation("Navigated to Reports view");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to Reports view");
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

            SetSelectedView(settings: true);

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
            AppVersion = _configuration["AppSettings:Version"] ?? "2.9.0";

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
            StartReminder();

            _logger.LogInformation("Main window view initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing main window view");
        }
    }

    // ---- Missing-day reminder -------------------------------------------------------------------------

    private void StartReminder()
    {
        _reminderTimer?.Stop();
        _reminderTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(10) };
        _reminderTimer.Tick += (_, _) => _ = RefreshReminderAsync();
        _reminderTimer.Start();
        _ = RefreshReminderAsync();
    }

    private void OnTasksChanged(object? sender, EventArgs e) => PostReminderRefresh();

    private void OnSettingChanged(object? sender, (string Key, object? Value) e)
    {
        if (e.Key.StartsWith(ReminderSettings.KeyPrefix, StringComparison.Ordinal)) PostReminderRefresh();
    }

    private void PostReminderRefresh()
    {
        try
        {
            Dispatcher.UIThread.Post(() => _ = RefreshReminderAsync());
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not schedule a reminder refresh");
        }
    }

    public async Task RefreshReminderAsync()
    {
        try
        {
            if (!_authService.IsAuthenticated)
            {
                IsReminderVisible = false;
                return;
            }

            var enabledText = await _settingsService.GetSettingAsync<string>(ReminderSettings.EnabledKey, null);
            var enabled = enabledText == null ? ReminderSettings.DefaultEnabled : string.Equals(enabledText, "true", StringComparison.OrdinalIgnoreCase);
            if (!enabled)
            {
                IsReminderVisible = false;
                return;
            }

            var hourText = await _settingsService.GetSettingAsync<string>(ReminderSettings.HourKey, null);
            var hour = int.TryParse(hourText, out var h) && h >= 0 && h <= 23 ? h : ReminderSettings.DefaultHour;

            var first = await _databaseService.GetFirstEntryDateAsync();
            if (first == null)
            {
                IsReminderVisible = false;
                return;
            }

            var today = DateTime.Today;
            var from = first.Value.Date > today.AddDays(-(MissingDays.LookbackDays - 1)) ? first.Value.Date : today.AddDays(-(MissingDays.LookbackDays - 1));
            var days = await _databaseService.GetDailyHoursAsync(new Core.Models.TaskQuery(null, from, today));
            var logged = days.Where(d => d.Hours > 0).Select(d => d.Date.Date).ToHashSet();

            var missing = MissingDays.Find(first, today, DateTime.Now, logged, hour);
            _reminderSignature = string.Join(",", missing.Select(d => d.ToString("yyyyMMdd")));

            if (missing.Count == 0 || _reminderSignature == _dismissedSignature)
            {
                IsReminderVisible = false;
                return;
            }

            _reminderTargetDate = missing[0];
            ReminderText = MissingDays.Describe(missing, today);
            ReminderActionText = $"Add {missing[0]:ddd d MMM}";
            IsReminderVisible = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not check for missing days");
        }
    }

    [RelayCommand]
    private void DismissReminder()
    {
        _dismissedSignature = _reminderSignature;
        IsReminderVisible = false;
    }

    [RelayCommand]
    private async Task AddMissingEntryAsync()
    {
        var date = _reminderTargetDate;
        if (date == null) return;

        try
        {
            var task = await _dialogService.ShowAddTaskDialogAsync(null, date);
            if (task != null)
            {
                await _databaseService.AddTaskAsync(task);
                // TasksChanged refreshes the banner
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding a missing entry");
            await _dialogService.ShowErrorDialogAsync("Error", "Failed to add the entry. Please try again.");
        }
    }

    public void Dispose()
    {
        _reminderTimer?.Stop();
        _databaseService.TasksChanged -= OnTasksChanged;
        _settingsService.SettingChanged -= OnSettingChanged;
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