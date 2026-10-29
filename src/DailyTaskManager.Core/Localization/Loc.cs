using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DailyTaskManager.Core.Localization
{
    /// <summary>
    /// The interface language. Text in the app is written in English; <see cref="Translate"/> looks it up in the
    /// catalog (exact sentences first, then patterns such as "Deleted \"{0}\"") and returns the translation, or the
    /// English text when there is none. The choice is kept in a small file per machine, so the background reminder
    /// (a separate process) speaks the same language as the app.
    /// </summary>
    public static class Loc
    {
        public const string English = "en";

        /// <summary>Language code and the language's own name (never translated)</summary>
        public static readonly IReadOnlyList<KeyValuePair<string, string>> Languages = new[]
        {
            new KeyValuePair<string, string>("en", "English"),
            new KeyValuePair<string, string>("bs", "Bosanski"),
            new KeyValuePair<string, string>("hr", "Hrvatski"),
            new KeyValuePair<string, string>("sr", "Srpski"),
            new KeyValuePair<string, string>("de", "Deutsch"),
            new KeyValuePair<string, string>("tr", "Türkçe"),
        };

        private static readonly string[] CultureNames = { "en-US", "bs-Latn-BA", "hr-HR", "sr-Latn-RS", "de-DE", "tr-TR" };

        // Columns of the catalog after the English text, in this order
        private static readonly string[] CatalogColumns = { "bs", "hr", "sr", "de", "tr" };

        private sealed class Pattern
        {
            public Regex Regex = null!;
            public string[] Translations = null!;
            public int LiteralLength;
        }

        private static readonly object Gate = new object();
        private static Dictionary<string, string[]>? _exact;
        private static Dictionary<string, string[]>? _all;
        private static List<Pattern>? _patterns;

        public static string Language { get; private set; } = English;

        public static CultureInfo Culture { get; private set; } = new CultureInfo("en-US");

        /// <summary>Raised after the language changed, on the thread that changed it</summary>
        public static event Action? LanguageChanged;

        private static string PreferencesPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DailyTaskManager", "language.txt");

        /// <summary>Reads the saved language and applies its culture. Call once at start-up (app and background process).</summary>
        public static void Initialize()
        {
            var code = English;
            try
            {
                if (File.Exists(PreferencesPath)) code = File.ReadAllText(PreferencesPath).Trim().ToLowerInvariant();
            }
            catch
            {
                // Unreadable preference: English
            }

            Apply(IsSupported(code) ? code : English);
        }

        public static bool IsSupported(string code) => Languages.Any(l => l.Key == code);

        /// <param name="persist">False only in tests, which must not change the user's saved choice</param>
        public static void SetLanguage(string code, bool persist = true)
        {
            if (!IsSupported(code)) code = English;
            if (code == Language) return;

            Apply(code);
            if (persist) Save(code);
            LanguageChanged?.Invoke();
        }

        private static void Save(string code)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(PreferencesPath)!);
                File.WriteAllText(PreferencesPath, code);
            }
            catch
            {
                // The choice still applies for this session
            }
        }

        private static void Apply(string code)
        {
            Language = code;
            Culture = new CultureInfo(CultureNames[Array.FindIndex(Languages.ToArray(), l => l.Key == code)]);
            CultureInfo.DefaultThreadCurrentCulture = Culture;
            CultureInfo.DefaultThreadCurrentUICulture = Culture;
            CultureInfo.CurrentCulture = Culture;
            CultureInfo.CurrentUICulture = Culture;
        }

        /// <summary>
        /// Translates an English template (looked up as written, e.g. "{0}h logged on {1} days") and fills in the values.
        /// Used where the sentence is built from parts, so the plural and word order can follow the language.
        /// </summary>
        public static string F(string englishTemplate, params object[] args)
        {
            var template = englishTemplate;
            if (Language != English)
            {
                EnsureCatalog();
                var column = Array.IndexOf(CatalogColumns, Language);
                if (_all!.TryGetValue(englishTemplate, out var row) && row[column].Length > 0) template = row[column];
            }

            return string.Format(Culture, template, args);
        }

        /// <summary>Shorthand for <see cref="Translate"/></summary>
        public static string T(string english) => Translate(english);

        public static string Translate(string text)
        {
            if (Language == English || string.IsNullOrEmpty(text)) return text;

            var core = text.Trim();
            if (core.Length == 0) return text;

            EnsureCatalog();
            var column = Array.IndexOf(CatalogColumns, Language);
            var lead = text.Substring(0, text.Length - text.TrimStart().Length);
            var trail = text.Substring(text.TrimEnd().Length);

            if (_exact!.TryGetValue(core, out var row))
            {
                var translated = row[column];
                return translated.Length == 0 ? text : lead + translated + trail;
            }

            foreach (var pattern in _patterns!)
            {
                var match = pattern.Regex.Match(core);
                if (!match.Success) continue;

                var template = pattern.Translations[column];
                if (template.Length == 0) return text;

                var result = Regex.Replace(template, @"\{(\d)\}", m =>
                {
                    var index = int.Parse(m.Groups[1].Value) + 1;
                    return index < match.Groups.Count ? Translate(match.Groups[index].Value) : m.Value;
                });
                return lead + result + trail;
            }

            return text;
        }

        private static void EnsureCatalog()
        {
            if (_exact != null) return;

            lock (Gate)
            {
                if (_exact != null) return;

                var exact = new Dictionary<string, string[]>(StringComparer.Ordinal);
                var all = new Dictionary<string, string[]>(StringComparer.Ordinal);
                var patterns = new List<Pattern>();

                void Add(string en, string bs, string hr, string sr, string de, string tr)
                {
                    var row = new[] { bs, hr, sr, de, tr };
                    all[en] = row;
                    if (!en.Contains("{0}"))
                    {
                        exact[en] = row;
                        return;
                    }

                    // "Deleted \"{0}\"" becomes ^Deleted "(.*?)"$ ; the captured parts are translated again on the way out
                    var literal = Regex.Replace(en, @"\{\d\}", "");
                    var regex = "^" + Regex.Replace(Regex.Escape(en), @"\\\{(\d)\}", "(.+?)") + "$";
                    patterns.Add(new Pattern
                    {
                        Regex = new Regex(regex, RegexOptions.Singleline | RegexOptions.CultureInvariant),
                        Translations = row,
                        LiteralLength = literal.Length
                    });
                }

                UiTranslations.Register(Add);
                MessageTranslations.Register(Add);
                HelpTranslations.Register(Add);
                BusinessTranslations.Register(Add);

                // The most specific pattern (most fixed text) wins
                _patterns = patterns.OrderByDescending(p => p.LiteralLength).ToList();
                _all = all;
                _exact = exact;
            }
        }

        /// <summary>Names the catalog has no translation for in the given language; used by tests to keep the catalog complete</summary>
        public static IReadOnlyList<string> MissingTranslations(string code)
        {
            EnsureCatalog();
            var column = Array.IndexOf(CatalogColumns, code);
            return _exact!.Where(kv => kv.Value[column].Length == 0).Select(kv => kv.Key).ToList();
        }

        /// <summary>Every catalog entry (English text and its five translations), for tests</summary>
        public static IReadOnlyDictionary<string, string[]> Catalog
        {
            get
            {
                EnsureCatalog();
                return _all!;
            }
        }

        public static int CatalogSize
        {
            get
            {
                EnsureCatalog();
                return _exact!.Count + _patterns!.Count;
            }
        }

        /// <summary>The catalog's English sentences, for tests</summary>
        public static IReadOnlyCollection<string> CatalogKeys
        {
            get
            {
                EnsureCatalog();
                return _exact!.Keys.ToList();
            }
        }
    }
}
