using System;

namespace DailyTaskManager.Core.Models
{
    /// <summary>
    /// Keys and defaults of the missing-day reminder settings
    /// </summary>
    public static class ReminderSettings
    {
        public const string KeyPrefix = "Reminder.";

        public const string EnabledKey = "Reminder.Enabled";

        public const string HourKey = "Reminder.Hour";

        /// <summary>Whether Windows also reminds the user while the app is closed</summary>
        public const string BackgroundKey = "Reminder.Background";

        public const bool DefaultEnabled = true;

        public const bool DefaultBackground = true;

        /// <summary>Today counts as missing once it is this hour or later</summary>
        public const int DefaultHour = 16;

        public static bool ParseFlag(string? stored, bool defaultValue) =>
            stored == null ? defaultValue : string.Equals(stored, "true", StringComparison.OrdinalIgnoreCase);

        public static int ParseHour(string? stored) =>
            int.TryParse(stored, out var hour) && hour >= 0 && hour <= 23 ? hour : DefaultHour;
    }
}
