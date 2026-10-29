using System;
using System.IO;
using System.Text.Json;

namespace DailyTaskManager.Avalonia.Background;

/// <summary>
/// Remembers per machine how often the background reminder has already spoken today, so it never nags
/// </summary>
public class ReminderState
{
    public const int MaxPerDay = 2;
    public static readonly TimeSpan MinGap = TimeSpan.FromHours(2);

    public string Day { get; set; } = "";

    public int Count { get; set; }

    public DateTime LastShown { get; set; }

    /// <summary>Whether the background job may tell the user about new versions (mirrors the setting, so no database is needed)</summary>
    public bool UpdateNotifications { get; set; } = true;

    public DateTime LastUpdateCheck { get; set; }

    /// <summary>The version the user was last told about, so each release is announced once (then again after a few days)</summary>
    public string NotifiedVersion { get; set; } = "";

    public DateTime NotifiedAt { get; set; }

    /// <summary>Which exe (and task definition) the scheduled task was last registered for</summary>
    public string TaskMarker { get; set; } = "";

    public static string FolderPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DailyTaskManager");

    private static string FilePath => Path.Combine(FolderPath, "reminder-state.json");

    public bool CanNotify(DateTime now)
    {
        if (Day != DayKey(now)) return true;
        return Count < MaxPerDay && now - LastShown >= MinGap;
    }

    public void Record(DateTime now)
    {
        if (Day != DayKey(now))
        {
            Day = DayKey(now);
            Count = 0;
        }

        Count++;
        LastShown = now;
    }

    public static readonly TimeSpan UpdateCheckInterval = TimeSpan.FromHours(6);
    public static readonly TimeSpan UpdateRepeatAfter = TimeSpan.FromDays(3);

    public bool UpdateCheckDue(DateTime now) => UpdateNotifications && now - LastUpdateCheck >= UpdateCheckInterval;

    public bool ShouldAnnounce(string version, DateTime now) =>
        NotifiedVersion != version || now - NotifiedAt >= UpdateRepeatAfter;

    public void RecordAnnounced(string version, DateTime now)
    {
        NotifiedVersion = version;
        NotifiedAt = now;
    }

    public static ReminderState Load()
    {
        try
        {
            return File.Exists(FilePath)
                ? JsonSerializer.Deserialize<ReminderState>(File.ReadAllText(FilePath)) ?? new ReminderState()
                : new ReminderState();
        }
        catch
        {
            return new ReminderState();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(FolderPath);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
        }
        catch
        {
            // Worst case the reminder repeats; not worth failing for
        }
    }

    private static string DayKey(DateTime time) => time.ToString("yyyy-MM-dd");
}
