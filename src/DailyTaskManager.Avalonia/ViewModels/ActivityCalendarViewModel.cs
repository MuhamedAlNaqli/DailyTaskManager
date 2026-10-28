using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DailyTaskManager.Avalonia.Services;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Avalonia.ViewModels;

/// <summary>One day cell of the calendar</summary>
public class CalendarDay
{
    public DateTime Date { get; init; }

    public string DayText { get; init; } = "";

    public bool InMonth { get; init; }

    public bool IsToday { get; init; }

    public decimal Hours { get; init; }

    public int Entries { get; init; }

    public string HoursText { get; init; } = "";

    public string Tooltip { get; init; } = "";

    public IBrush Fill { get; init; } = Brushes.Transparent;

    public IBrush Border { get; init; } = Brushes.Transparent;

    public double BorderWidth { get; init; } = 1;

    public double CellOpacity { get; init; } = 1;
}

public partial class ActivityCalendarViewModel : ObservableObject, IDisposable
{
    private const decimal DailyTarget = 7m;

    private static readonly IBrush Green = Brush.Parse("#4010B981");
    private static readonly IBrush Amber = Brush.Parse("#40F59E0B");
    private static readonly IBrush Red = Brush.Parse("#33EF4444");
    private static readonly IBrush Neutral = Brush.Parse("#14808080");
    private static readonly IBrush NeutralBorder = Brush.Parse("#22808080");
    private static readonly IBrush TodayBorder = Brush.Parse("#FF3B82F6");

    private readonly IDatabaseService _databaseService;
    private readonly IAuthenticationService _authService;
    private readonly DialogService _dialogService;
    private readonly ILogger<ActivityCalendarViewModel> _logger;
    private int _loadVersion;

    public IReadOnlyList<string> WeekdayNames { get; } = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

    public ObservableCollection<CalendarDay> Days { get; } = new();

    [ObservableProperty]
    private DateTime _month = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    [ObservableProperty]
    private string _monthTitle = "";

    [ObservableProperty]
    private string _summaryText = "";

    public ActivityCalendarViewModel(
        IDatabaseService databaseService,
        IAuthenticationService authService,
        DialogService dialogService,
        ILogger<ActivityCalendarViewModel> logger)
    {
        _databaseService = databaseService;
        _authService = authService;
        _dialogService = dialogService;
        _logger = logger;

        _databaseService.TasksChanged += OnTasksChanged;
        _ = LoadAsync();
    }

    public void Dispose()
    {
        _databaseService.TasksChanged -= OnTasksChanged;
        Interlocked.Increment(ref _loadVersion);
    }

    private void OnTasksChanged(object? sender, EventArgs e) => Dispatcher.UIThread.Post(() => _ = LoadAsync());

    partial void OnMonthChanged(DateTime value) => _ = LoadAsync();

    [RelayCommand]
    private void PreviousMonth() => Month = Month.AddMonths(-1);

    [RelayCommand]
    private void NextMonth() => Month = Month.AddMonths(1);

    [RelayCommand]
    private void GoToToday() => Month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    [RelayCommand]
    private async Task AddForDayAsync(CalendarDay? day)
    {
        if (day == null) return;

        try
        {
            // A day that already has an entry is edited instead of getting a second one
            var existing = (await _databaseService.GetTasksAsync(new TaskQuery(null, day.Date, day.Date))).FirstOrDefault();
            if (existing != null)
            {
                var edited = await _dialogService.ShowEditTaskDialogAsync(existing);
                if (edited != null) await _databaseService.UpdateTaskAsync(edited);
                return;
            }

            var task = await _dialogService.ShowAddTaskDialogAsync(null, day.Date);
            if (task != null)
            {
                await _databaseService.AddTaskAsync(task);
                // TasksChanged refreshes the calendar
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding an entry for {Date}", day.Date);
            await _dialogService.ShowErrorDialogAsync("Error", "Failed to add the entry. Please try again.");
        }
    }

    private async Task LoadAsync()
    {
        var version = Interlocked.Increment(ref _loadVersion);
        try
        {
            if (!_authService.IsAuthenticated) return;

            var first = Month;
            var last = first.AddMonths(1).AddDays(-1);
            var gridStart = first.AddDays(-(((int)first.DayOfWeek + 6) % 7)); // Monday on or before the 1st
            var weeks = (int)Math.Ceiling(((last - gridStart).TotalDays + 1) / 7);
            var gridEnd = gridStart.AddDays(weeks * 7 - 1);

            var tasks = await _databaseService.GetTasksAsync(new TaskQuery(null, gridStart, gridEnd));
            if (version != _loadVersion) return;

            var byDate = tasks.GroupBy(t => t.Date.Date).ToDictionary(g => g.Key, g => g.ToList());
            var today = DateTime.Today;

            var cells = new List<CalendarDay>(weeks * 7);
            for (var d = gridStart; d <= gridEnd; d = d.AddDays(1))
            {
                byDate.TryGetValue(d, out var entries);
                cells.Add(BuildCell(d, d.Month == first.Month && d.Year == first.Year, today, entries));
            }

            Days.Clear();
            foreach (var cell in cells) Days.Add(cell);
            MonthTitle = first.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

            var monthCells = cells.Where(c => c.InMonth).ToList();
            var total = monthCells.Sum(c => c.Hours);
            var logged = monthCells.Count(c => c.Hours > 0);
            var missing = monthCells.Count(c => IsMissing(c.Date, c.Hours, today));
            SummaryText = $"{total:0.0}h logged on {logged} day{(logged == 1 ? "" : "s")}" +
                          (missing > 0 ? $" - {missing} working day{(missing == 1 ? "" : "s")} without an entry" : "");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading the calendar");
        }
    }

    private static bool IsMissing(DateTime date, decimal hours, DateTime today) =>
        hours <= 0 && date < today && date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday;

    private static CalendarDay BuildCell(DateTime date, bool inMonth, DateTime today, List<DailyTask>? entries)
    {
        var hours = entries?.Sum(e => e.Hours) ?? 0m;
        var count = entries?.Count ?? 0;

        IBrush fill;
        if (hours >= DailyTarget) fill = Green;
        else if (hours > 0) fill = Amber;
        else if (IsMissing(date, hours, today)) fill = Red;
        else fill = Neutral;

        string tooltip;
        if (count == 0)
        {
            tooltip = $"{date:ddd d MMM yyyy}: no entries - click to add one";
        }
        else
        {
            var lines = entries!
                .Take(3)
                .Select(e => "- " + FirstLine(e.Description) + $" ({e.Hours:0.##}h)");
            tooltip = $"{date:ddd d MMM yyyy}: {hours:0.0}h\n" + string.Join("\n", lines) +
                      (count > 3 ? $"\n(+{count - 3} more)" : "");
        }

        return new CalendarDay
        {
            Date = date,
            DayText = date.Day.ToString(CultureInfo.InvariantCulture),
            InMonth = inMonth,
            IsToday = date == today,
            Hours = hours,
            Entries = count,
            HoursText = hours > 0 ? $"{hours:0.#}h" : "",
            Tooltip = tooltip,
            Fill = fill,
            Border = date == today ? TodayBorder : NeutralBorder,
            BorderWidth = date == today ? 2 : 1,
            CellOpacity = inMonth ? 1 : 0.45
        };
    }

    private static string FirstLine(string text)
    {
        var line = text.Replace("\r", "").Split('\n').Select(l => l.Trim().TrimStart('-', ' ')).FirstOrDefault(l => l.Length > 0) ?? "";
        return line.Length > 40 ? line.Substring(0, 40) + "..." : line;
    }
}
