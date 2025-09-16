using System;
using Avalonia.Controls;
using Avalonia.Threading;
using DailyTaskManager.Avalonia.ViewModels;
using Microsoft.Extensions.Logging;

namespace DailyTaskManager.Avalonia.Views;

public partial class DailyTasksView : UserControl
{
    private readonly ILogger<DailyTasksView>? _logger;
    private DailyTasksViewModel? _currentViewModel;

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

    private void OnViewLoaded(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Set up DatePicker events after the view is fully loaded
        SetupDatePickerEvents();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is DailyTasksViewModel viewModel)
        {
            Console.WriteLine("🔌 DailyTasksView: DataContext changed to DailyTasksViewModel");
            _currentViewModel = viewModel;

            // Defer setup until after UI is loaded
            Dispatcher.UIThread.Post(() => SetupDatePickerEvents(), DispatcherPriority.Background);
        }
        else
        {
            Console.WriteLine("⚠️ DailyTasksView: DataContext is not DailyTasksViewModel");
            _currentViewModel = null;
        }
    }

    private void SetupDatePickerEvents()
    {
        if (_currentViewModel == null) return;

        Console.WriteLine("🔌 DailyTasksView: Setting up Search button events...");

        try
        {
            // Only wire up the search button - no DatePicker events to avoid circular loops
            var searchButton = this.FindControl<Button>("SearchButton");

            if (searchButton != null)
            {
                Console.WriteLine("✅ DailyTasksView: Found SearchButton, attaching event handler");

                // Remove any existing handlers to avoid duplicates
                searchButton.Click -= OnSearchButtonClicked;
                searchButton.Click += OnSearchButtonClicked;
            }
            else
            {
                Console.WriteLine("❌ DailyTasksView: SearchButton not found!");
            }

            Console.WriteLine("🎯 DailyTasksView: Event setup completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error setting up events: {ex.Message}");
        }
    }


    private void OnSearchButtonClicked(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_currentViewModel == null)
        {
            Console.WriteLine("❌ DailyTasksView: Search button clicked but ViewModel is null!");
            return;
        }

        Console.WriteLine("🔍 DailyTasksView: Search button clicked!");

        try
        {
            // Get current DatePicker values
            var dateFromPicker = this.FindControl<DatePicker>("DateFromPicker");
            var dateToPicker = this.FindControl<DatePicker>("DateToPicker");

            Console.WriteLine($"🔍 Found DatePickers - From: {dateFromPicker != null}, To: {dateToPicker != null}");

            var dateFromValue = dateFromPicker?.SelectedDate?.DateTime;
            var dateToValue = dateToPicker?.SelectedDate?.DateTime;

            Console.WriteLine($"🔍 Current DatePicker values: DateFrom = {dateFromValue}, DateTo = {dateToValue}");
            Console.WriteLine($"🔍 Current ViewModel values: DateFrom = {_currentViewModel.DateFrom}, DateTo = {_currentViewModel.DateTo}");

            // Get current task count before filtering
            var taskCountBefore = _currentViewModel.FilteredTasks?.Count ?? 0;
            Console.WriteLine($"📊 Tasks before filtering: {taskCountBefore}");

            // Update ViewModel properties only if they're different to avoid unnecessary binding updates
            bool valuesChanged = false;

            if (dateFromValue != _currentViewModel.DateFrom)
            {
                Console.WriteLine($"🔄 Updating DateFrom: {_currentViewModel.DateFrom} → {dateFromValue}");
                _currentViewModel.DateFrom = dateFromValue;
                valuesChanged = true;
            }

            if (dateToValue != _currentViewModel.DateTo)
            {
                Console.WriteLine($"🔄 Updating DateTo: {_currentViewModel.DateTo} → {dateToValue}");
                _currentViewModel.DateTo = dateToValue;
                valuesChanged = true;
            }

            if (!valuesChanged)
            {
                Console.WriteLine("🔄 No date values changed, but still applying filters...");
            }

            Console.WriteLine($"🎯 ViewModel updated - DateFrom: {_currentViewModel.DateFrom}, DateTo: {_currentViewModel.DateTo}");

            // Always apply filters to ensure filtering happens
            Console.WriteLine("🚀 Applying filters...");
            _currentViewModel.ApplyFilters();

            // Check task count after filtering
            var taskCountAfter = _currentViewModel.FilteredTasks?.Count ?? 0;
            Console.WriteLine($"📊 Tasks after filtering: {taskCountAfter}");

            if (taskCountBefore != taskCountAfter)
            {
                Console.WriteLine($"✅ Filtering successful! Tasks changed from {taskCountBefore} to {taskCountAfter}");
            }
            else
            {
                Console.WriteLine($"⚠️ Task count unchanged. Current filter criteria may not match any tasks or all tasks already match.");
            }

            Console.WriteLine("✅ Search completed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in search button click: {ex.Message}");
            Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
        }
    }
}