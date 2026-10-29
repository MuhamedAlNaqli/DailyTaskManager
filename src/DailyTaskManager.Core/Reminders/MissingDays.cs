using DailyTaskManager.Core.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DailyTaskManager.Core.Reminders;

/// <summary>
/// Works out which working days have no logged hours
/// </summary>
public static class MissingDays
{
    public const int LookbackDays = 14;

    /// <summary>
    /// Returns the weekdays without entries, oldest first. Only days from the user's first entry on are considered
    /// (so a new account is not told about the past), and today counts once it is <paramref name="reminderHour"/> or later.
    /// </summary>
    public static List<DateTime> Find(DateTime? firstEntry, DateTime today, DateTime now, ISet<DateTime> loggedDays, int reminderHour)
    {
        var missing = new List<DateTime>();
        if (firstEntry == null) return missing;

        var from = firstEntry.Value.Date > today.AddDays(-(LookbackDays - 1)) ? firstEntry.Value.Date : today.AddDays(-(LookbackDays - 1));
        for (var d = from.Date; d <= today.Date; d = d.AddDays(1))
        {
            if (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday) continue;
            if (loggedDays.Contains(d)) continue;
            if (d == today.Date && now.Hour < reminderHour) continue;
            missing.Add(d);
        }

        return missing;
    }

    /// <summary>Short sentence for the reminder banner</summary>
    public static string Describe(IReadOnlyList<DateTime> days, DateTime today)
    {
        if (days.Count == 0) return "";

        string Label(DateTime d) => d.ToString("ddd d MMM", Loc.Culture);

        if (days.Count == 1)
        {
            return days[0].Date == today.Date
                ? Loc.T("You haven't logged any hours today yet.")
                : Loc.F("No entry for {0}.", Label(days[0]));
        }

        var shown = string.Join(", ", days.Take(4).Select(Label));
        var more = days.Count > 4 ? ", ..." : "";
        return Loc.F("No entries for {0} working days: {1}", days.Count, shown + more);
    }
}
