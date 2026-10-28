#pragma warning disable CA1416 // System.Speech is Windows-only; every call is guarded by IsSupported
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Speech.Recognition;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DailyTaskManager.Core.Interfaces;

namespace DailyTaskManager.Infrastructure.Services;

/// <summary>
/// Dictation using the speech recognizers installed in Windows (System.Speech)
/// </summary>
public class SpeechRecognitionService : ISpeechRecognitionService, IDisposable
{
    private readonly ILogger<SpeechRecognitionService> _logger;
    private readonly object _gate = new();
    private SpeechRecognitionEngine? _engine;
    private string? _languageCode;

    public SpeechRecognitionService(ILogger<SpeechRecognitionService> logger)
    {
        _logger = logger;
    }

    /// <summary>True on Windows when at least one speech recognizer is installed</summary>
    public bool IsSupported
    {
        get
        {
            try
            {
                return OperatingSystem.IsWindows() && SpeechRecognitionEngine.InstalledRecognizers().Count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Speech recognizers could not be listed");
                return false;
            }
        }
    }

    public bool IsListening { get; private set; }

    public event EventHandler<string>? SpeechRecognized;
    public event EventHandler<string>? RecognitionError;

    private RecognizerInfo? PickRecognizer()
    {
        var installed = SpeechRecognitionEngine.InstalledRecognizers();
        if (installed.Count == 0) return null;

        if (!string.IsNullOrEmpty(_languageCode))
        {
            var chosen = installed.FirstOrDefault(r => string.Equals(r.Culture.Name, _languageCode, StringComparison.OrdinalIgnoreCase));
            if (chosen != null) return chosen;
        }

        var ui = CultureInfo.CurrentUICulture;
        return installed.FirstOrDefault(r => r.Culture.Name == ui.Name)
               ?? installed.FirstOrDefault(r => r.Culture.TwoLetterISOLanguageName == ui.TwoLetterISOLanguageName)
               ?? installed.FirstOrDefault(r => r.Culture.Name == "en-US")
               ?? installed[0];
    }

    private SpeechRecognitionEngine CreateEngine()
    {
        var info = PickRecognizer() ?? throw new InvalidOperationException("No speech recognizer is installed in Windows.");
        var engine = new SpeechRecognitionEngine(info);
        try
        {
            engine.LoadGrammar(new DictationGrammar());
            engine.SetInputToDefaultAudioDevice();
        }
        catch
        {
            engine.Dispose();
            throw;
        }

        _logger.LogInformation("Speech recognizer: {Name} ({Culture})", info.Name, info.Culture.Name);
        return engine;
    }

    private static string FriendlyMessage(Exception ex) => ex switch
    {
        InvalidOperationException when ex.Message.Contains("audio", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("microphone", StringComparison.OrdinalIgnoreCase)
            => "No microphone found. Connect one and check Windows sound settings.",
        InvalidOperationException => ex.Message,
        _ => "Speech recognition could not be started: " + ex.Message
    };

    public async Task<string?> RecognizeSpeechAsync(int timeoutSeconds = 30)
    {
        if (!IsSupported)
        {
            RecognitionError?.Invoke(this, "Speech recognition is not available (no Windows speech recognizer installed).");
            return null;
        }

        SpeechRecognitionEngine? engine = null;
        try
        {
            engine = CreateEngine();
            var tcs = new TaskCompletionSource<string?>();
            engine.SpeechRecognized += (_, e) => tcs.TrySetResult(e.Result.Text);
            engine.RecognizeCompleted += (_, _) => tcs.TrySetResult(null);

            IsListening = true;
            engine.RecognizeAsync(RecognizeMode.Single);

            var finished = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds)));
            if (finished != tcs.Task) engine.RecognizeAsyncCancel();

            return finished == tcs.Task ? await tcs.Task : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during speech recognition");
            RecognitionError?.Invoke(this, FriendlyMessage(ex));
            return null;
        }
        finally
        {
            IsListening = false;
            engine?.Dispose();
        }
    }

    public Task StartContinuousRecognitionAsync()
    {
        lock (_gate)
        {
            if (IsListening) return Task.CompletedTask;

            if (!IsSupported)
            {
                RecognitionError?.Invoke(this, "Speech recognition is not available (no Windows speech recognizer installed).");
                return Task.CompletedTask;
            }

            try
            {
                var engine = CreateEngine();
                engine.SpeechRecognized += (_, e) =>
                {
                    // Ignore low-confidence noise
                    if (e.Result.Confidence >= 0.3f && !string.IsNullOrWhiteSpace(e.Result.Text))
                    {
                        SpeechRecognized?.Invoke(this, e.Result.Text);
                    }
                };
                engine.RecognizeAsync(RecognizeMode.Multiple);

                _engine = engine;
                IsListening = true;
                _logger.LogInformation("Continuous speech recognition started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting continuous speech recognition");
                IsListening = false;
                RecognitionError?.Invoke(this, FriendlyMessage(ex));
            }
        }

        return Task.CompletedTask;
    }

    public Task StopContinuousRecognitionAsync()
    {
        lock (_gate)
        {
            try
            {
                if (_engine != null)
                {
                    _engine.RecognizeAsyncCancel();
                    _engine.Dispose();
                    _engine = null;
                    _logger.LogInformation("Continuous speech recognition stopped");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping continuous speech recognition");
            }
            finally
            {
                IsListening = false;
            }
        }

        return Task.CompletedTask;
    }

    public Task<List<string>> GetAvailableLanguagesAsync()
    {
        try
        {
            var languages = OperatingSystem.IsWindows()
                ? SpeechRecognitionEngine.InstalledRecognizers().Select(r => r.Culture.Name).ToList()
                : new List<string>();
            return Task.FromResult(languages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available languages");
            return Task.FromResult(new List<string>());
        }
    }

    public Task SetLanguageAsync(string languageCode)
    {
        _languageCode = languageCode;
        _logger.LogInformation("Speech recognition language set to {Language}", languageCode);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        StopContinuousRecognitionAsync().GetAwaiter().GetResult();
    }
}
