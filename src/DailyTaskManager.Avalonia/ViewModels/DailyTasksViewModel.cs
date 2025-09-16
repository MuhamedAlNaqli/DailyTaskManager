using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Models;
using DailyTaskManager.Avalonia.Services;

namespace DailyTaskManager.Avalonia.ViewModels;

public partial class DailyTasksViewModel : ObservableObject
{
    private readonly IDatabaseService _databaseService;
    private readonly IExportService _exportService;
    private readonly IAuthenticationService _authService;
    private readonly DialogService _dialogService;
    private readonly ILogger<DailyTasksViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<DailyTask> _tasks = new();
    
    [ObservableProperty]
    private ObservableCollection<DailyTask> _filteredTasks = new();

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

    public DailyTasksViewModel(
        IDatabaseService databaseService, 
        IExportService exportService,
        IAuthenticationService authService,
        DialogService dialogService,
        ILogger<DailyTasksViewModel> logger)
    {
        _databaseService = databaseService;
        _exportService = exportService;
        _authService = authService;
        _dialogService = dialogService;
        _logger = logger;
        
        // Subscribe to authentication changes
        _authService.AuthenticationChanged += OnAuthenticationChanged;
        
        // Always attempt to load tasks - LoadTasksAsync handles authentication check
        _ = LoadTasksAsync();
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
            Tasks.Clear();
        }
    }

    [RelayCommand]
    private async Task LoadTasksAsync()
    {
        try
        {
            // Check if user is authenticated first
            if (!_authService.IsAuthenticated)
            {
                _logger.LogInformation("User not authenticated, skipping task loading");
                Tasks.Clear();
                ApplyFilters();
                return;
            }

            IsLoading = true;
            _logger.LogInformation("Loading tasks for authenticated user...");

            var tasks = await _databaseService.GetTasksAsync();
            Tasks = new ObservableCollection<DailyTask>(tasks);
            ApplyFilters();

            _logger.LogInformation("Loaded {Count} tasks", tasks.Count);
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
            IsLoading = false;
        }
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
            if (!FilteredTasks.Any())
            {
                await _dialogService.ShowInfoDialogAsync("No Data", "No tasks available to export.");
                return;
            }

            IsLoading = true;
            var filePath = await _exportService.ExportTasksToExcelAsync(FilteredTasks.ToList());
            
            await _dialogService.ShowInfoDialogAsync("Success", $"Tasks exported successfully to:\n{filePath}");
            
            _logger.LogInformation("Exported {Count} tasks to Excel", FilteredTasks.Count);
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
            if (!FilteredTasks.Any())
            {
                await _dialogService.ShowInfoDialogAsync("No Data", "No tasks available to export.");
                return;
            }

            IsLoading = true;
            
            // Export tasks to PDF with default settings
            var filePath = await _exportService.ExportTasksToPdfAsync(FilteredTasks.ToList());
            
            await _dialogService.ShowInfoDialogAsync("Export Complete", $"PDF exported successfully to:\n{filePath}");
            
            _logger.LogInformation("PDF export completed successfully for {Count} tasks: {FilePath}", FilteredTasks.Count, filePath);
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
    private async Task RefreshAsync()
    {
        await LoadTasksAsync();
    }
    
    
    public void ApplyFilters()
    {
        _logger.LogInformation("ApplyFilters called. Total tasks: {TotalTasks}, DateFrom: {DateFrom}, DateTo: {DateTo}", 
            Tasks.Count, DateFrom, DateTo);
            
        var filtered = Tasks.Where(FilterTask).ToList();
        
        // Clear and repopulate instead of replacing the entire collection
        // This ensures proper UI notification
        FilteredTasks.Clear();
        foreach (var task in filtered)
        {
            FilteredTasks.Add(task);
        }
        
        _logger.LogInformation("Filtering complete. Filtered tasks: {FilteredCount}", filtered.Count);
    }
    
    private bool FilterTask(DailyTask task)
    {
        if (task == null) return false;
        
        // Search filter
        bool matchesSearch = string.IsNullOrWhiteSpace(SearchText)
            || task.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || task.Date.ToString("d").Contains(SearchText)
            || task.Hours.ToString().Contains(SearchText);
        
        // Date range filters - compare date parts only
        bool matchesDateFrom = !DateFrom.HasValue || task.Date.Date >= DateFrom.Value.Date;
        bool matchesDateTo = !DateTo.HasValue || task.Date.Date <= DateTo.Value.Date;

        var result = matchesSearch && matchesDateFrom && matchesDateTo;

        // Debug logging for date filters specifically
        if (DateFrom.HasValue || DateTo.HasValue)
        {
            _logger.LogInformation("Task {TaskId} ({Date}): SearchMatch={SearchMatch}, DateFromMatch={DateFromMatch} (From: {DateFrom}), DateToMatch={DateToMatch} (To: {DateTo}), Result={Result}",
                task.Id, task.Date.Date, matchesSearch, matchesDateFrom, DateFrom?.Date, matchesDateTo, DateTo?.Date, result);
        }
        
        return result;
    }
    
    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = string.Empty;
        DateFrom = null;
        DateTo = null;
    }

    partial void OnSearchTextChanged(string value)
    {
        _logger.LogInformation("SearchText property changed to: '{SearchText}'", value);
        ApplyFilters();
    }

    partial void OnDateFromChanged(DateTime? value)
    {
        _logger.LogInformation("DateFrom property changed to: {DateFrom}", value);
        // Don't auto-apply filters for date changes - let user use search button for control
    }

    partial void OnDateToChanged(DateTime? value)
    {
        _logger.LogInformation("DateTo property changed to: {DateTo}", value);
        // Don't auto-apply filters for date changes - let user use search button for control
    }
}