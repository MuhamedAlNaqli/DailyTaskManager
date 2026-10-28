using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Interactivity;
using Avalonia.Threading;
using DailyTaskManager.Avalonia.ViewModels;
using DailyTaskManager.Core.Models;

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

    protected override void OnAttachedToVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // Listen on the window so the shortcuts work wherever the focus is (dialogs are separate windows)
        (TopLevel.GetTopLevel(this))?.AddHandler(KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Bubble | RoutingStrategies.Tunnel);
    }

    protected override void OnDetachedFromVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        (TopLevel.GetTopLevel(this))?.RemoveHandler(KeyDownEvent, OnWindowKeyDown);
        base.OnDetachedFromVisualTree(e);
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (_currentViewModel == null || !IsEffectivelyVisible || !e.KeyModifiers.HasFlag(KeyModifiers.Control)) return;

        switch (e.Key)
        {
            case Key.N:
                _currentViewModel.AddNewTaskCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F:
                var box = this.FindControl<TextBox>("SearchBox");
                box?.Focus();
                box?.SelectAll();
                e.Handled = true;
                break;
            case Key.Z when _currentViewModel.IsUndoVisible && TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is not TextBox:
                _currentViewModel.UndoDeleteCommand.Execute(null);
                e.Handled = true;
                break;
        }
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
            if (_currentViewModel != null) _currentViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _currentViewModel = viewModel;
            viewModel.PropertyChanged += OnViewModelPropertyChanged;

            // Defer setup until after UI is loaded
            Dispatcher.UIThread.Post(() => SetupCalendarEvents(), DispatcherPriority.Background);
        }
        else
        {
            Serilog.Log.Debug("DailyTasksView: DataContext is not DailyTasksViewModel");
            _currentViewModel = null;
        }
    }

    private void OnDescriptionDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not TextBlock display || display.DataContext is not DailyTask task || _currentViewModel == null) return;

        var viewModel = _currentViewModel;
        BeginInlineEdit(display, task.Description, multiline: true,
            text => viewModel.UpdateInlineAsync(task, text, null));
        e.Handled = true;
    }

    private void OnHoursDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not TextBlock display || display.DataContext is not DailyTask task || _currentViewModel == null) return;

        var viewModel = _currentViewModel;
        BeginInlineEdit(display, task.Hours.ToString("0.##", System.Globalization.CultureInfo.CurrentCulture), multiline: false, text =>
        {
            var normalized = text.Trim().Replace(',', '.');
            if (!decimal.TryParse(normalized, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var hours))
            {
                return viewModel.UpdateInlineAsync(task, null, 0); // rejected with a message
            }

            return viewModel.UpdateInlineAsync(task, null, hours);
        });
        e.Handled = true;
    }

    /// <summary>
    /// Replaces a text block with an editor: Enter or Ctrl+Enter saves (plain Enter is a new line in multi-line text),
    /// Esc cancels, leaving the field saves
    /// </summary>
    private static void BeginInlineEdit(TextBlock display, string initial, bool multiline, Func<string, System.Threading.Tasks.Task<bool>> commit)
    {
        var box = new TextBox
        {
            Text = initial,
            AcceptsReturn = multiline,
            TextWrapping = multiline ? global::Avalonia.Media.TextWrapping.Wrap : global::Avalonia.Media.TextWrapping.NoWrap,
            MinWidth = multiline ? 240 : 64,
            MinHeight = 0,
            Padding = new global::Avalonia.Thickness(6, 4),
            FontSize = display.FontSize,
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center
        };
        box.Classes.Add("filled");
        box.Classes.Add("inline-edit");
        ToolTip.SetTip(box, multiline ? "Ctrl+Enter or click away to save, Esc to cancel" : "Enter or click away to save, Esc to cancel");

        var panel = display.Parent as Panel;
        var border = display.Parent as Border;
        if (panel != null)
        {
            display.IsVisible = false;
            panel.Children.Insert(panel.Children.IndexOf(display) + 1, box);
        }
        else if (border != null)
        {
            border.Child = box;
        }
        else
        {
            return;
        }

        var finished = false;

        void Restore()
        {
            if (panel != null)
            {
                panel.Children.Remove(box);
                display.IsVisible = true;
            }
            else if (border != null && ReferenceEquals(border.Child, box))
            {
                border.Child = display;
            }
        }

        async System.Threading.Tasks.Task Finish(bool save)
        {
            if (finished) return;
            finished = true;

            try
            {
                var text = box.Text ?? "";
                if (save && text != initial)
                {
                    // The list reloads on success and replaces this row; on a rejected value the original text comes back
                    await commit(text);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Inline edit failed");
            }
            finally
            {
                Restore();
            }
        }

        // Tunnelling: the text box turns Enter (with or without Ctrl) into a new line before a normal handler would see it
        box.AddHandler(KeyDownEvent, (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                _ = Finish(false);
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && (!multiline || e.KeyModifiers.HasFlag(KeyModifiers.Control)))
            {
                _ = Finish(true);
                e.Handled = true;
            }
        }, RoutingStrategies.Tunnel);
        box.LostFocus += (_, _) => _ = Finish(true);
        // The box is already in the tree and its template is not applied yet, so focus it once layout has run
        Dispatcher.UIThread.Post(() => FocusEditor(box), DispatcherPriority.Loaded);
    }

    private static void FocusEditor(TextBox box)
    {
        // The editor was closed meanwhile
        if (box.GetVisualRoot() == null) return;

        if (!box.Focus())
        {
            // Not focusable yet: try once more shortly after
            DispatcherTimer.RunOnce(() => FocusEditor(box), TimeSpan.FromMilliseconds(80));
            return;
        }

        box.SelectAll();
    }

    private void OnTemplateClicked(object? sender, RoutedEventArgs e)
    {
        // Close the quick-add flyout once a template was picked
        this.FindControl<Button>("QuickAddButton")?.Flyout?.Hide();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // A new page starts at the top of the list
        if (e.PropertyName == nameof(DailyTasksViewModel.CurrentPage))
        {
            this.FindControl<ScrollViewer>("PageScroll")?.ScrollToHome();
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