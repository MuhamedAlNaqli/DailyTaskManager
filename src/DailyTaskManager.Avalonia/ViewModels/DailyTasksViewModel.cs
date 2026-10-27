using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Models;
using DailyTaskManager.Avalonia.Services;

namespace DailyTaskManager.Avalonia.ViewModels;

public partial class DailyTasksViewModel : ObservableObject, IDisposable
{
    private readonly IDatabaseService _databaseService;
    private readonly IExportService _exportService;
    private readonly IAuthenticationService _authService;
    private readonly ISettingsService _settingsService;
    private readonly DialogService _dialogService;
    private readonly ILogger<DailyTasksViewModel> _logger;

    public const int PageSize = 25;

    private static readonly TimeSpan SearchDebounce = TimeSpan.FromMilliseconds(250);

    private CancellationTokenSource? _searchDebounce;
    private int _loadVersion;

    /// <summary>The tasks on the current page</summary>
    [ObservableProperty]
    private ObservableCollection<DailyTask> _filteredTasks = new();

    /// <summary>Number of tasks matching the current filters, across all pages</summary>
    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageInfo), nameof(CanGoPrevious), nameof(CanGoNext))]
    private int _currentPage = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageInfo), nameof(CanGoNext))]
    private int _totalPages = 1;

    [ObservableProperty]
    private DailyTask? _selectedTask;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private DateTime? _dateFrom;

    [ObservableProperty]
    private DateTime? _dateTo;

    [ObservableProperty]
    private decimal _todayHours;

    [ObservableProperty]
    private decimal _thisWeekHours;

    [ObservableProperty]
    private decimal _dailyAverage30Days;

    public DailyTasksViewModel(
        IDatabaseService databaseService,
        IExportService exportService,
        IAuthenticationService authService,
        ISettingsService settingsService,
        DialogService dialogService,
        ILogger<DailyTasksViewModel> logger)
    {
        _databaseService = databaseService;
        _exportService = exportService;
        _authService = authService;
        _settingsService = settingsService;
        _dialogService = dialogService;
        _logger = logger;

        // Subscribe to authentication changes
        _authService.AuthenticationChanged += OnAuthenticationChanged;

        // Always attempt to load tasks - LoadTasksAsync handles authentication check
        _ = LoadTasksAsync();
    }

    public bool CanGoPrevious => CurrentPage > 1;

    public bool CanGoNext => CurrentPage < TotalPages;

    public string PageInfo => TotalCount == 0
        ? "No entries"
        : $"{(CurrentPage - 1) * PageSize + 1}-{Math.Min(CurrentPage * PageSize, TotalCount)} of {TotalCount}  (page {CurrentPage}/{TotalPages})";

    public void Dispose()
    {
        _authService.AuthenticationChanged -= OnAuthenticationChanged;
        _searchDebounce?.Cancel();
        _searchDebounce?.Dispose();
    }

    private async void OnAuthenticationChanged(object? sender, AuthResult e)
    {
        if (e.IsSuccess && _authService.IsAuthenticated)
        {
            _logger.LogInformation("Authentication completed, reloading tasks...");
            await LoadTasksAsync();
        }
        else
        {
            // User logged out, clear tasks
            Interlocked.Increment(ref _loadVersion);
            FilteredTasks = new ObservableCollection<DailyTask>();
            TotalCount = 0;
        }
    }

    [RelayCommand]
    private Task LoadTasksAsync() => LoadPageAsync(CurrentPage, showOverlay: true);

    private TaskQuery CurrentQuery => new(SearchText, DateFrom, DateTo);

    /// <summary>
    /// Loads one page (plus the header statistics) from the database. Only the latest request wins,
    /// so quick typing or paging never shows stale results.
    /// </summary>
    private async Task LoadPageAsync(int page, bool showOverlay)
    {
        var version = Interlocked.Increment(ref _loadVersion);

        try
        {
            if (!_authService.IsAuthenticated)
            {
                FilteredTasks = new ObservableCollection<DailyTask>();
                TotalCount = 0;
                return;
            }

            if (showOverlay) IsLoading = true;

            var pageTask = _databaseService.GetTasksPageAsync(CurrentQuery, page, PageSize);
            var summaryTask = _databaseService.GetTaskSummaryAsync();
            await Task.WhenAll(pageTask, summaryTask);

            if (version != _loadVersion) return; // a newer request superseded this one

            var result = pageTask.Result;
            var summary = summaryTask.Result;

            FilteredTasks = new ObservableCollection<DailyTask>(result.Items);
            TotalCount = result.TotalCount;
            TotalPages = Math.Max(1, result.TotalPages);
            CurrentPage = result.Page;
            OnPropertyChanged(nameof(PageInfo));

            TodayHours = summary.TodayHours;
            ThisWeekHours = summary.ThisWeekHours;
            DailyAverage30Days = summary.DailyAverage30Days;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tasks");
            // Only show error dialog if we're authenticated (otherwise it's expected to fail)
            if (_authService.IsAuthenticated)
            {
                await _dialogService.ShowErrorDialogAsync("Error", "Failed to load tasks. Please try again.");
            }
        }
        finally
        {
            if (showOverlay && version == _loadVersion) IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private Task PreviousPageAsync() => LoadPageAsync(CurrentPage - 1, showOverlay: false);

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private Task NextPageAsync() => LoadPageAsync(CurrentPage + 1, showOverlay: false);

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private Task FirstPageAsync() => LoadPageAsync(1, showOverlay: false);

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private Task LastPageAsync() => LoadPageAsync(TotalPages, showOverlay: false);

    partial void OnCurrentPageChanged(int value) => NotifyPagingCommands();

    partial void OnTotalPagesChanged(int value) => NotifyPagingCommands();

    private void NotifyPagingCommands()
    {
        PreviousPageCommand.NotifyCanExecuteChanged();
        FirstPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
        LastPageCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task AddNewTaskAsync()
    {
        try
        {
            var newTask = await _dialogService.ShowAddTaskDialogAsync();
            if (newTask != null)
            {
                IsLoading = true;
                await _databaseService.AddTaskAsync(newTask);
                await LoadTasksAsync();

                _logger.LogInformation("Added new task: {Description}", newTask.Description);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding new task");
            await _dialogService.ShowErrorDialogAsync("Error", "Failed to add task. Please try again.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task EditTaskAsync(DailyTask? task)
    {
        if (task == null) return;

        try
        {
            var editedTask = await _dialogService.ShowEditTaskDialogAsync(task);
            if (editedTask != null)
            {
                IsLoading = true;
                await _databaseService.UpdateTaskAsync(editedTask);
                await LoadTasksAsync();

                _logger.LogInformation("Updated task: {Description}", editedTask.Description);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing task {TaskId}", task.Id);
            await _dialogService.ShowErrorDialogAsync("Error", "Failed to update task. Please try again.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteTaskAsync(DailyTask? task)
    {
        if (task == null) return;

        try
        {
            var confirmed = await _dialogService.ShowConfirmDialogAsync(
                "Delete Task",
                $"Are you sure you want to delete '{task.Description}'?");

            if (confirmed)
            {
                IsLoading = true;
                var success = await _databaseService.RemoveTaskAsync(task.Id);

                if (success)
                {
                    await LoadTasksAsync();
                    _logger.LogInformation("Deleted task: {Description}", task.Description);
                }
                else
                {
                    await _dialogService.ShowErrorDialogAsync("Error", "Task not found or could not be deleted.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting task {TaskId}", task.Id);
            await _dialogService.ShowErrorDialogAsync("Error", "Failed to delete task. Please try again.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportToExcelAsync()
    {
        try
        {
            if (TotalCount == 0)
            {
                await _dialogService.ShowInfoDialogAsync("No Data", "No tasks available to export.");
                return;
            }

            IsLoading = true;
            var filePath = await _exportService.ExportTasksToExcelAsync(await _databaseService.GetTasksAsync(CurrentQuery));

            await _dialogService.ShowInfoDialogAsync("Success", $"Tasks exported successfully to:\n{filePath}");

            _logger.LogInformation("Exported {Count} tasks to Excel", TotalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting tasks to Excel");
            await _dialogService.ShowErrorDialogAsync("Export Error", "Failed to export tasks to Excel. Please try again.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportToPdfAsync()
    {
        try
        {
            if (TotalCount == 0)
            {
                await _dialogService.ShowInfoDialogAsync("No Data", "No tasks available to export.");
                return;
            }

            IsLoading = true;

            var pdfSettings = await _settingsService.GetPdfSettingsAsync();
            var filePath = await _exportService.ExportTasksToPdfAsync(await _databaseService.GetTasksAsync(CurrentQuery), pdfSettings);

            await _dialogService.ShowInfoDialogAsync("Export Complete", $"PDF exported successfully to:\n{filePath}");

            _logger.LogInformation("PDF export completed successfully for {Count} tasks: {FilePath}", TotalCount, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting to PDF");
            await _dialogService.ShowErrorDialogAsync("Export Error", $"Failed to export PDF: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportToWordAsync()
    {
        try
        {
            if (TotalCount == 0)
            {
                await _dialogService.ShowInfoDialogAsync("No Data", "No tasks available to export.");
                return;
            }

            IsLoading = true;

            var wordSettings = await _settingsService.GetPdfSettingsAsync();
            var filePath = await _exportService.ExportTasksToWordAsync(await _databaseService.GetTasksAsync(CurrentQuery), wordSettings);

            await _dialogService.ShowInfoDialogAsync("Export Complete", $"Word document exported successfully to:\n{filePath}");

            _logger.LogInformation("Word export completed successfully for {Count} tasks: {FilePath}", TotalCount, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting to Word");
            await _dialogService.ShowErrorDialogAsync("Export Error", $"Failed to export Word document: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadTasksAsync();
    }


    /// <summary>Re-runs the query from the first page (used by the date range Apply button)</summary>
    public void ApplyFilters() => _ = ApplyFiltersAsync();

    public Task ApplyFiltersAsync() => LoadPageAsync(1, showOverlay: false);

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = string.Empty;
        DateFrom = null;
        DateTo = null;
    }

    partial void OnSearchTextChanged(string value)
    {
        // Wait for a pause in typing, then query once instead of once per keystroke
        _searchDebounce?.Cancel();
        _searchDebounce?.Dispose();
        var cts = _searchDebounce = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(SearchDebounce, cts.Token);
                await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => ApplyFiltersAsync());
            }
            catch (OperationCanceledException)
            {
            }
        });
    }
}
