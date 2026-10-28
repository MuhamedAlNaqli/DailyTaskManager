using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DailyTaskManager.Avalonia.Background;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.UI.Tests;

/// <summary>
/// The parts of the background reminder that need neither a database nor Windows: when it may speak,
/// and what it asks the task scheduler to run
/// </summary>
[TestClass]
public class BackgroundReminderTests
{
    [TestMethod]
    public void ReminderState_ShouldSpeakTwiceAtMostAndNotTooOften()
    {
        var state = new ReminderState();
        var nineAm = new DateTime(2026, 10, 28, 9, 0, 0);

        Assert.IsTrue(state.CanNotify(nineAm), "A fresh state may notify");
        state.Record(nineAm);

        Assert.IsFalse(state.CanNotify(nineAm.AddMinutes(30)), "Not again within two hours");
        Assert.IsTrue(state.CanNotify(nineAm.AddHours(2)));
        state.Record(nineAm.AddHours(2));

        Assert.IsFalse(state.CanNotify(nineAm.AddHours(8)), "At most two reminders a day");
        Assert.IsTrue(state.CanNotify(nineAm.AddDays(1)), "A new day starts over");
        state.Record(nineAm.AddDays(1));
        Assert.AreEqual(1, state.Count);
    }

    [TestMethod]
    public void TaskDefinition_ShouldRunTheExeWithTheCheckArgumentEveryHalfHour()
    {
        const string exe = @"C:\Program Files\SmartPoint\DailyTaskManager\DailyTaskManager.Avalonia.exe";
        var xml = XDocument.Parse(WindowsReminderScheduler.BuildTaskXml(exe, @"PC\Some & User"));
        XNamespace ns = "http://schemas.microsoft.com/windows/2004/02/mit/task";

        var exec = xml.Descendants(ns + "Exec").Single();
        Assert.AreEqual(exe, exec.Element(ns + "Command")!.Value);
        Assert.AreEqual("--reminder-check", exec.Element(ns + "Arguments")!.Value);
        Assert.AreEqual(Path.GetDirectoryName(exe), exec.Element(ns + "WorkingDirectory")!.Value);

        Assert.AreEqual("PT30M", xml.Descendants(ns + "Repetition").Single().Element(ns + "Interval")!.Value);
        Assert.IsTrue(xml.Descendants(ns + "LogonTrigger").Any(), "Also starts when the user signs in");
        Assert.AreEqual("true", xml.Descendants(ns + "StartWhenAvailable").Single().Value, "Catches up after sleep or shutdown");
        Assert.AreEqual(@"PC\Some & User", xml.Descendants(ns + "Principal").Single().Element(ns + "UserId")!.Value, "Special characters survive escaping");
    }

    [TestMethod]
    public void ReminderSettings_ShouldParseStoredValuesWithDefaults()
    {
        Assert.IsTrue(ReminderSettings.ParseFlag(null, true));
        Assert.IsFalse(ReminderSettings.ParseFlag(null, false));
        Assert.IsTrue(ReminderSettings.ParseFlag("TRUE", false));
        Assert.IsFalse(ReminderSettings.ParseFlag("false", true));

        Assert.AreEqual(9, ReminderSettings.ParseHour("9"));
        Assert.AreEqual(ReminderSettings.DefaultHour, ReminderSettings.ParseHour("25"));
        Assert.AreEqual(ReminderSettings.DefaultHour, ReminderSettings.ParseHour("abc"));
        Assert.AreEqual(ReminderSettings.DefaultHour, ReminderSettings.ParseHour(null));
    }
}
