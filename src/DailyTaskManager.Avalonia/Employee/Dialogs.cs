using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Company.Api.Contracts;
using Timesheet.Domain;

namespace DailyTaskManager.Avalonia.EmployeeMode;

/// <summary>A dialog view model: the result is set before closing; null means cancelled</summary>
public interface IDialogResult<T>
{
    event Action? RequestClose;

    T? Result { get; }
}

public static class EmployeeDialogs
{
    public static async Task<TResult?> ShowAsync<TWindow, TResult>(TWindow window, IDialogResult<TResult> viewModel) where TWindow : Window
    {
        var owner = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner == null) return default;

        viewModel.RequestClose += () => window.Close();
        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        await window.ShowDialog(owner);
        return viewModel.Result;
    }
}

/// <summary>A code the employee can enter for a day (overtime is derived from intervals, so it is not offered)</summary>
public sealed record CodeOption(TimeCode Code)
{
    public string Label => $"{Code.Code} · {Code.NameBs}";

    public bool NeedsHours => Code.Unit == CodeUnit.Hours;

    public override string ToString() => Label;
}

// ---- One day --------------------------------------------------------------------------------------------------------------

public partial class DayEntryViewModel : ObservableObject, IDialogResult<SetEntryRequest?>
{
    public DayEntryViewModel(DateOnly date, IReadOnlyList<EntryDto> current)
    {
        Title = $"Unos za {EmployeeFormat.Date(date)} ({EmployeeFormat.Weekday(date)})";
        Codes = TimeCodes.All.Where(c => !c.IsOvertime).Select(c => new CodeOption(c)).ToList();

        var existing = current.FirstOrDefault(e => TimeCodes.Get(e.Code).IsBaseEntry) ?? current.FirstOrDefault();
        SelectedCode = Codes.FirstOrDefault(c => c.Code.Code == (existing?.Code ?? Timesheet.Domain.Codes.RegularWork)) ?? Codes[0];
        HoursText = existing is { Minutes: > 0 } ? (existing.Minutes / 60m).ToString("0.##", EmployeeFormat.Bosnian) : "8";
    }

    public string Title { get; }

    public IReadOnlyList<CodeOption> Codes { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NeedsHours))]
    private CodeOption? _selectedCode;

    [ObservableProperty] private string _hoursText = "8";
    [ObservableProperty] private string _error = "";

    public bool NeedsHours => SelectedCode?.NeedsHours == true;

    public SetEntryRequest? Result { get; private set; }

    public event Action? RequestClose;

    [RelayCommand]
    private void Save()
    {
        if (SelectedCode == null) return;

        var minutes = 0;
        if (SelectedCode.NeedsHours && !EmployeeFormat.TryParseHours(HoursText, out minutes))
        {
            Error = "Upišite sate, na primjer 8, 7,5 ili 7:30 (najviše 24).";
            return;
        }

        Result = new SetEntryRequest(SelectedCode.Code.Code, minutes);
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();
}

// ---- An absence over several days -----------------------------------------------------------------------------------------

public partial class AbsenceViewModel : ObservableObject, IDialogResult<AbsenceRangeRequest?>
{
    private readonly Func<AbsenceRangeRequest, Task<AbsencePreviewDto>> _preview;
    private readonly int _year;
    private readonly int _month;

    public AbsenceViewModel(int year, int month, Func<AbsenceRangeRequest, Task<AbsencePreviewDto>> preview)
    {
        _year = year;
        _month = month;
        _preview = preview;
        Title = $"Odsustvo u {EmployeeFormat.Month(year, month)}";
        Codes = TimeCodes.All.Where(c => c.IsAbsence).Select(c => new CodeOption(c)).ToList();
        SelectedCode = Codes.FirstOrDefault(c => c.Code.Code == Timesheet.Domain.Codes.AnnualLeaveA) ?? Codes[0];

        var first = new DateTime(year, month, 1);
        From = new DateTimeOffset(first);
        To = new DateTimeOffset(first);
    }

    public string Title { get; }

    public IReadOnlyList<CodeOption> Codes { get; }

    public DateTimeOffset MinDate => new(new DateTime(_year, _month, 1));

    public DateTimeOffset MaxDate => new(new DateTime(_year, _month, DateTime.DaysInMonth(_year, _month)));

    [ObservableProperty] private CodeOption? _selectedCode;
    [ObservableProperty] private DateTimeOffset? _from;
    [ObservableProperty] private DateTimeOffset? _to;
    [ObservableProperty] private string _previewText = "";
    [ObservableProperty] private string _error = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _canSave;

    partial void OnSelectedCodeChanged(CodeOption? value) => Invalidate();

    partial void OnFromChanged(DateTimeOffset? value) => Invalidate();

    partial void OnToChanged(DateTimeOffset? value) => Invalidate();

    private void Invalidate()
    {
        CanSave = false;
        PreviewText = "";
    }

    public AbsenceRangeRequest? Result { get; private set; }

    public event Action? RequestClose;

    private AbsenceRangeRequest? Build()
    {
        if (SelectedCode == null || From == null || To == null) return null;
        return new AbsenceRangeRequest(SelectedCode.Code.Code, DateOnly.FromDateTime(From.Value.Date), DateOnly.FromDateTime(To.Value.Date));
    }

