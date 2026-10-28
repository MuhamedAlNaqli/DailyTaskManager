using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Avalonia.ViewModels;

/// <summary>A selectable time range on the Analytics page</summary>
public class PeriodChoice
{
    public PeriodChoice(string name, Func<DateTime, (DateTime? From, DateTime? To)> range)
    {
        Name = name;
        Range = range;
    }

    public string Name { get; }

    public Func<DateTime, (DateTime? From, DateTime? To)> Range { get; }

    public override string ToString() => Name;
}

/// <summary>One bar of the hours chart</summary>
public class ChartBar
{
    public string Label { get; init; } = "";

    public string SubLabel { get; init; } = "";

    public string Tooltip { get; init; } = "";

    public double BarHeight { get; init; }

    public double BarWidth { get; init; }

    public double SlotWidth { get; init; }

    public IBrush Brush { get; init; } = Brushes.Gray;
}

/// <summary>One line of the per-client breakdown</summary>
public class ClientRow
{
    public string Name { get; init; } = "";

    public string HoursText { get; init; } = "";

    public string ShareText { get; init; } = "";

    public double Share { get; init; }

    public string EntriesText { get; init; } = "";
}

public partial class AnalyticsViewModel : ObservableObject, IDisposable
{
    private const double ChartHeight = 160;
    private const decimal DailyTarget = 7m;

    private static readonly IBrush Green = Brush.Parse("#FF10B981");
    private static readonly IBrush Amber = Brush.Parse("#FFF59E0B");
    private static readonly IBrush Red = Brush.Parse("#FFEF4444");
    private static readonly IBrush Blue = Brush.Parse("#FF3B82F6");
    private static readonly IBrush Muted = Brush.Parse("#33808080");

    private readonly IDatabaseService _databaseService;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AnalyticsViewModel> _logger;
    private bool _initializing = true;
    private int _loadVersion;

    public ObservableCollection<PeriodChoice> Periods { get; } = new()
    {
        new("This month", today => (new DateTime(today.Year, today.Month, 1), new DateTime(today.Year, today.Month, 1).AddMonths(1).AddDays(-1))),
        new("Last month", today => (new DateTime(today.Year, today.Month, 1).AddMonths(-1), new DateTime(today.Year, today.Month, 1).AddDays(-1))),
        new("Last 30 days", today => (today.AddDays(-29), today)),
        new("Last 3 months", today => (new DateTime(today.Year, today.Month, 1).AddMonths(-2), new DateTime(today.Year, today.Month, 1).AddMonths(1).AddDays(-1))),
        new("This year", today => (new DateTime(today.Year, 1, 1), new DateTime(today.Year, 12, 31))),
        new("All time", _ => (null, null)),
    };

    public ObservableCollection<ClientChoice> ClientOptions { get; } = new() { ClientChoice.All, ClientChoice.Unassigned };

    public ObservableCollection<ChartBar> Bars { get; } = new();

    public ObservableCollection<ClientRow> ClientRows { get; } = new();

    [ObservableProperty]
    private PeriodChoice _selectedPeriod = null!;

    [ObservableProperty]
    private ClientChoice? _selectedClient = ClientChoice.All;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasData;

    [ObservableProperty]
    private string _totalHoursText = "0.0h";

    [ObservableProperty]
    private string _entriesText = "0 entries";

    [ObservableProperty]
    private string _daysWorkedText = "0";

    [ObservableProperty]
    private string _averageText = "0.0h";

    [ObservableProperty]
    private string _targetText = "-";

    [ObservableProperty]
    private string _targetDetail = "";

    [ObservableProperty]
    private bool _targetBehind;

    [ObservableProperty]
    private string _chartTitle = "Hours";

    [ObservableProperty]
    private bool _isDailyChart = true;

    public AnalyticsViewModel(IDatabaseService databaseService, IAuthenticationService authService, ILogger<AnalyticsViewModel> logger)
    {
        _databaseService = databaseService;
        _authService = authService;
        _logger = logger;
        _selectedPeriod = Periods[0];
        _initializing = false;

        _ = LoadAsync();
    }

