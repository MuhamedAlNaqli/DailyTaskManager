using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using DailyTaskManager.Avalonia.ViewModels;

namespace DailyTaskManager.Avalonia.Views;

public partial class DailyTasksView : UserControl
{
    private DailyTasksViewModel? _currentViewModel;
    private Calendar? _dateRangeCalendar;
    private TextBlock? _dateRangeText;
    private Button? _dateRangeButton;
    private FlyoutBase? _dateRangeFlyout;

    public DailyTasksView()
    {
        InitializeComponent();
        AttachEventHandlers();
    }

    private void AttachEventHandlers()
    {
        this.DataContextChanged += OnDataContextChanged;
        this.Loaded += OnViewLoaded;
    }

    private void OnViewLoaded(object? sender, RoutedEventArgs e)
    {
        // Set up calendar and button events after the view is fully loaded
        SetupCalendarEvents();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is DailyTasksViewModel viewModel)
        {
            Serilog.Log.Debug("DailyTasksView: DataContext changed to DailyTasksViewModel");
            _currentViewModel = viewModel;

            // Defer setup until after UI is loaded
            Dispatcher.UIThread.Post(() => SetupCalendarEvents(), DispatcherPriority.Background);
        }
        else
        {
            Serilog.Log.Debug("DailyTasksView: DataContext is not DailyTasksViewModel");
            _currentViewModel = null;
        }
    }

    private void SetupCalendarEvents()
    {
        if (_currentViewModel == null) return;

        Serilog.Log.Debug("DailyTasksView: Setting up Calendar and button events...");

        try
        {
            // Get references to controls
            _dateRangeButton = this.FindControl<Button>("DateRangeButton");
            _dateRangeCalendar = this.FindControl<Calendar>("DateRangeCalendar");
            _dateRangeText = this.FindControl<TextBlock>("DateRangeText");
            var applyButton = this.FindControl<Button>("ApplyDateRangeButton");
            var clearAllButton = this.FindControl<Button>("ClearAllFiltersButton");

            // Set up calendar
            if (_dateRangeCalendar != null)
            {
                _dateRangeCalendar.SelectedDatesChanged -= OnCalendarSelectedDatesChanged;
                _dateRangeCalendar.SelectedDatesChanged += OnCalendarSelectedDatesChanged;
                Serilog.Log.Debug("Calendar event attached");
            }

            // Set up apply button
            if (applyButton != null)
            {
                applyButton.Click -= OnApplyDateRangeClicked;
                applyButton.Click += OnApplyDateRangeClicked;
                Serilog.Log.Debug("Apply button event attached");
            }

            // Set up clear all filters button
            if (clearAllButton != null)
            {
                clearAllButton.Click -= OnClearAllFiltersClicked;
                clearAllButton.Click += OnClearAllFiltersClicked;
                Serilog.Log.Debug("Clear all filters button event attached");
            }

            // Get flyout reference
            if (_dateRangeButton != null)
            {
                _dateRangeFlyout = _dateRangeButton.Flyout;
            }

            Serilog.Log.Debug("DailyTasksView: Event setup completed");
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error setting up events");
        }
    }

    private void OnCalendarSelectedDatesChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_dateRangeCalendar == null || _dateRangeText == null) return;

        try
        {
            var selectedDates = _dateRangeCalendar.SelectedDates.OrderBy(d => d).ToList();

            if (selectedDates.Count >= 2)
            {
                var startDate = selectedDates.First();
                var endDate = selectedDates.Last();
                _dateRangeText.Text = $"{startDate:MMM dd} → {endDate:MMM dd}";
                Serilog.Log.Debug($"Date range selected: {startDate:d} to {endDate:d}");
            }
            else if (selectedDates.Count == 1)
            {
                var startDate = selectedDates.First();
                _dateRangeText.Text = $"{startDate:MMM dd}";
                Serilog.Log.Debug($"Start date selected: {startDate:d}");
            }
            else
            {
                _dateRangeText.Text = "Select Date Range";
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error in calendar selection");
        }
    }

    private void OnApplyDateRangeClicked(object? sender, RoutedEventArgs e)
    {
        if (_currentViewModel == null || _dateRangeCalendar == null) return;

        try
        {
            var selectedDates = _dateRangeCalendar.SelectedDates.OrderBy(d => d).ToList();

            if (selectedDates.Count >= 2)
            {
                var startDate = selectedDates.First();
                var endDate = selectedDates.Last();

                _currentViewModel.DateFrom = startDate;
                _currentViewModel.DateTo = endDate;

                Serilog.Log.Debug($"Applied date range: {startDate:d} to {endDate:d}");

                // Close the flyout
                _dateRangeFlyout?.Hide();

                // Apply filters
                _currentViewModel.ApplyFilters();
            }
            else if (selectedDates.Count == 1)
            {
                _currentViewModel.DateFrom = selectedDates[0];
                _currentViewModel.DateTo = selectedDates[0];
                _dateRangeFlyout?.Hide();
                _currentViewModel.ApplyFilters();
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error applying date range");
        }
    }

    private void OnClearAllFiltersClicked(object? sender, RoutedEventArgs e)
    {
        if (_currentViewModel == null || _dateRangeCalendar == null || _dateRangeText == null) return;

        try
        {
            // Clear date range calendar
            _dateRangeCalendar.SelectedDates.Clear();
            _dateRangeText.Text = "Select Date Range";

            // Clear date range in ViewModel
            _currentViewModel.DateFrom = null;
            _currentViewModel.DateTo = null;

            // Clear search text
            _currentViewModel.SearchText = string.Empty;

            Serilog.Log.Debug("All filters cleared (date range and search text)");

            // Apply filters to show all tasks
            _currentViewModel.ApplyFilters();

            // Close the flyout
            _dateRangeFlyout?.Hide();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error clearing all filters");
        }
    }
}