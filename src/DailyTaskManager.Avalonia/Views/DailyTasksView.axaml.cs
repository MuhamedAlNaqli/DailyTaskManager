using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using DailyTaskManager.Avalonia.ViewModels;
using Microsoft.Extensions.Logging;

namespace DailyTaskManager.Avalonia.Views;

public partial class DailyTasksView : UserControl
{
    private readonly ILogger<DailyTasksView>? _logger;
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
            Console.WriteLine("🔌 DailyTasksView: DataContext changed to DailyTasksViewModel");
            _currentViewModel = viewModel;

            // Defer setup until after UI is loaded
            Dispatcher.UIThread.Post(() => SetupCalendarEvents(), DispatcherPriority.Background);
        }
        else
        {
            Console.WriteLine("⚠️ DailyTasksView: DataContext is not DailyTasksViewModel");
            _currentViewModel = null;
        }
    }

    private void SetupCalendarEvents()
    {
        if (_currentViewModel == null) return;

        Console.WriteLine("🔌 DailyTasksView: Setting up Calendar and button events...");

        try
        {
            // Get references to controls
            var searchButton = this.FindControl<Button>("SearchButton");
            _dateRangeButton = this.FindControl<Button>("DateRangeButton");
            _dateRangeCalendar = this.FindControl<Calendar>("DateRangeCalendar");
            _dateRangeText = this.FindControl<TextBlock>("DateRangeText");
            var applyButton = this.FindControl<Button>("ApplyDateRangeButton");
            var clearButton = this.FindControl<Button>("ClearDateRangeButton");

            // Set up search button
            if (searchButton != null)
            {
                searchButton.Click -= OnSearchButtonClicked;
                searchButton.Click += OnSearchButtonClicked;
                Console.WriteLine("✅ Search button event attached");
            }

            // Set up calendar
            if (_dateRangeCalendar != null)
            {
                _dateRangeCalendar.SelectedDatesChanged -= OnCalendarSelectedDatesChanged;
                _dateRangeCalendar.SelectedDatesChanged += OnCalendarSelectedDatesChanged;
                Console.WriteLine("✅ Calendar event attached");
            }

            // Set up apply button
            if (applyButton != null)
            {
                applyButton.Click -= OnApplyDateRangeClicked;
                applyButton.Click += OnApplyDateRangeClicked;
                Console.WriteLine("✅ Apply button event attached");
            }

            // Set up clear button
            if (clearButton != null)
            {
                clearButton.Click -= OnClearDateRangeClicked;
                clearButton.Click += OnClearDateRangeClicked;
                Console.WriteLine("✅ Clear button event attached");
            }

            // Get flyout reference
            if (_dateRangeButton != null)
            {
                _dateRangeFlyout = _dateRangeButton.Flyout;
            }

            Console.WriteLine("🎯 DailyTasksView: Event setup completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error setting up events: {ex.Message}");
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
                Console.WriteLine($"📅 Date range selected: {startDate:d} to {endDate:d}");
            }
            else if (selectedDates.Count == 1)
            {
                var startDate = selectedDates.First();
                _dateRangeText.Text = $"{startDate:MMM dd} → ...";
                Console.WriteLine($"📅 Start date selected: {startDate:d}");
            }
            else
            {
                _dateRangeText.Text = "Select Date Range";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in calendar selection: {ex.Message}");
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

                Console.WriteLine($"✅ Applied date range: {startDate:d} to {endDate:d}");

                // Close the flyout
                _dateRangeFlyout?.Hide();

                // Apply filters
                _currentViewModel.ApplyFilters();
            }
            else
            {
                Console.WriteLine("⚠️ Please select both start and end dates");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error applying date range: {ex.Message}");
        }
    }

    private void OnClearDateRangeClicked(object? sender, RoutedEventArgs e)
    {
        if (_dateRangeCalendar == null || _dateRangeText == null) return;

        try
        {
            _dateRangeCalendar.SelectedDates.Clear();
            _dateRangeText.Text = "Select Date Range";
            Console.WriteLine("✅ Date range cleared");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error clearing date range: {ex.Message}");
        }
    }

    private void OnSearchButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (_currentViewModel == null)
        {
            Console.WriteLine("❌ DailyTasksView: Search button clicked but ViewModel is null!");
            return;
        }

        Console.WriteLine("🔍 DailyTasksView: Search button clicked!");

        try
        {
            // Apply filters with current ViewModel date values
            Console.WriteLine($"🔍 Current ViewModel values: DateFrom = {_currentViewModel.DateFrom}, DateTo = {_currentViewModel.DateTo}");
            _currentViewModel.ApplyFilters();
            Console.WriteLine("✅ Search completed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in search button click: {ex.Message}");
        }
    }
}