    public void Dispose()
    {
        Interlocked.Increment(ref _loadVersion);
    }

    partial void OnSelectedPeriodChanged(PeriodChoice value)
    {
        if (!_initializing) _ = LoadAsync();
    }

    partial void OnSelectedClientChanged(ClientChoice? value)
    {
        if (!_initializing) _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var version = Interlocked.Increment(ref _loadVersion);
        try
        {
            if (!_authService.IsAuthenticated) return;
            IsLoading = true;

            var today = DateTime.Today;
            var (from, to) = SelectedPeriod.Range(today);
            var query = new TaskQuery(null, from, to, SelectedClient?.Id);

            var daysTask = _databaseService.GetDailyHoursAsync(query);
            var clientsTask = _databaseService.GetClientHoursAsync(query);
            var clientListTask = _databaseService.GetClientsAsync();
            await Task.WhenAll(daysTask, clientsTask, clientListTask);
            if (version != _loadVersion) return;

            RebuildClientOptions(clientListTask.Result);
            Apply(daysTask.Result.Select(d => new DayHours { Date = d.Date.Date, Hours = d.Hours, Entries = d.Entries }).ToList(),
                  clientsTask.Result, from, to, today);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading analytics");
        }
        finally
        {
            if (version == _loadVersion) IsLoading = false;
        }
    }

    private void RebuildClientOptions(List<Client> clients)
    {
        // Only rebuild when the set of clients changed, so the selection is not disturbed
        var existing = ClientOptions.Skip(2).Select(c => c.Id).ToList();
        if (existing.SequenceEqual(clients.Select(c => (int?)c.Id))) return;

        var selectedId = SelectedClient?.Id;
        _initializing = true;
        try
        {
            while (ClientOptions.Count > 2) ClientOptions.RemoveAt(2);
            foreach (var client in clients) ClientOptions.Add(ClientChoice.From(client));
            SelectedClient = ClientOptions.FirstOrDefault(c => c.Id == selectedId) ?? ClientChoice.All;
        }
        finally
        {
            _initializing = false;
        }
    }

    private void Apply(List<DayHours> days, List<ClientHours> clients, DateTime? from, DateTime? to, DateTime today)
    {
        var total = days.Sum(d => d.Hours);
        var entries = days.Sum(d => d.Entries);
        var worked = days.Count(d => d.Hours > 0);

        HasData = days.Count > 0;
        TotalHoursText = $"{total:0.0}h";
        EntriesText = entries == 1 ? "1 entry" : $"{entries} entries";
        DaysWorkedText = worked.ToString(CultureInfo.InvariantCulture);
        AverageText = worked > 0 ? $"{total / worked:0.0}h" : "0.0h";

        // Range used for charts and the target: bounded by the data for "all time"
        var start = from ?? (days.Count > 0 ? days.Min(d => d.Date) : today);
        var end = to ?? (days.Count > 0 ? days.Max(d => d.Date) : today);

        UpdateTarget(total, start, end, today);

        var byDate = days.ToDictionary(d => d.Date);
        var daily = (end - start).TotalDays <= 62;
        IsDailyChart = daily;

        var bars = daily ? BuildDailyBars(byDate, start, end, today) : BuildMonthlyBars(days, start, end);
        Bars.Clear();
        foreach (var bar in bars) Bars.Add(bar);
        ChartTitle = daily ? "Hours per day" : "Hours per month";

        ClientRows.Clear();
        foreach (var c in clients)
        {
            var share = total > 0 ? (double)(c.Hours / total) : 0;
            ClientRows.Add(new ClientRow
            {
                Name = c.ClientName,
                HoursText = $"{c.Hours:0.0}h",
                ShareText = $"{share * 100:0}%",
                Share = share * 100,
                EntriesText = c.Entries == 1 ? "1 entry" : $"{c.Entries} entries"
            });
        }
    }

