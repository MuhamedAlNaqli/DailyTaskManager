using Company.Api.Contracts;
using DailyTaskManager.Avalonia.EmployeeMode;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Timesheet.Domain;

namespace DailyTaskManager.UI.Tests;

/// <summary>The logic of employee mode that needs no window: formats, parsing and the dialogs' view models</summary>
[TestClass]
public class EmployeeModeTests
{
    [DataTestMethod]
    [DataRow("8", 480)]
    [DataRow("7,5", 450)]
    [DataRow("7.5", 450)]
    [DataRow("7:30", 450)]
    [DataRow("7h30", 450)]
    [DataRow("7h", 420)]
    [DataRow(" 24 ", 1440)]
    [DataRow("0,5", 30)]
    public void Hours_ShouldBeReadTheWayAnEmployeeWritesThem(string text, int expected)
    {
        Assert.IsTrue(EmployeeFormat.TryParseHours(text, out var minutes), text);
        Assert.AreEqual(expected, minutes);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("abc")]
    [DataRow("0")]
    [DataRow("25")]
    [DataRow("7:75")]
    [DataRow("-3")]
    public void InvalidHours_ShouldBeRejected(string text) => Assert.IsFalse(EmployeeFormat.TryParseHours(text, out _), text);

    [DataTestMethod]
    [DataRow("17:00", 17, 0)]
    [DataRow("8:30", 8, 30)]
    [DataRow("17", 17, 0)]
    [DataRow("20.15", 20, 15)]
    public void Times_ShouldBeRead(string text, int hour, int minute)
    {
        Assert.IsTrue(EmployeeFormat.TryParseTime(text, out var time), text);
        Assert.AreEqual(new TimeOnly(hour, minute), time);
    }

    [TestMethod]
    public void Formats_ShouldBeBosnian()
    {
        Assert.AreEqual("Septembar 2026.", EmployeeFormat.Month(2026, 9));
        Assert.AreEqual("2,5 h", EmployeeFormat.Hours(150));
        Assert.AreEqual("8 h", EmployeeFormat.Hours(480));
        Assert.AreEqual("utorak", EmployeeFormat.Weekday(new DateOnly(2026, 9, 1)));
        Assert.AreEqual("Vraćeno na ispravku", EmployeeFormat.Status("Returned"));
        Assert.AreEqual("1022 Godišnji odmor A", EmployeeFormat.Entry(new EntryDto(1022, 0)));
        Assert.AreEqual("1010 Redovan rad: 8 h", EmployeeFormat.Entry(new EntryDto(1010, 480)));
    }

    // ---- Dialogs -------------------------------------------------------------------------------------------------------

    [TestMethod]
    public void DayDialog_ShouldOfferNoOvertimeCodes_AndAskForHoursOnlyWhenNeeded()
    {
        var vm = new DayEntryViewModel(new DateOnly(2026, 9, 1), Array.Empty<EntryDto>());
        Assert.IsFalse(vm.Codes.Any(c => c.Code.IsOvertime), "overtime is entered as an interval");
        Assert.AreEqual(1010, vm.SelectedCode!.Code.Code);
        Assert.IsTrue(vm.NeedsHours);

        vm.SelectedCode = vm.Codes.Single(c => c.Code.Code == 1022);
        Assert.IsFalse(vm.NeedsHours);
        vm.SaveCommand.Execute(null);
        Assert.AreEqual(new SetEntryRequest(1022, 0), vm.Result);
    }

    [TestMethod]
    public void DayDialog_ShouldRejectBadHoursAndKeepTheDialogOpen()
    {
        var vm = new DayEntryViewModel(new DateOnly(2026, 9, 1), Array.Empty<EntryDto>());
        var closed = false;
        vm.RequestClose += () => closed = true;

        vm.HoursText = "abc";
        vm.SaveCommand.Execute(null);
        Assert.IsFalse(closed);
        Assert.IsNull(vm.Result);
        StringAssert.Contains(vm.Error, "sate");

        vm.HoursText = "7,5";
        vm.SaveCommand.Execute(null);
        Assert.IsTrue(closed);
        Assert.AreEqual(new SetEntryRequest(1010, 450), vm.Result);
    }

    [TestMethod]
    public void DayDialog_ShouldStartFromTheCurrentEntry()
    {
        var vm = new DayEntryViewModel(new DateOnly(2026, 9, 1), new[] { new EntryDto(1010, 360) });
        Assert.AreEqual(1010, vm.SelectedCode!.Code.Code);
        Assert.AreEqual("6", vm.HoursText);
    }

