namespace DailyTaskManager.Core.Models
{
    /// <summary>
    /// Keys and defaults of the update notification settings
    /// </summary>
    public static class UpdateSettings
    {
        /// <summary>Whether Windows also tells the user about a new version while the app is closed</summary>
        public const string BackgroundNotifyKey = "Updates.BackgroundNotify";

        public const bool DefaultBackgroundNotify = true;
    }
}
