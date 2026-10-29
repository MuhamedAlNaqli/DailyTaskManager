using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Avalonia.ViewModels;

/// <summary>One column of the week view: a calendar day with its entries</summary>
public class WeekDay
{
    public WeekDay(DateTime date, IEnumerable<DailyTask> tasks)
    {
        Date = date.Date;
        Tasks = tasks.OrderBy(t => t.CreatedAt).ToList();
        TotalHours = Tasks.Sum(t => t.Hours);
    }

    public DateTime Date { get; }

    public IReadOnlyList<DailyTask> Tasks { get; }

    public decimal TotalHours { get; }

    public string DayName => Date.ToString("ddd", CultureInfo.InvariantCulture);

    public string DateText => Date.ToString("d MMM", CultureInfo.InvariantCulture);

    public string HoursText => HasTasks ? $"{TotalHours:0.##}h" : "";

    public bool HasTasks => Tasks.Count > 0;

    public bool IsToday => Date == DateTime.Today;

    public bool IsWeekend => Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    /// <summary>Weeks run Monday to Sunday</summary>
    public static DateTime StartOfWeek(DateTime date) => date.Date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
}
