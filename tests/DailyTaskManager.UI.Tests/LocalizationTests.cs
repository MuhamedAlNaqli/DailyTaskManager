using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DailyTaskManager.Core.Localization;

namespace DailyTaskManager.UI.Tests;

/// <summary>
/// The translation catalog: complete for every language, consistent in its placeholders, and applied correctly.
/// The language is process-wide, so these tests run one at a time and always return to English.
/// </summary>
[TestClass]
[DoNotParallelize]
public class LocalizationTests
{
    private static readonly string[] Languages = { "bs", "hr", "sr", "de", "tr" };

    [TestCleanup]
    public void BackToEnglish() => Loc.SetLanguage("en", persist: false);

    [TestMethod]
    public void Catalog_ShouldHaveATranslationForEveryLanguage()
    {
        Assert.IsTrue(Loc.Catalog.Count > 350, "The catalog covers the whole interface");

        var problems = new List<string>();
        foreach (var (english, row) in Loc.Catalog)
        {
            Assert.AreEqual(5, row.Length);
            for (var i = 0; i < row.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(row[i])) problems.Add($"{Languages[i]}: {english}");
            }
        }

        Assert.AreEqual(0, problems.Count, "Missing translations:\n" + string.Join("\n", problems.Take(20)));
    }

    [TestMethod]
    public void Catalog_ShouldKeepThePlaceholdersOfTheEnglishText()
    {
        var problems = new List<string>();
        foreach (var (english, row) in Loc.Catalog)
        {
            var expected = Placeholders(english);
            for (var i = 0; i < row.Length; i++)
            {
                if (!Placeholders(row[i]).SequenceEqual(expected)) problems.Add($"{Languages[i]}: {english}");
            }
        }

        Assert.AreEqual(0, problems.Count, "Placeholders differ:\n" + string.Join("\n", problems.Take(20)));
    }

    [TestMethod]
    public void Serbian_ShouldUseTheLatinScript()
    {
        var cyrillic = Loc.Catalog.Where(kv => Regex.IsMatch(kv.Value[2], @"[Ѐ-ӿ]")).Select(kv => kv.Key).ToList();
        Assert.AreEqual(0, cyrillic.Count, string.Join("\n", cyrillic.Take(10)));
    }

    [TestMethod]
    public void Translate_ShouldUseTheChosenLanguageAndLeaveOtherTextAlone()
    {
        Assert.AreEqual("Settings", Loc.Translate("Settings"));

        Loc.SetLanguage("hr", persist: false);
        Assert.AreEqual("Postavke", Loc.Translate("Settings"));
        Assert.AreEqual("hr-HR", Loc.Culture.Name);

        Loc.SetLanguage("sr", persist: false);
        Assert.AreEqual("Podešavanja", Loc.Translate("Settings"));

        Loc.SetLanguage("de", persist: false);
        Assert.AreEqual("Einstellungen", Loc.Translate("Settings"));

        Loc.SetLanguage("tr", persist: false);
        Assert.AreEqual("Ayarlar", Loc.Translate("Settings"));

        Loc.SetLanguage("bs", persist: false);
        Assert.AreEqual("Mtel", Loc.Translate("Mtel"), "A client name is not translated");
        Assert.AreEqual("  Postavke ", Loc.Translate("  Settings "), "Surrounding spaces are kept");
    }

    [TestMethod]
    public void Translate_ShouldFillPatternsWithTheDynamicParts()
    {
        Loc.SetLanguage("bs", persist: false);

        Assert.AreEqual("Obrisano \"Statusni sastanak\"", Loc.Translate("Deleted \"Statusni sastanak\""));
        Assert.AreEqual("Ukupno za sedmicu: 28h", Loc.Translate("Week total: 28h"));
        Assert.AreEqual("Verzija 3.4.0", Loc.Translate("Version 3.4.0"));
        Assert.AreEqual("Trenutna verzija: 3.4.0\nImate najnoviju verziju (3.4.0).",
            Loc.Translate("Current version: 3.4.0\nYou are up to date (version 3.4.0)."), "Parts of a pattern are translated too");
    }

    [TestMethod]
    public void Format_ShouldPickTheTemplateOfTheLanguage()
    {
        Assert.AreEqual("28.0h logged on 4 days", Loc.F("{0}h logged on {1} days", "28.0", 4));

        Loc.SetLanguage("de", persist: false);
        Assert.AreEqual("28,0h an 4 Tagen erfasst", Loc.F("{0}h logged on {1} days", "28,0", 4));
        Assert.AreEqual("Not in the catalog 5", Loc.F("Not in the catalog {0}", 5), "Without a translation the English template is used");
    }

    [TestMethod]
    public void Languages_ShouldMatchTheDictationLanguages()
    {
        var dictation = DailyTaskManager.Core.Models.DictationSettings.Languages.Select(l => l.Key).Where(k => k != "auto").OrderBy(k => k);
        var interfaces = Loc.Languages.Select(l => l.Key).OrderBy(k => k);
        CollectionAssert.AreEqual(dictation.ToList(), interfaces.ToList(), "Every dictation language also has a translated interface");
    }

    private static List<string> Placeholders(string text) =>
        Regex.Matches(text, @"\{\d\}").Select(m => m.Value).OrderBy(v => v).ToList();
}
