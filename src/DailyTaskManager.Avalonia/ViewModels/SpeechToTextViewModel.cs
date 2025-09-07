using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DailyTaskManager.Core.Interfaces;

namespace DailyTaskManager.Avalonia.ViewModels;

public partial class SpeechToTextViewModel : ObservableObject
{
    private readonly ISpeechRecognitionService _speechService;
    private readonly ILogger<SpeechToTextViewModel> _logger;

    [ObservableProperty]
    private string _recognizedText = "";

    [ObservableProperty]
    private bool _isListening = false;

    [ObservableProperty]
    private bool _isSupported = false;

    [ObservableProperty]
    private string _status = "Ready";

    public SpeechToTextViewModel(ISpeechRecognitionService speechService, ILogger<SpeechToTextViewModel> logger)
    {
        _speechService = speechService;
        _logger = logger;

        IsSupported = _speechService.IsSupported;
        
        if (!IsSupported)
        {
            Status = "Speech recognition not supported on this platform";
        }
    }

    [RelayCommand]
    private async Task StartListeningAsync()
    {
        try
        {
            if (!IsSupported) return;

            IsListening = true;
            Status = "Listening...";

            var result = await _speechService.RecognizeSpeechAsync(30);
            
            if (!string.IsNullOrEmpty(result))
            {
                RecognizedText = result;
                Status = "Speech recognized";
            }
            else
            {
                Status = "No speech detected";
            }

            _logger.LogInformation("Speech recognition result: {Text}", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during speech recognition");
            Status = "Error during speech recognition";
        }
        finally
        {
            IsListening = false;
        }
    }

    [RelayCommand]
    private async Task StopListeningAsync()
    {
        try
        {
            IsListening = false;
            Status = "Stopped";
            
            await _speechService.StopContinuousRecognitionAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping speech recognition");
        }
    }

    [RelayCommand]
    private void ClearText()
    {
        RecognizedText = "";
        Status = "Text cleared";
    }
}