using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using Company.Api.Client;
using DailyTaskManager.Avalonia.Services;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Localization;
using Microsoft.Extensions.Logging;

namespace DailyTaskManager.Avalonia.EmployeeMode;

// What the employee view model needs from the world around it, as small interfaces: the dialogs, the saving of files and the change of
// window. The program supplies windows and the file system; a test supplies answers.

/// <summary>The questions the employee window asks the person in front of it</summary>
public interface IEmployeeDialogs
{
    /// <summary>A yes or no question</summary>
    Task<bool> ConfirmAsync(string title, string message);

    /// <summary>The dialog for a new or changed entry; null when it was cancelled</summary>
    Task<EntryResult?> EntryAsync(EntryViewModel viewModel);

    /// <summary>A text (the reason of a return, an answer); null when it was cancelled</summary>
    Task<string?> PromptAsync(TextPromptViewModel viewModel);
}

public sealed class WindowEmployeeDialogs : IEmployeeDialogs
{
    private readonly DialogService _dialogs;

    public WindowEmployeeDialogs(DialogService dialogs) => _dialogs = dialogs;

    public Task<bool> ConfirmAsync(string title, string message) => _dialogs.ShowConfirmDialogAsync(title, message);

    public Task<EntryResult?> EntryAsync(EntryViewModel viewModel) =>
        EmployeeDialogs.ShowAsync<EntryDialog, EntryResult?>(new EntryDialog { DataContext = viewModel }, viewModel);

    public Task<string?> PromptAsync(TextPromptViewModel viewModel) =>
        EmployeeDialogs.ShowAsync<TextPromptDialog, string?>(new TextPromptDialog { DataContext = viewModel }, viewModel);
}

/// <summary>What came of asking for a file and saving it: where it went, or what went wrong. Neither when the person cancelled.</summary>
public sealed record FileSaveResult(string? Path, string? Error)
{
    public static FileSaveResult Cancelled { get; } = new(null, null);
}

/// <summary>Gets a printed sheet from the server and saves it where the person says</summary>
public interface ISheetFileSaver
{
    Task<FileSaveResult> SaveAsync(string? format, Func<string, Task<DownloadedFile>> download);
}

public sealed class SheetFileSaver : ISheetFileSaver
{
    private readonly IPlatformService _platform;
    private readonly ILogger<SheetFileSaver> _logger;

    public SheetFileSaver(IPlatformService platform, ILogger<SheetFileSaver> logger)
    {
        _platform = platform;
        _logger = logger;
    }

    public async Task<FileSaveResult> SaveAsync(string? format, Func<string, Task<DownloadedFile>> download)
    {
        format = string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase) ? "pdf" : "xlsx";

        try
        {
            var file = await download(format);

            var types = format == "pdf"
                ? new Dictionary<string, List<string>> { ["PDF"] = new() { "pdf" } }
                : new Dictionary<string, List<string>> { ["Excel"] = new() { "xlsx" } };
            var path = await _platform.ShowSaveFileDialogAsync("Sačuvaj šihtaricu", file.FileName, types);
            if (path == null) return FileSaveResult.Cancelled;

            await File.WriteAllBytesAsync(path, file.Bytes);
            return new FileSaveResult(path, null);
        }
        catch (ApiException ex)
        {
            _logger.LogWarning(ex, "The printed sheet was refused ({Code})", ex.Code);
            return new FileSaveResult(null, ex.Code == "no-team" ? "Nikome nisu dodijeljeni vaši nadzor ni odobravanje." : ex.Message);
        }
        catch (Exception ex)
        {
            // A garbled answer, a full disk, a file that is open elsewhere: all the same to the person
            _logger.LogWarning(ex, "The printed sheet could not be produced or saved");
            return new FileSaveResult(null, "Datoteka nije sačuvana: " + ex.Message);
        }
    }
}

/// <summary>Leaving employee mode</summary>
public interface IEmployeeShell
{
    /// <summary>Back to the sign-in screen. The next start shows it too, until the user signs in again with "remember me".</summary>
    void SignOut();
}

public sealed class EmployeeShell : IEmployeeShell
{
    public void SignOut()
    {
        AppMode.Set(Mode.Consultant);
        Loc.Initialize();

        if (App.Current is App app && app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var old = desktop.MainWindow;
            if (App.ServiceProvider != null) Program.PrepareConsultant(App.ServiceProvider);
            var next = app.CreateLoginWindow();
            desktop.MainWindow = next;
            next.Show();
            old?.Close();
        }
    }
}

/// <summary>What is due for the person, from the server</summary>
public sealed class RemindersViewModel : ObservableObject
{
    public ObservableCollection<ReminderRow> Items { get; } = new();

    public bool HasAny => Items.Count > 0;

    public async Task RefreshAsync(ICompanyApiClient client)
    {
        var due = await client.GetRemindersAsync();
        Items.Clear();
        foreach (var item in due.Items.OrderBy(i => EmployeeFormat.ReminderOrder(i.Kind))) Items.Add(new ReminderRow(item));
        OnPropertyChanged(nameof(HasAny));
    }
}

/// <summary>The timesheets that wait for the person as an approver</summary>
public sealed class PendingListViewModel : ObservableObject
{
    private int _count;

    public ObservableCollection<PendingRow> Items { get; } = new();

    public int Count
    {
        get => _count;
        private set
        {
            if (SetProperty(ref _count, value)) OnPropertyChanged(nameof(TabText));
        }
    }

    public bool IsEmpty => Items.Count == 0;

    public string TabText => Count > 0 ? $"Za odobravanje ({Count})" : "Za odobravanje";

    public async Task RefreshAsync(ICompanyApiClient client)
    {
        var items = await client.GetPendingAsync();
        Items.Clear();
        foreach (var item in items) Items.Add(new PendingRow(item));
        Count = items.Count;
        OnPropertyChanged(nameof(IsEmpty));
    }
}
