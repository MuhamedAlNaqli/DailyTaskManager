using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Avalonia.ViewModels;

/// <summary>The week view: seven day columns, with navigation between weeks</summary>
public partial class DailyTasksViewModel
{
    private int _weekLoadVersion;

    /// <summary>The week view is the default; the list keeps the filters, paging and bulk actions</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsListView))]
    private bool _isWeekView = true;

    public bool IsListView => !IsWeekView;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WeekTitle), nameof(IsCurrentWeek))]
    private DateTime _weekStart = WeekDay.StartOfWeek(DateTime.Today);

    [ObservableProperty]
    private ObservableCollection<WeekDay> _weekDays = new();

    [ObservableProperty]
    private decimal _weekTotalHours;

    public bool IsCurrentWeek => WeekStart == WeekDay.StartOfWeek(DateTime.Today);

    public string WeekTitle
    {
        get
        {
            var end = WeekStart.AddDays(6);
            var culture = CultureInfo.InvariantCulture;
            return WeekStart.Year == end.Year
                ? $"{WeekStart.ToString("d MMM", culture)} - {end.ToString("d MMM yyyy", culture)}"
                : $"{WeekStart.ToString("d MMM yyyy", culture)} - {end.ToString("d MMM yyyy", culture)}";
        }
    }

    [RelayCommand]
    private void ShowWeekView() => IsWeekView = true;

    [RelayCommand]
    private void ShowListView() => IsWeekView = false;

    [RelayCommand]
    private Task PreviousWeekAsync() => MoveWeekAsync(WeekStart.AddDays(-7));

    [RelayCommand]
    private Task NextWeekAsync() => MoveWeekAsync(WeekStart.AddDays(7));

    [RelayCommand]
    private Task CurrentWeekAsync() => MoveWeekAsync(WeekDay.StartOfWeek(DateTime.Today));

    private Task MoveWeekAsync(DateTime start)
    {
        WeekStart = start;
        return RefreshWeekAsync();
    }

    [RelayCommand]
    private async Task AddForDayAsync(WeekDay? day)
    {
        if (day == null) return;

        try
        {
            var task = await _dialogService.ShowAddTaskDialogAsync(null, day.Date);
            if (task != null)
            {
                await _databaseService.AddTaskAsync(task);
                await LoadTasksAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding an entry for {Date}", day.Date);
            await _dialogService.ShowErrorDialogAsync("Error", "Failed to add the entry. Please try again.");
        }
    }

    /// <summary>Loads the entries of the shown week. Only the latest request wins.</summary>
    public async Task RefreshWeekAsync()
    {
        var version = Interlocked.Increment(ref _weekLoadVersion);

        try
        {
            if (!_authService.IsAuthenticated)
            {
                WeekDays = new ObservableCollection<WeekDay>();
                WeekTotalHours = 0;
                return;
            }

            var start = WeekStart;
            var tasks = await _databaseService.GetTasksAsync(new TaskQuery(null, start, start.AddDays(6)));
            if (version != _weekLoadVersion) return;

            WeekDays = new ObservableCollection<WeekDay>(
                Enumerable.Range(0, 7).Select(i => new WeekDay(start.AddDays(i), tasks.Where(t => t.Date.Date == start.AddDays(i)))));
            WeekTotalHours = tasks.Sum(t => t.Hours);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading the week of {Start}", WeekStart);
        }
    }
}
