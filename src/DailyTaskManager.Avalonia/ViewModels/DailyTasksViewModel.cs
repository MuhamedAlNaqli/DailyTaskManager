using System;
using System.Collections.Generic;
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
    private CancellationTokenSource? _undoTimer;
    private DailyTask? _lastDeleted;
    private bool _rebuildingClients;
    private int _loadVersion;

    /// <summary>Client filter drop-down: all, unassigned, then each client</summary>
    public ObservableCollection<ClientChoice> ClientFilterOptions { get; } = new() { ClientChoice.All, ClientChoice.Unassigned };

    /// <summary>Targets for "assign client to all matching entries"</summary>
    public ObservableCollection<ClientChoice> AssignOptions { get; } = new() { ClientChoice.NoClient };

    [ObservableProperty]
    private ClientChoice? _selectedClientFilter = ClientChoice.All;

    [ObservableProperty]
    private ClientChoice? _assignTarget = ClientChoice.NoClient;

    public ObservableCollection<TaskTemplate> Templates { get; } = new();

    private CancellationTokenSource? _toastTimer;

    [ObservableProperty]
    private bool _hasTemplates;

    [ObservableProperty]
    private bool _isToastVisible;

    [ObservableProperty]
    private string _toastMessage = "";

    [ObservableProperty]
    private bool _isUndoVisible;

    [ObservableProperty]
    private string _undoMessage = "";

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
        _databaseService.TasksChanged += OnTasksChanged;

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
        _databaseService.TasksChanged -= OnTasksChanged;
        _undoTimer?.Cancel();
        _undoTimer?.Dispose();
        _toastTimer?.Cancel();
        _toastTimer?.Dispose();
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
            WeekDays = new ObservableCollection<WeekDay>();
            TotalCount = 0;
        }
    }

    [RelayCommand]
    private Task LoadTasksAsync() => LoadPageAsync(CurrentPage, showOverlay: true);

    private TaskQuery CurrentQuery => new(SearchText, DateFrom, DateTo, SelectedClientFilter?.Id);

    private int _overlayRequests;

    /// <summary>
    /// Loads one page (plus the header statistics) from the database. Only the latest request wins,
    /// so quick typing or paging never shows stale results.
    /// </summary>
    private async Task LoadPageAsync(int page, bool showOverlay)
    {
        var version = Interlocked.Increment(ref _loadVersion);
        var overlayCounted = false;

        try
        {
            if (!_authService.IsAuthenticated)
            {
                FilteredTasks = new ObservableCollection<DailyTask>();
                TotalCount = 0;
                return;
            }

            if (showOverlay)
            {
                _overlayRequests++;
                overlayCounted = true;
                IsLoading = true;
            }

            var pageTask = _databaseService.GetTasksPageAsync(CurrentQuery, page, PageSize);
            var summaryTask = _databaseService.GetTaskSummaryAsync();
            // Full reloads (start, add, edit, delete) also refresh the client lists, as they may have changed
            var clientsTask = showOverlay ? _databaseService.GetClientsAsync() : Task.FromResult<List<Client>?>(null);
            var templatesTask = showOverlay ? _databaseService.GetTemplatesAsync() : Task.FromResult<List<TaskTemplate>?>(null);
            await Task.WhenAll(pageTask, summaryTask, clientsTask, templatesTask);

            if (version != _loadVersion) return; // a newer request superseded this one

            var result = pageTask.Result;
            var summary = summaryTask.Result;
            if (clientsTask.Result != null) RebuildClientOptions(clientsTask.Result);
            if (templatesTask.Result != null)
            {
                Templates.Clear();
                foreach (var template in templatesTask.Result) Templates.Add(template);
                HasTemplates = Templates.Count > 0;
            }

            FilteredTasks = new ObservableCollection<DailyTask>(result.Items);
            TotalCount = result.TotalCount;
            TotalPages = Math.Max(1, result.TotalPages);
            CurrentPage = result.Page;
            OnPropertyChanged(nameof(PageInfo));

            TodayHours = summary.TodayHours;
            ThisWeekHours = summary.ThisWeekHours;
            DailyAverage30Days = summary.DailyAverage30Days;

            // The week view shows the same entries from another angle, so it reloads with every full or quiet refresh
            await RefreshWeekAsync();
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
            // A superseded request must still release the overlay it raised, or a quiet reload that replaces it leaves it stuck
            if (overlayCounted && --_overlayRequests <= 0)
            {
                _overlayRequests = 0;
                IsLoading = false;
            }
        }
    }

    private void OnTasksChanged(object? sender, EventArgs e)
    {
        // Entries changed elsewhere (calendar, reminder banner...): refresh the current page quietly
        try
        {
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() => _ = LoadPageAsync(CurrentPage, showOverlay: false));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not schedule a refresh");
        }
    }

    /// <summary>
    /// Saves a change made directly in the list (description and/or hours); returns false when the change was rejected
    /// </summary>
    public async Task<bool> UpdateInlineAsync(DailyTask task, string? description, decimal? hours)
    {
        try
        {
            var newDescription = description == null ? task.Description : description.Trim();
            var newHours = hours ?? task.Hours;

            if (newDescription.Length == 0)
            {
                _ = ShowToastAsync("The description can't be empty");
                return false;
            }

            if (newDescription.Length > 1000)
            {
                _ = ShowToastAsync("The description is too long (max 1000 characters)");
                return false;
            }

            if (newHours <= 0 || newHours > 24)
            {
                _ = ShowToastAsync("Hours must be between 0 and 24");
                return false;
            }

            if (newDescription == task.Description && newHours == task.Hours) return true;

            await _databaseService.UpdateTaskAsync(new DailyTask
            {
                Id = task.Id,
                UserId = task.UserId,
                Date = task.Date,
                Description = newDescription,
                Hours = newHours,
                ClientId = task.ClientId,
                CreatedAt = task.CreatedAt
            });
            await LoadTasksAsync();
            _ = ShowToastAsync("Saved");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving inline edit of task {TaskId}", task.Id);
            await _dialogService.ShowErrorDialogAsync("Error", "Failed to save the change. Please try again.");
            return false;
        }
    }

    [RelayCommand]
    private async Task QuickAddTemplateAsync(TaskTemplate? template)
    {
        if (template == null) return;

        try
        {
            await _databaseService.AddTaskAsync(new DailyTask
            {
                Date = DateTime.Today,
                Description = template.Description,
                Hours = template.Hours,
                ClientId = template.ClientId
            });
            await LoadTasksAsync();
            _ = ShowToastAsync($"Added \"{template.Name}\" for today");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding entry from template {TemplateId}", template.Id);
            await _dialogService.ShowErrorDialogAsync("Error", "Failed to add the entry. Please try again.");
        }
    }

    [RelayCommand]
    private async Task DeleteTemplateAsync(TaskTemplate? template)
    {
        if (template == null) return;

        try
        {
            if (await _databaseService.DeleteTemplateAsync(template.Id))
            {
                Templates.Remove(template);
                HasTemplates = Templates.Count > 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {TemplateId}", template.Id);
            await _dialogService.ShowErrorDialogAsync("Error", "Failed to delete the template.");
        }
    }

    private async Task ShowToastAsync(string message)
    {
        ToastMessage = message;
        IsToastVisible = true;

        _toastTimer?.Cancel();
        var cts = _toastTimer = new CancellationTokenSource();
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(4), cts.Token);
            IsToastVisible = false;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void RebuildClientOptions(List<Client> clients)
    {
        var filterId = SelectedClientFilter?.Id;
        var assignId = AssignTarget?.Id;
        _rebuildingClients = true;
        try
        {
            while (ClientFilterOptions.Count > 2) ClientFilterOptions.RemoveAt(2);
            while (AssignOptions.Count > 1) AssignOptions.RemoveAt(1);
            foreach (var client in clients)
            {
                ClientFilterOptions.Add(ClientChoice.From(client));
                AssignOptions.Add(ClientChoice.From(client));
            }

            SelectedClientFilter = ClientFilterOptions.FirstOrDefault(c => c.Id == filterId) ?? ClientChoice.All;
            AssignTarget = AssignOptions.FirstOrDefault(c => c.Id == assignId) ?? ClientChoice.NoClient;
        }
        finally
        {
            _rebuildingClients = false;
        }
    }

    partial void OnSelectedClientFilterChanged(ClientChoice? value)
    {
        if (!_rebuildingClients) ApplyFilters();
    }

    [RelayCommand]
    private async Task AssignClientToFilteredAsync()
    {
        if (AssignTarget == null || TotalCount == 0) return;

        try
        {
            var target = AssignTarget;
            var what = target.Id.HasValue ? $"assign client '{target.Name}' to" : "remove the client from";
            var confirmed = await _dialogService.ShowConfirmDialogAsync(
                "Assign client", $"Do you want to {what} all {TotalCount} entries matching the current filters?");
            if (!confirmed) return;

            IsLoading = true;
            var changed = await _databaseService.AssignClientAsync(CurrentQuery, target.Id);
            _logger.LogInformation("Bulk client assignment changed {Count} entries", changed);
            await LoadTasksAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning client");
            await _dialogService.ShowErrorDialogAsync("Error", "Failed to assign the client. Please try again.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DuplicateTaskAsync(DailyTask? task)
    {
        if (task == null) return;

        try
        {
            var copy = await _dialogService.ShowAddTaskDialogAsync(task);
            if (copy != null)
            {
                IsLoading = true;
                await _databaseService.AddTaskAsync(copy);
                await LoadTasksAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating task {TaskId}", task.Id);
            await _dialogService.ShowErrorDialogAsync("Error", "Failed to duplicate the task. Please try again.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task UndoDeleteAsync()
    {
        var task = _lastDeleted;
        if (task == null) return;

        try
        {
            _lastDeleted = null;
            IsUndoVisible = false;
            _undoTimer?.Cancel();
            await _databaseService.RestoreTaskAsync(task);
            await LoadTasksAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring deleted task");
            await _dialogService.ShowErrorDialogAsync("Error", "Failed to restore the task.");
        }
    }

    private async Task ShowUndoAsync(DailyTask deleted)
    {
        _lastDeleted = deleted;
        var text = deleted.Description.Replace("\r", " ").Replace("\n", " ").Trim();
        UndoMessage = $"Deleted \"{(text.Length > 50 ? text.Substring(0, 50) + "..." : text)}\"";
        IsUndoVisible = true;

        _undoTimer?.Cancel();
        var cts = _undoTimer = new CancellationTokenSource();
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(8), cts.Token);
            IsUndoVisible = false;
            _lastDeleted = null;
        }
        catch (OperationCanceledException)
        {
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
            // No confirmation: the entry can be restored from the undo bar
            IsLoading = true;
            var success = await _databaseService.RemoveTaskAsync(task.Id);

            if (success)
            {
                await LoadTasksAsync();
                _logger.LogInformation("Deleted task: {Description}", task.Description);
                _ = ShowUndoAsync(task);
            }
            else
            {
                await _dialogService.ShowErrorDialogAsync("Error", "Task not found or could not be deleted.");
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