    private void UpdateTarget(decimal total, DateTime start, DateTime end, DateTime today)
    {
        // Expected hours: weekdays up to today (or the end of the period, if it is over) times the daily target
        var last = end < today ? end : today;
        var weekdays = 0;
        for (var d = start; d <= last; d = d.AddDays(1))
        {
            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday) weekdays++;
        }

        var target = weekdays * DailyTarget;
        var diff = total - target;
        TargetText = $"{target:0}h";
        TargetBehind = diff < 0;
        TargetDetail = target == 0
            ? "No working days yet"
            : diff >= 0 ? $"{diff:0.0}h ahead of {weekdays} working days" : $"{-diff:0.0}h behind over {weekdays} working days";
    }

    private static List<ChartBar> BuildDailyBars(Dictionary<DateTime, DayHours> byDate, DateTime start, DateTime end, DateTime today)
    {
        var count = (int)(end - start).TotalDays + 1;
        var max = Math.Max(8m, byDate.Count > 0 ? byDate.Values.Max(d => d.Hours) : 0m);
        var slot = Math.Clamp(780.0 / count, 12, 44);
        var bars = new List<ChartBar>(count);

        for (var d = start; d <= end; d = d.AddDays(1))
        {
            byDate.TryGetValue(d, out var row);
            var hours = row?.Hours ?? 0m;
            var weekend = d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday;

            IBrush brush;
            string state;
            if (hours >= DailyTarget) { brush = Green; state = "target reached"; }
            else if (hours > 0) { brush = Amber; state = "below target"; }
            else if (d > today || weekend) { brush = Muted; state = weekend ? "weekend" : "upcoming"; }
            else { brush = Red; state = "no entry"; }

            bars.Add(new ChartBar
            {
                Label = d.Day.ToString(CultureInfo.InvariantCulture),
                SubLabel = "",
                Tooltip = $"{d:ddd d MMM yyyy}: {hours:0.0}h ({state})" + (row != null ? $", {row.Entries} entr{(row.Entries == 1 ? "y" : "ies")}" : ""),
                BarHeight = hours > 0 ? Math.Max(4, (double)(hours / max) * ChartHeight) : 3,
                BarWidth = slot - 6,
                SlotWidth = slot,
                Brush = brush
            });
        }

        return bars;
    }

    private static List<ChartBar> BuildMonthlyBars(List<DayHours> days, DateTime start, DateTime end)
    {
        var totals = days.GroupBy(d => new DateTime(d.Date.Year, d.Date.Month, 1))
            .ToDictionary(g => g.Key, g => (Hours: g.Sum(x => x.Hours), Days: g.Count(x => x.Hours > 0)));

        var first = new DateTime(start.Year, start.Month, 1);
        var last = new DateTime(end.Year, end.Month, 1);
        var months = new List<DateTime>();
        for (var m = first; m <= last; m = m.AddMonths(1)) months.Add(m);

        var max = Math.Max(1m, totals.Count > 0 ? totals.Values.Max(v => v.Hours) : 0m);
        var slot = Math.Clamp(780.0 / Math.Max(1, months.Count), 36, 90);

        return months.Select(m =>
        {
            totals.TryGetValue(m, out var v);
            return new ChartBar
            {
                Label = m.ToString("MMM yy", CultureInfo.InvariantCulture),
                SubLabel = v.Hours > 0 ? $"{v.Hours:0}h" : "",
                Tooltip = $"{m:MMMM yyyy}: {v.Hours:0.0}h over {v.Days} day{(v.Days == 1 ? "" : "s")}",
                BarHeight = v.Hours > 0 ? Math.Max(4, (double)(v.Hours / max) * ChartHeight) : 3,
                BarWidth = slot - 14,
                SlotWidth = slot,
                Brush = v.Hours > 0 ? Blue : Muted
            };
        }).ToList();
    }
}