    [TestMethod]
    public void OvertimeDialog_ShouldPreviewTheTotalAndTheSplitWhileTyping()
    {
        Func<DateOnly, DayType> workingDays = d => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? DayType.Weekend : DayType.Working;
        var vm = new OvertimeViewModel(2026, 9, workingDays, null)
        {
            Date = new DateTimeOffset(new DateTime(2026, 9, 7)),
            StartText = "20:00",
            EndText = "22:30"
        };

        StringAssert.Contains(vm.SummaryText, "2,5 h");
        StringAssert.Contains(vm.SummaryText, "2020");
        StringAssert.Contains(vm.SummaryText, "2021");
        Assert.AreEqual("", vm.WarningText);

        vm.StartText = "10:00";
        vm.EndText = "12:00";
        Assert.IsTrue(vm.WarningText.Contains("redovno radno vrijeme"), "inside 08:30 to 17:00 on a working day");
    }

    [TestMethod]
    public void OvertimeDialog_ShouldNeedADescriptionAndDifferentTimes()
    {
        Func<DateOnly, DayType> days = _ => DayType.Working;
        var vm = new OvertimeViewModel(2026, 9, days, null) { Date = new DateTimeOffset(new DateTime(2026, 9, 7)), StartText = "18:00", EndText = "20:00" };

        vm.SaveCommand.Execute(null);
        Assert.IsNull(vm.Result);
        StringAssert.Contains(vm.Error, "opis");

        vm.Description = "Ispravka greške";
        vm.StartText = "20:00";
        vm.SaveCommand.Execute(null);
        StringAssert.Contains(vm.Error, "isti");
        Assert.IsNull(vm.Result);

        vm.StartText = "18:00";
        vm.SaveCommand.Execute(null);
        Assert.AreEqual(new OvertimeRequest(new DateOnly(2026, 9, 7), new TimeOnly(18, 0), new TimeOnly(20, 0), "Ispravka greške"), vm.Result);
    }

    [TestMethod]
    public async Task AbsenceDialog_ShouldPreviewBeforeItCanBeSaved()
    {
        var calls = 0;
        var vm = new AbsenceViewModel(2026, 9, request =>
        {
            calls++;
            return Task.FromResult(new AbsencePreviewDto(request.Code, new[] { new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 2) },
                new[] { new SkippedDayDto(new DateOnly(2026, 9, 5), "Weekend") }));
        });

        vm.From = new DateTimeOffset(new DateTime(2026, 9, 1));
        vm.To = new DateTimeOffset(new DateTime(2026, 9, 6));
        Assert.IsFalse(vm.SaveCommand.CanExecute(null), "nothing can be saved before the days were shown");

        await vm.ShowPreviewCommand.ExecuteAsync(null);
        Assert.AreEqual(1, calls);
        StringAssert.Contains(vm.PreviewText, "2 dana");
        StringAssert.Contains(vm.PreviewText, "vikend");
        Assert.IsTrue(vm.SaveCommand.CanExecute(null));

        vm.To = new DateTimeOffset(new DateTime(2026, 9, 10));
        Assert.IsFalse(vm.SaveCommand.CanExecute(null), "changing the range asks for a new preview");

        await vm.ShowPreviewCommand.ExecuteAsync(null);
        vm.SaveCommand.Execute(null);
        Assert.AreEqual(1022, vm.Result!.Code);
        Assert.AreEqual(new DateOnly(2026, 9, 10), vm.Result.To);
    }

    [TestMethod]
    public async Task AbsenceDialog_ShouldRefuseARangeThatEndsBeforeItStarts()
    {
        var vm = new AbsenceViewModel(2026, 9, _ => throw new InvalidOperationException("must not be called"))
        {
            From = new DateTimeOffset(new DateTime(2026, 9, 10)),
            To = new DateTimeOffset(new DateTime(2026, 9, 5))
        };

        await vm.ShowPreviewCommand.ExecuteAsync(null);
        StringAssert.Contains(vm.Error, "Kraj");
        Assert.IsFalse(vm.SaveCommand.CanExecute(null));
    }

    [TestMethod]
    public void DayRows_ShouldShowEntriesOvertimeAndFindings()
    {
        var day = new DayDto(new DateOnly(2026, 9, 5), "S", "Weekend", null,
            new[] { new EntryDto(1022, 0) }, new[] { new AllocationDto(new DateOnly(2026, 9, 5), 2023, 150) });
        var row = new EmployeeDayRow(day, new[] { new IssueDto("V-02", "Blocking", new DateOnly(2026, 9, 5), "Godišnji odmor na vikend") });

        Assert.AreEqual("05.09.", row.DateText);
        Assert.AreEqual("vikend", row.TypeText);
        Assert.AreEqual("1022 Godišnji odmor A", row.EntriesText);
        StringAssert.Contains(row.OvertimeText, "2023");
        StringAssert.Contains(row.OvertimeText, "2,5 h");
        Assert.AreEqual(1, row.IssueCount);
        Assert.IsTrue(row.HasBlocking);
        StringAssert.Contains(row.IssueTooltip, "V-02");
        Assert.IsFalse(row.IsWorkingDay);
    }
}
