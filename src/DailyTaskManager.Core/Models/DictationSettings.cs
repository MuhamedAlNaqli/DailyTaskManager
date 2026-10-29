using System.Collections.Generic;

namespace DailyTaskManager.Core.Models
{
    /// <summary>
    /// Keys, defaults and the choices of the dictation language setting
    /// </summary>
    public static class DictationSettings
    {
        public const string LanguageKey = "Dictation.Language";

        /// <summary>Whisper detects the language itself</summary>
        public const string AutoLanguage = "auto";

        /// <summary>Language code (as Whisper knows it) and the name shown in Settings</summary>
        public static readonly IReadOnlyList<KeyValuePair<string, string>> Languages = new[]
        {
            new KeyValuePair<string, string>(AutoLanguage, "Detect automatically"),
            new KeyValuePair<string, string>("en", "English"),
            new KeyValuePair<string, string>("bs", "Bosnian"),
            new KeyValuePair<string, string>("hr", "Croatian"),
            new KeyValuePair<string, string>("sr", "Serbian"),
            new KeyValuePair<string, string>("de", "German"),
            new KeyValuePair<string, string>("tr", "Turkish"),
        };
    }
}
