using Microsoft.VisualStudio.TestTools.UnitTesting;
using DailyTaskManager.Avalonia.ViewModels;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.UI.Tests;

/// <summary>
/// The week view's day model: weeks run Monday to Sunday and each day sums its own entries
/// </summary>
[TestClass]
public class WeekViewTests
{
    [TestMethod]
    public void StartOfWeek_ShouldBeTheMondayOfThatWeek()
    {
        var monday = new DateTime(2026, 10, 26);
        for (var offset = 0; offset < 7; offset++)
        {
            Assert.AreEqual(monday, WeekDay.StartOfWeek(monday.AddDays(offset)), $"Day {offset} of the week");
        }

        Assert.AreEqual(monday.AddDays(7), WeekDay.StartOfWeek(monday.AddDays(7)), "The next Monday starts a new week");
    }

    [TestMethod]
    public void WeekDay_ShouldSumHoursAndFlagWeekendsAndToday()
    {
        var day = new WeekDay(new DateTime(2025, 10, 25, 15, 30, 0), new[]
        {
            new DailyTask { Description = "a", Hours = 3.5m, CreatedAt = new DateTime(2025, 10, 25, 9, 0, 0) },
            new DailyTask { Description = "b", Hours = 4m, CreatedAt = new DateTime(2025, 10, 25, 8, 0, 0) }
        });

        Assert.AreEqual(new DateTime(2025, 10, 25), day.Date, "The time of day is dropped");
        Assert.AreEqual(7.5m, day.TotalHours);
        Assert.AreEqual("7.5h", day.HoursText);
        Assert.AreEqual("b", day.Tasks[0].Description, "Entries are listed in the order they were created");
        Assert.IsTrue(day.IsWeekend, "25 Oct 2025 is a Saturday (a fixed date in the past, so the test does not depend on today)");
        Assert.AreEqual("Sat", day.DayName);
        Assert.IsFalse(day.IsToday);

        var empty = new WeekDay(DateTime.Today, Array.Empty<DailyTask>());
        Assert.IsFalse(empty.HasTasks);
        Assert.AreEqual("", empty.HoursText);
        Assert.IsTrue(empty.IsToday);
    }
}
