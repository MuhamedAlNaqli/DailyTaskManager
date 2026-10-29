using Microsoft.VisualStudio.TestTools.UnitTesting;
using DailyTaskManager.Avalonia.Background;
using DailyTaskManager.Avalonia.ViewModels;

namespace DailyTaskManager.UI.Tests;

/// <summary>
/// The background update notice (feed parsing, announce-once rule) and the Help page content
/// </summary>
[TestClass]
public class HelpAndUpdateNoticeTests
{
    private const string Feed = @"<?xml version=""1.0""?>
<rss xmlns:sparkle=""http://www.andymatuschak.org/xml-namespaces/sparkle"" version=""2.0""><channel>
  <item><title>old</title><sparkle:version>2.2.1.0</sparkle:version><sparkle:shortVersionString>2.2.1</sparkle:shortVersionString><sparkle:os>windows</sparkle:os></item>
  <item><title>new</title><sparkle:version>3.2.0.0</sparkle:version><sparkle:shortVersionString>3.2.0</sparkle:shortVersionString><sparkle:os>windows</sparkle:os></item>
  <item><title>mac</title><sparkle:version>9.0.0.0</sparkle:version><sparkle:os>macos</sparkle:os></item>
  <item><title>broken</title><sparkle:version>not-a-version</sparkle:version></item>
</channel></rss>";

    [TestMethod]
    public void ParseLatest_ShouldPickTheNewestWindowsRelease()
    {
        var latest = BackgroundUpdateChecker.ParseLatest(Feed);

        Assert.IsNotNull(latest);
        Assert.AreEqual(new Version(3, 2, 0, 0), latest.Version);
        Assert.AreEqual("3.2.0", latest.DisplayVersion);
    }

    [TestMethod]
    public void ParseLatest_ShouldReturnNullWithoutUsableItems()
    {
        Assert.IsNull(BackgroundUpdateChecker.ParseLatest("<rss><channel></channel></rss>"));
    }

    [TestMethod]
    public void UpdateNotice_ShouldBeAnnouncedOncePerReleaseAndRepeatAfterDays()
    {
        var state = new ReminderState();
        var now = new DateTime(2026, 10, 29, 9, 0, 0);

        Assert.IsTrue(state.UpdateCheckDue(now), "Never checked yet");
        state.LastUpdateCheck = now;
        Assert.IsFalse(state.UpdateCheckDue(now.AddHours(1)), "Checks are spaced out");
        Assert.IsTrue(state.UpdateCheckDue(now.AddHours(7)));

        Assert.IsTrue(state.ShouldAnnounce("3.3.0", now));
        state.RecordAnnounced("3.3.0", now);
        Assert.IsFalse(state.ShouldAnnounce("3.3.0", now.AddDays(1)), "Not announced again the next day");
        Assert.IsTrue(state.ShouldAnnounce("3.3.0", now.AddDays(4)), "A gentle nudge after a few days");
        Assert.IsTrue(state.ShouldAnnounce("3.4.0", now.AddHours(1)), "A newer release is announced right away");

        state.UpdateNotifications = false;
        state.LastUpdateCheck = default;
        Assert.IsFalse(state.UpdateCheckDue(now), "Switched off in Settings");
    }

    [TestMethod]
    public void Help_ShouldExplainTheHiddenFeatures()
    {
        var items = new HelpViewModel().Sections.SelectMany(s => s.Items).ToList();

        Assert.IsTrue(items.Any(i => i.Keys.SequenceEqual(new[] { "Ctrl", "Z" })), "Undo shortcut");
        Assert.IsTrue(items.Any(i => i.Keys.SequenceEqual(new[] { "Ctrl", "Enter" })), "Save inline edit");
        Assert.IsTrue(items.Any(i => i.Keys.Contains("Double-click")), "Inline edit by double-click");
        Assert.IsTrue(items.Any(i => i.Title.Contains("Week view")), "Week view");
        Assert.IsTrue(items.Any(i => i.Title.Contains("New versions")), "Background update notice");
        Assert.IsTrue(items.All(i => i.Title.Length > 0 && i.Description.Length > 0), "No empty entries");
    }
}
