using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DailyTaskManager.Core.Models;
using DailyTaskManager.Core.Interfaces;

namespace DailyTaskManager.Avalonia.ViewModels;

public partial class AddTaskViewModel : ObservableObject
{
    private readonly ILogger<AddTaskViewModel> _logger;
    private readonly IAutoCompleteService _autoCompleteService;
    private CancellationTokenSource? _searchCancellationTokenSource;

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
    private bool _showSuggestions = false;

    [ObservableProperty]
    private int _selectedSuggestionIndex = -1;

    public ObservableCollection<string> Suggestions { get; } = new();

    public event EventHandler<bool>? RequestClose;

    public AddTaskViewModel(ILogger<AddTaskViewModel> logger, IAutoCompleteService autoCompleteService)
    {
        _logger = logger;
        _autoCompleteService = autoCompleteService;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            if (!ValidateInput())
                return;

            IsLoading = true;
            _logger.LogInformation("Validating new task: {Description}", Description);

            // Brief validation delay for UX
            await Task.Delay(100);

            _logger.LogInformation("Task validation completed successfully");
            RequestClose?.Invoke(this, true); // Dialog result = true
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating task");
            ShowValidationError("Failed to validate task. Please try again.");
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

    public DailyTask CreateTask()
    {
        return new DailyTask
        {
            Date = Date?.DateTime ?? DateTime.Today,
            Description = Description.Trim(),
            Hours = Hours
        };
    }

    partial void OnDescriptionChanged(string value)
    {
        _ = SearchSuggestionsWithDebounceAsync(value);
    }

    private async Task SearchSuggestionsWithDebounceAsync(string input)
    {
        try
        {
            // Cancel previous search
            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = _searchCancellationTokenSource.Token;

            // Debounce delay
            await Task.Delay(300, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return;

            await SearchSuggestionsAsync(input);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelled, do nothing
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in debounced search for input '{Input}'", input);
        }
    }

    private async Task SearchSuggestionsAsync(string input)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input) || input.Length < 2)
            {
                HideSuggestions();
                return;
            }

            var suggestions = await _autoCompleteService.GetSuggestionsAsync(input, 8);

            Suggestions.Clear();
            foreach (var suggestion in suggestions)
            {
                Suggestions.Add(suggestion);
            }

            ShowSuggestions = Suggestions.Count > 0;
            SelectedSuggestionIndex = -1;

            _logger.LogDebug("Found {Count} suggestions for '{Input}'", suggestions.Count, input);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching suggestions for input '{Input}'", input);
            HideSuggestions();
        }
    }

    [RelayCommand]
    public void SelectSuggestion(string? suggestion)
    {
        if (!string.IsNullOrEmpty(suggestion))
        {
            Description = suggestion;
            HideSuggestions();
            _logger.LogDebug("Selected suggestion: '{Suggestion}'", suggestion);
        }
    }

    [RelayCommand]
    public void NavigateSuggestions(string direction)
    {
        if (!ShowSuggestions || Suggestions.Count == 0)
            return;

        switch (direction.ToLower())
        {
            case "up":
                SelectedSuggestionIndex = SelectedSuggestionIndex <= 0
                    ? Suggestions.Count - 1
                    : SelectedSuggestionIndex - 1;
                break;
            case "down":
                SelectedSuggestionIndex = SelectedSuggestionIndex >= Suggestions.Count - 1
                    ? 0
                    : SelectedSuggestionIndex + 1;
                break;
        }
    }

    [RelayCommand]
    public void ApplySelectedSuggestion()
    {
        if (SelectedSuggestionIndex >= 0 && SelectedSuggestionIndex < Suggestions.Count)
        {
            SelectSuggestion(Suggestions[SelectedSuggestionIndex]);
        }
    }

    [RelayCommand]
    public void HideSuggestions()
    {
        ShowSuggestions = false;
        SelectedSuggestionIndex = -1;
        _searchCancellationTokenSource?.Cancel();
    }
}