    [RelayCommand]
    private async Task ShowPreviewAsync()
    {
        Error = "";
        var request = Build();
        if (request == null) return;
        if (request.To < request.From)
        {
            Error = "Kraj ne može biti prije početka.";
            return;
        }

        try
        {
            var preview = await _preview(request);
            if (preview.Days.Count == 0)
            {
                PreviewText = "U ovom periodu nema dana na koje se šifra upisuje (vikendi i praznici se preskaču).";
                CanSave = false;
                return;
            }

            var skipped = preview.Skipped.Count == 0
                ? ""
                : "\nPreskočeno: " + string.Join(", ", preview.Skipped.Select(s => $"{EmployeeFormat.ShortDate(s.Date)} ({Reason(s.Reason)})"));
            PreviewText = $"Šifra {request.Code} bit će upisana za {preview.Days.Count} {(preview.Days.Count == 1 ? "dan" : "dana")}: " +
                          string.Join(", ", preview.Days.Select(EmployeeFormat.ShortDate)) + skipped;
            CanSave = true;
        }
        catch (Company.Api.Client.ApiException ex)
        {
            Error = ex.Message;
        }
    }

    private static string Reason(string reason) => reason switch
    {
        "Weekend" => "vikend",
        "StateHoliday" => "državni praznik",
        "ReligiousHoliday" => "vjerski praznik",
        _ => reason
    };

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        Result = Build();
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();
}

// ---- Overtime -----------------------------------------------------------------------------------------------------------------

public partial class OvertimeViewModel : ObservableObject, IDialogResult<OvertimeRequest?>
{
    private readonly Func<DateOnly, DayType> _dayTypeOf;
    private readonly int _year;
    private readonly int _month;

    public OvertimeViewModel(int year, int month, Func<DateOnly, DayType> dayTypeOf, OvertimeDto? existing)
    {
        _year = year;
        _month = month;
        _dayTypeOf = dayTypeOf;
        Title = existing == null ? "Novi prekovremeni rad" : "Izmjena prekovremenog rada";

        var date = existing?.Date ?? (DateTime.Today.Year == year && DateTime.Today.Month == month ? DateOnly.FromDateTime(DateTime.Today) : new DateOnly(year, month, 1));
        Date = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue));
        StartText = existing?.Start.ToString("HH\\:mm") ?? "17:00";
        EndText = existing?.End.ToString("HH\\:mm") ?? "19:00";
        Description = existing?.Description ?? "";
        Recalculate();
    }

    public string Title { get; }

    public DateTimeOffset MinDate => new(new DateTime(_year, _month, 1));

    public DateTimeOffset MaxDate => new(new DateTime(_year, _month, DateTime.DaysInMonth(_year, _month)));

    [ObservableProperty] private DateTimeOffset? _date;
    [ObservableProperty] private string _startText = "";
    [ObservableProperty] private string _endText = "";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private string _summaryText = "";
    [ObservableProperty] private string _warningText = "";
    [ObservableProperty] private string _error = "";

    partial void OnDateChanged(DateTimeOffset? value) => Recalculate();

    partial void OnStartTextChanged(string value) => Recalculate();

    partial void OnEndTextChanged(string value) => Recalculate();

    public OvertimeRequest? Result { get; private set; }

    public event Action? RequestClose;

    /// <summary>
    /// The total and the split into codes while the employee types, from the same rules the server uses. The server
    /// checks again; this is only a preview.
    /// </summary>
    private void Recalculate()
    {
        SummaryText = "";
        WarningText = "";
        if (Date == null || !EmployeeFormat.TryParseTime(StartText, out var start) || !EmployeeFormat.TryParseTime(EndText, out var end)) return;

        var interval = new OvertimeInterval(DateOnly.FromDateTime(Date.Value.Date), start, end);
        if (interval.TotalMinutes == 0)
        {
            WarningText = "Početak i kraj ne smiju biti isti.";
            return;
        }

        var result = OvertimeClassifier.Classify(interval, _dayTypeOf);
        var parts = result.Allocations.Select(a => $"{a.Code} {TimeCodes.Get(a.Code).NameBs}: {EmployeeFormat.Hours(a.Minutes)}" +
                                                    (a.Date != interval.Date ? $" (za {EmployeeFormat.ShortDate(a.Date)})" : ""));
        SummaryText = $"Ukupno {EmployeeFormat.Hours(result.TotalMinutes)}" + (result.Allocations.Count > 0 ? " — " + string.Join("; ", parts) : "");
        if (result.Conflicts.Count > 0)
            WarningText = "Prekovremeni rad se ne može upisati u redovno radno vrijeme (08:30–17:00) radnog dana ili praznika.";
    }

    [RelayCommand]
    private void Save()
    {
        Error = "";
        if (Date == null) { Error = "Odaberite datum."; return; }
        if (!EmployeeFormat.TryParseTime(StartText, out var start) || !EmployeeFormat.TryParseTime(EndText, out var end))
        {
            Error = "Upišite vrijeme kao 17:00.";
            return;
        }

        if (new OvertimeInterval(DateOnly.FromDateTime(Date.Value.Date), start, end).TotalMinutes == 0)
        {
            Error = "Početak i kraj ne smiju biti isti.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            Error = "Kratak opis posla je obavezan: napišite šta je tačno rađeno.";
            return;
        }

        Result = new OvertimeRequest(DateOnly.FromDateTime(Date.Value.Date), start, end, Description.Trim());
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();
}
