namespace Timesheet.Domain;

/// <summary>Colour group of the legend; it decides what a code means for the monthly fund of hours</summary>
public enum CodeColour
{
    /// <summary>Regular hours and whole-day absences (in the fund)</summary>
    Green,

    /// <summary>State holiday hours not worked (in the fund)</summary>
    Blue,

    /// <summary>Regular work on a holiday and night shift work (in the fund)</summary>
    Orange,

    /// <summary>Overtime (never in the fund)</summary>
    Red
}

public enum CodeUnit
{
    /// <summary>The entry is a number of hours</summary>
    Hours,

    /// <summary>The entry is the code itself for a whole day; a day counts as the daily norm (8 h)</summary>
    Day
}

/// <summary>One category of the legend of the sheet</summary>
public sealed record TimeCode(int Code, string NameBs, string NameEn, CodeColour Colour, CodeUnit Unit)
{
    public bool IsOvertime => Colour == CodeColour.Red;

    /// <summary>Green, blue and orange codes form the monthly fund; overtime does not</summary>
    public bool InMonthlyFund => !IsOvertime;

    /// <summary>A whole-day code other than regular work: leave, sick leave, business trip, religious holiday</summary>
    public bool IsAbsence => Unit == CodeUnit.Day;

    /// <summary>
    /// The entry that fills a working day: regular hours (1010) or an absence code. A day has at most one
    /// (the absence codes are typed into the regular-work row of the sheet).
    /// </summary>
    public bool IsBaseEntry => Code == Codes.RegularWork || IsAbsence;

    public override string ToString() => $"{Code} {NameBs}";
}

/// <summary>The numbers of the legend, so that rules read as words</summary>
public static class Codes
{
    public const int RegularWork = 1010;
    public const int PaidLeave = 1020;
    public const int UnpaidLeave = 1021;
    public const int AnnualLeaveA = 1022;
    public const int AnnualLeaveB = 1023;
    public const int StateHoliday = 1024;
    public const int ReligiousHoliday = 1025;
    public const int BusinessTripDomestic = 1027;
    public const int BusinessTripAbroad = 1028;
    public const int BusinessTripNoPerDiem = 1029;
    public const int SickUpTo42Days = 1030;
    public const int SickOver42Days = 1031;
    public const int MaternityLeave = 1040;
    public const int HolidayWork = 2010;
    public const int RegularNightWork = 2011;
    public const int HolidayNightWork = 2012;
    public const int Overtime = 2020;
    public const int NightOvertime = 2021;
    public const int HolidayOvertime = 2022;
    public const int WeekendOvertime = 2023;
    public const int NightWeekendOvertime = 2024;
    public const int NightHolidayOvertime = 2025;

    public static bool IsAnnualLeave(int code) => code is AnnualLeaveA or AnnualLeaveB;

    public static bool IsBusinessTrip(int code) => code is BusinessTripDomestic or BusinessTripAbroad or BusinessTripNoPerDiem;

    public static bool IsSickLeave(int code) => code is SickUpTo42Days or SickOver42Days;
}

/// <summary>The catalogue of the 22 codes of the legend (the analysis says 20; the legend of the sheet lists 22)</summary>
public static class TimeCodes
{
    public static IReadOnlyList<TimeCode> All { get; } = new[]
    {
        new TimeCode(Codes.RegularWork, "Redovan rad", "Regular work", CodeColour.Green, CodeUnit.Hours),
        new TimeCode(Codes.PaidLeave, "Plaćeno odsustvo", "Paid leave", CodeColour.Green, CodeUnit.Day),
        new TimeCode(Codes.UnpaidLeave, "Neplaćeno odsustvo", "Unpaid leave", CodeColour.Green, CodeUnit.Day),
        new TimeCode(Codes.AnnualLeaveA, "Godišnji odmor A", "Annual leave A", CodeColour.Green, CodeUnit.Day),
        new TimeCode(Codes.AnnualLeaveB, "Godišnji odmor B", "Annual leave B", CodeColour.Green, CodeUnit.Day),
        new TimeCode(Codes.StateHoliday, "Državni praznik", "State holiday (not worked)", CodeColour.Blue, CodeUnit.Hours),
        new TimeCode(Codes.ReligiousHoliday, "Vjerski praznik", "Religious holiday", CodeColour.Green, CodeUnit.Day),
        new TimeCode(Codes.BusinessTripDomestic, "Službeni put – u zemlji", "Business trip, domestic", CodeColour.Green, CodeUnit.Day),
        new TimeCode(Codes.BusinessTripAbroad, "Službeni put – izvan zemlje", "Business trip, abroad", CodeColour.Green, CodeUnit.Day),
        new TimeCode(Codes.BusinessTripNoPerDiem, "Službeni put – bez isplate dnevnice", "Business trip without per diem", CodeColour.Green, CodeUnit.Day),
        new TimeCode(Codes.SickUpTo42Days, "Bolovanje do 42 dana", "Sick leave up to 42 days", CodeColour.Green, CodeUnit.Day),
        new TimeCode(Codes.SickOver42Days, "Bolovanje preko 42 dana", "Sick leave over 42 days", CodeColour.Green, CodeUnit.Day),
        new TimeCode(Codes.MaternityLeave, "Porodiljsko odsustvo", "Maternity leave", CodeColour.Green, CodeUnit.Day),
        new TimeCode(Codes.HolidayWork, "Rad praznikom", "Work on a holiday", CodeColour.Orange, CodeUnit.Hours),
        new TimeCode(Codes.RegularNightWork, "Redovan noćni rad", "Regular night work", CodeColour.Orange, CodeUnit.Hours),
        new TimeCode(Codes.HolidayNightWork, "Rad praznikom noćni", "Night work on a holiday", CodeColour.Orange, CodeUnit.Hours),
        new TimeCode(Codes.Overtime, "Prekovremeni rad", "Overtime", CodeColour.Red, CodeUnit.Hours),
        new TimeCode(Codes.NightOvertime, "Prekovremeni noćni rad", "Night overtime", CodeColour.Red, CodeUnit.Hours),
        new TimeCode(Codes.HolidayOvertime, "Prekovremeni rad praznikom", "Overtime on a holiday", CodeColour.Red, CodeUnit.Hours),
        new TimeCode(Codes.WeekendOvertime, "Prekovremeni rad vikendom", "Weekend overtime", CodeColour.Red, CodeUnit.Hours),
        new TimeCode(Codes.NightWeekendOvertime, "Prekovremeni noćni rad vikendom", "Night weekend overtime", CodeColour.Red, CodeUnit.Hours),
        new TimeCode(Codes.NightHolidayOvertime, "Prekovremeni noćni rad praznikom", "Night holiday overtime", CodeColour.Red, CodeUnit.Hours),
    };

    private static readonly Dictionary<int, TimeCode> ByCode = All.ToDictionary(c => c.Code);

    public static bool TryGet(int code, out TimeCode timeCode) => ByCode.TryGetValue(code, out timeCode!);

    public static TimeCode Get(int code) =>
        ByCode.TryGetValue(code, out var found) ? found : throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown time code");

    public static bool Exists(int code) => ByCode.ContainsKey(code);
}
