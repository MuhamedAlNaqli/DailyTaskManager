using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Avalonia.ViewModels;

public partial class EditTaskViewModel : ObservableObject
{
    private readonly ILogger<EditTaskViewModel> _logger;
    private DailyTask? _originalTask;

    [ObservableProperty]
    private DateTime _date = DateTime.Today;

    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private decimal _hours = 7;

    [ObservableProperty]
    private bool _isLoading = false;
    
    [ObservableProperty]
    private string _validationMessage = "";

    [ObservableProperty]
    private bool _hasValidationError = false;

    public event EventHandler<bool>? RequestClose;

    public EditTaskViewModel(ILogger<EditTaskViewModel> logger)
    {
        _logger = logger;
    }

    public void LoadTask(DailyTask task)
    {
        _originalTask = task;
        Date = task.Date;
        Description = task.Description;
        Hours = task.Hours;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            if (!ValidateInput())
                return;

            IsLoading = true;
            _logger.LogInformation("Updating task: {Description}", Description);

            // Simulate save operation
            await Task.Delay(500);

            _logger.LogInformation("Task updated successfully");
            RequestClose?.Invoke(this, true); // Dialog result = true
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task");
            ShowValidationError("Failed to update task. Please try again.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(this, false); // Dialog result = false
    }

    private bool ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(Description))
        {
            ShowValidationError("Description is required.");
            return false;
        }

        if (Hours <= 0 || Hours > 24)
        {
            ShowValidationError("Hours must be between 0 and 24.");
            return false;
        }

        ClearValidationError();
        return true;
    }

    private void ShowValidationError(string message)
    {
        ValidationMessage = message;
        HasValidationError = true;
    }

    private void ClearValidationError()
    {
        ValidationMessage = "";
        HasValidationError = false;
    }

    public DailyTask? GetUpdatedTask()
    {
        if (_originalTask == null) return null;

        _originalTask.Date = Date;
        _originalTask.Description = Description;
        _originalTask.Hours = Hours;
        
        return _originalTask;
    }
}