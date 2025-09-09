using Avalonia.Controls;
using DailyTaskManager.Avalonia.ViewModels;
using Microsoft.Extensions.Logging;

namespace DailyTaskManager.Avalonia.Views;

public partial class DailyTasksView : UserControl
{
    public DailyTasksView()
    {
        InitializeComponent();
    }

    private void DateFromPicker_SelectedDateChanged(object? sender, DatePickerSelectedValueChangedEventArgs e)
    {
        // Debug logging to see if event is firing - let XAML binding handle the actual property setting
        System.Diagnostics.Debug.WriteLine($"DateFromPicker_SelectedDateChanged: OldValue={e.OldDate}, NewValue={e.NewDate}");
        
        // Note: Removed manual property setting to avoid conflicts with TwoWay binding
        // The XAML binding should handle: SelectedDate="{Binding DateFrom, Mode=TwoWay}"
    }

    private void DateToPicker_SelectedDateChanged(object? sender, DatePickerSelectedValueChangedEventArgs e)
    {
        // Debug logging to see if event is firing - let XAML binding handle the actual property setting
        System.Diagnostics.Debug.WriteLine($"DateToPicker_SelectedDateChanged: OldValue={e.OldDate}, NewValue={e.NewDate}");
        
        // Note: Removed manual property setting to avoid conflicts with TwoWay binding
        // The XAML binding should handle: SelectedDate="{Binding DateTo, Mode=TwoWay}"
    }
}