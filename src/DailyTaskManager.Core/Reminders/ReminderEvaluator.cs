using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Core.Reminders;

/// <summary>
/// What the reminder knows about the user's logging at a given moment
/// </summary>
public class ReminderStatus
{
    public bool Enabled { get; set; }

    public bool BackgroundEnabled { get; set; }

    public int Hour { get; set; }

    /// <summary>Working days without hours, oldest first; empty when nothing is due</summary>
    public IReadOnlyList<DateTime> MissingDays { get; set; } = new List<DateTime>();

    public bool IsDue => Enabled && MissingDays.Count > 0;

    public string Message { get; set; } = "";
}

/// <summary>
/// Single place that decides whether the user owes entries; used by the in-app banner and the background job
/// </summary>
public class ReminderEvaluator
{
    private readonly IDatabaseService _database;
    private readonly ISettingsService _settings;

    public ReminderEvaluator(IDatabaseService database, ISettingsService settings)
    {
        _database = database;
        _settings = settings;
    }

    public async Task<ReminderStatus> EvaluateAsync(DateTime now)
    {
        var enabled = ReminderSettings.ParseFlag(await _settings.GetSettingAsync<string>(ReminderSettings.EnabledKey, null), ReminderSettings.DefaultEnabled);
        var background = ReminderSettings.ParseFlag(await _settings.GetSettingAsync<string>(ReminderSettings.BackgroundKey, null), ReminderSettings.DefaultBackground);
        var hour = ReminderSettings.ParseHour(await _settings.GetSettingAsync<string>(ReminderSettings.HourKey, null));
        var status = new ReminderStatus { Enabled = enabled, BackgroundEnabled = background, Hour = hour };
        if (!enabled) return status;

        var first = await _database.GetFirstEntryDateAsync();
        if (first == null) return status;

        var today = now.Date;
        var from = first.Value.Date > today.AddDays(-(MissingDays.LookbackDays - 1)) ? first.Value.Date : today.AddDays(-(MissingDays.LookbackDays - 1));
        var days = await _database.GetDailyHoursAsync(new TaskQuery(null, from, today));
        var logged = new HashSet<DateTime>(days.Where(d => d.Hours > 0).Select(d => d.Date.Date));

        var missing = MissingDays.Find(first, today, now, logged, hour);
        status.MissingDays = missing;
        status.Message = MissingDays.Describe(missing, today);
        return status;
    }
}
