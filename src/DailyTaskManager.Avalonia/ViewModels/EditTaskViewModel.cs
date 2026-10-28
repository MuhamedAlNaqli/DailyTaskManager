using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Avalonia.ViewModels;

public partial class EditTaskViewModel : ObservableObject
{
    private readonly ILogger<EditTaskViewModel> _logger;
    private readonly IDatabaseService _databaseService;
    private DailyTask? _originalTask;
    private int? _pendingClientId;
    private int? _resolvedClientId;

    [ObservableProperty]
    private DateTimeOffset? _date = DateTimeOffset.Now.Date;

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

    [ObservableProperty]
    private ClientChoice? _selectedClient;

    [ObservableProperty]
    private string _newClientName = "";

    public ObservableCollection<ClientChoice> Clients { get; } = new();

    public event EventHandler<bool>? RequestClose;

    public EditTaskViewModel(ILogger<EditTaskViewModel> logger, IDatabaseService databaseService)
    {
        _logger = logger;
        _databaseService = databaseService;
        _ = LoadClientsAsync();
    }

    private async Task LoadClientsAsync()
    {
        try
        {
            var clients = await _databaseService.GetClientsAsync();
            Clients.Clear();
            Clients.Add(ClientChoice.NoClient);
            foreach (var client in clients) Clients.Add(ClientChoice.From(client));
            SelectPendingClient();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading clients");
        }
    }

    private void SelectPendingClient()
    {
        if (Clients.Count == 0) return;
        SelectedClient = Clients.FirstOrDefault(c => c.Id == _pendingClientId && c != ClientChoice.NoClient) ?? ClientChoice.NoClient;
    }

    public void LoadTask(DailyTask task)
    {
        _originalTask = task;
        Date = new DateTimeOffset(task.Date);
        Description = task.Description;
        Hours = task.Hours;
        _pendingClientId = task.ClientId;
        SelectPendingClient();
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

            _resolvedClientId = !string.IsNullOrWhiteSpace(NewClientName)
                ? (await _databaseService.GetOrCreateClientAsync(NewClientName)).Id
                : SelectedClient?.Id;

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

        return new DailyTask
        {
            Id = _originalTask.Id,
            UserId = _originalTask.UserId,
            Date = Date?.DateTime ?? DateTime.Today,
            Description = Description.Trim(),
            Hours = Hours,
            ClientId = _resolvedClientId,
            CreatedAt = _originalTask.CreatedAt
        };
    }
}