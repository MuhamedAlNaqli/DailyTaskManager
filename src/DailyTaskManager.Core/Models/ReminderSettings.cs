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

        public const bool DefaultEnabled = true;

        /// <summary>Today counts as missing once it is this hour or later</summary>
        public const int DefaultHour = 16;
    }
}
