using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using Microsoft.Extensions.Logging;
using DailyTaskManager.Core.Interfaces;

namespace DailyTaskManager.Avalonia.Services;

/// <summary>
/// Drives the dictation button of a dialog: starts and stops listening and hands recognized text to the dialog
/// </summary>
public partial class DictationController : ObservableObject, IDisposable
{
    private readonly ISpeechRecognitionService _speech;
    private readonly ILogger _logger;
    private bool _subscribed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ButtonText))]
    [NotifyPropertyChangedFor(nameof(IconKind))]
    private bool _isListening;

    [ObservableProperty]
    private string _status = "";

    public DictationController(ISpeechRecognitionService speech, ILogger logger)
    {
        _speech = speech;
        _logger = logger;
        IsAvailable = speech.IsSupported;
    }

    /// <summary>False when Windows has no speech recognizer; the dialog then hides the button</summary>
    public bool IsAvailable { get; }

    public string ButtonText => IsListening ? "Stop" : "Dictate";

    public MaterialIconKind IconKind => IsListening ? MaterialIconKind.Stop : MaterialIconKind.MicrophoneOutline;

    /// <summary>Raised on the UI thread with each recognized phrase</summary>
    public event Action<string>? TextRecognized;

    [RelayCommand]
    private async Task ToggleAsync()
    {
        if (IsListening)
        {
            await StopAsync();
            return;
        }

        try
        {
            Subscribe();
            Status = "Listening... speak now";
            await _speech.StartContinuousRecognitionAsync();
            IsListening = _speech.IsListening;
            if (!IsListening && string.IsNullOrEmpty(Status)) Status = "Could not start listening.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not start dictation");
            Status = "Could not start dictation: " + ex.Message;
            IsListening = false;
        }
    }

    public async Task StopAsync()
    {
        try
        {
            await _speech.StopContinuousRecognitionAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not stop dictation");
        }
        finally
        {
            Unsubscribe();
            IsListening = false;
            Status = "";
        }
    }

    private void Subscribe()
    {
        if (_subscribed) return;
        _speech.SpeechRecognized += OnRecognized;
        _speech.RecognitionError += OnError;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;
        _speech.SpeechRecognized -= OnRecognized;
        _speech.RecognitionError -= OnError;
        _subscribed = false;
    }

    private void OnRecognized(object? sender, string text) => Dispatcher.UIThread.Post(() => TextRecognized?.Invoke(text));

    private void OnError(object? sender, string message) => Dispatcher.UIThread.Post(() =>
    {
        Status = message;
        IsListening = false;
    });

    public void Dispose()
    {
        _ = StopAsync();
    }
}
