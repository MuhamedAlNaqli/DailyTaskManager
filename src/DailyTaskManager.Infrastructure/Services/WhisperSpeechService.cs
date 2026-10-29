using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using Whisper.net;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Infrastructure.Services;

/// <summary>
/// Offline dictation with OpenAI's Whisper (through whisper.cpp), the same family of models Handy uses.
/// The microphone is cut into phrases at pauses; each phrase is transcribed on its own, so text appears while
/// the user keeps talking. The model (about 190 MB) is downloaded once, on first use, and is only held in memory
/// while dictating.
/// </summary>
public sealed class WhisperSpeechService : ISpeechRecognitionService, IDisposable
{
    private const string ModelFileName = "ggml-small-q5_1.bin";
    private const string ModelUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/" + ModelFileName;
    private const long MinimumModelBytes = 100_000_000;

    private const int SampleRate = 16000;
    private const int ChunkMilliseconds = 50;
    private static readonly int PauseSamples = SampleRate * 8 / 10;       // 0.8 s of silence ends a phrase
    private static readonly int MinSpeechSamples = SampleRate * 3 / 10;   // shorter blips are noise
    private static readonly int MinFlushSamples = SampleRate / 2;         // what is left at Stop must be a real phrase
    private static readonly int StopClickSamples = SampleRate * 4 / 10;   // the click on Stop is not part of the speech
    private static readonly int PreRollSamples = SampleRate * 3 / 10;     // keep the start of the first word
    private static readonly int MaxPhraseSamples = SampleRate * 25;
    private static readonly int MinWhisperSamples = SampleRate * 11 / 10; // whisper.cpp wants over a second of audio

    private static readonly TimeSpan KeepModelLoaded = TimeSpan.FromSeconds(45);

    private static readonly Regex NoiseOnly = new(@"^[\s\[\(\*♪\-]*(blank_audio|silence|music|noise|applause|inaudible)?[\s\]\)\*♪\-\.]*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Whisper tends to "hear" these in near-silence
    private static readonly HashSet<string> Hallucinations = new(StringComparer.OrdinalIgnoreCase)
    {
        "thank you", "thank you.", "thanks for watching", "thanks for watching!", "you", "bye", "bye.", "hvala", "hvala.",
        "hvala na pažnji", "hvala na pažnji.", "hvala što ste gledali", "hvala što ste gledali.", "hvala na gledanju", "hvala na gledanju.",
        "pretplatite se", "titlovi", "titlovi.", "amara.org", "subtitles by the amara.org community", "danke fürs zuschauen", "izlediğiniz için teşekkürler"
    };

    /// <summary>Starting words for Whisper's prompt, per language: it makes the model expect work notes in that language</summary>
    private static readonly Dictionary<string, string> PromptLeads = new()
    {
        ["hr"] = "Dnevne radne aktivnosti: sastanak, testiranje, ispravka grešaka, analiza, razvoj.",
        ["en"] = "Daily work activities: meeting, testing, bug fixing, analysis, development.",
        ["de"] = "Tägliche Arbeitsaktivitäten: Besprechung, Testen, Fehlerbehebung, Analyse, Entwicklung.",
        ["tr"] = "Günlük iş faaliyetleri: toplantı, test, hata düzeltme, analiz, geliştirme."
    };

    private readonly ILogger<WhisperSpeechService> _logger;
    private readonly ISettingsService _settings;
    private readonly IDatabaseService? _database;
    private readonly object _gate = new();
    private readonly SemaphoreSlim _modelLock = new(1, 1);

    private WhisperFactory? _factory;
    private Timer? _unloadTimer;
    private string? _languageOverride;

    private WaveInEvent? _mic;
    private Channel<float[]>? _queue;
    private Task? _worker;
    private CancellationTokenSource? _cts;
    private PhraseCutter? _cutter;

    public WhisperSpeechService(ILogger<WhisperSpeechService> logger, ISettingsService settings, IDatabaseService? database = null)
    {
        _logger = logger;
        _settings = settings;
        _database = database;
    }

    public static string ModelPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DailyTaskManager", "models", ModelFileName);

    /// <summary>True when this is 64-bit Windows with a microphone attached</summary>
    public bool IsSupported
    {
        get
        {
            try
            {
                return OperatingSystem.IsWindows() && Environment.Is64BitProcess && WaveInEvent.DeviceCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Audio devices could not be listed");
                return false;
            }
        }
    }

    public bool IsListening { get; private set; }

    public event EventHandler<string>? SpeechRecognized;
    public event EventHandler<string>? RecognitionError;
    public event EventHandler<string>? StatusChanged;

    // ---- Model ---------------------------------------------------------------------------------------------

    /// <summary>Downloads the model on first use; the file only appears under its final name once complete</summary>
    private async Task EnsureModelFileAsync(CancellationToken token)
    {
        var path = ModelPath;
        if (File.Exists(path) && new FileInfo(path).Length >= MinimumModelBytes) return;

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var partial = path + ".part";
        StatusChanged?.Invoke(this, "Downloading the speech model (first use only)...");
        _logger.LogInformation("Downloading the speech model from {Url}", ModelUrl);

        using var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        using var response = await client.GetAsync(ModelUrl, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        var total = response.Content.Headers.ContentLength ?? 0;

        await using (var source = await response.Content.ReadAsStreamAsync(token))
        await using (var target = File.Create(partial))
        {
            var buffer = new byte[256 * 1024];
            long done = 0;
            var lastPercent = -1;
            int read;
            while ((read = await source.ReadAsync(buffer, token)) > 0)
            {
                await target.WriteAsync(buffer.AsMemory(0, read), token);
                done += read;
                var percent = total > 0 ? (int)(done * 100 / total) : 0;
                if (percent != lastPercent)
                {
                    lastPercent = percent;
                    StatusChanged?.Invoke(this, total > 0
                        ? $"Downloading the speech model (first use only)... {percent}%"
                        : $"Downloading the speech model (first use only)... {done / 1048576} MB");
                }
            }
        }

        if (new FileInfo(partial).Length < MinimumModelBytes) throw new IOException("The speech model download was incomplete.");
        File.Move(partial, path, overwrite: true);
        _logger.LogInformation("Speech model saved to {Path}", path);
    }

    private async Task<WhisperFactory> LoadFactoryAsync(CancellationToken token)
    {
        await _modelLock.WaitAsync(token);
        try
        {
            _unloadTimer?.Dispose();
            _unloadTimer = null;
            if (_factory != null) return _factory;

            await EnsureModelFileAsync(token);
            StatusChanged?.Invoke(this, "Loading the speech model...");
            _factory = await Task.Run(() => WhisperFactory.FromPath(ModelPath), token);
            return _factory;
        }
        finally
        {
            _modelLock.Release();
        }
    }

    /// <summary>Frees the model's memory a little after dictation ended, so the app stays small</summary>
    private void ScheduleUnload()
    {
        _unloadTimer?.Dispose();
        _unloadTimer = new Timer(_ =>
        {
            lock (_gate)
            {
                if (IsListening) return;
            }

            _modelLock.Wait();
            try
            {
                _factory?.Dispose();
                _factory = null;
                _logger.LogInformation("Speech model unloaded");
            }
            finally
            {
                _modelLock.Release();
            }
        }, null, KeepModelLoaded, Timeout.InfiniteTimeSpan);
    }

    private async Task<string> ResolveLanguageAsync()
    {
        if (!string.IsNullOrEmpty(_languageOverride)) return _languageOverride!;
        try
        {
            var stored = await _settings.GetSettingAsync<string>(DictationSettings.LanguageKey, null);
            if (!string.IsNullOrWhiteSpace(stored)) return stored!;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "The dictation language setting could not be read");
        }

        return DictationSettings.AutoLanguage;
    }

    // ---- Transcription -------------------------------------------------------------------------------------

    /// <summary>
    /// Bosnian and Serbian are close to Croatian, which Whisper knows far better (and writes in Latin script), so they
    /// are transcribed through Croatian. "auto" lets Whisper detect the language.
    /// </summary>
    public static string WhisperLanguage(string language) => language switch
    {
        "bs" or "sr" => "hr",
        _ => string.IsNullOrWhiteSpace(language) ? DictationSettings.AutoLanguage : language
    };

    /// <summary>
    /// The text Whisper is primed with: a line in the spoken language plus the user's own recent entries, so names and
    /// terms they use every day (and the language itself) are expected instead of guessed
    /// </summary>
    private async Task<string> BuildPromptAsync(string whisperLanguage)
    {
        var lead = PromptLeads.TryGetValue(whisperLanguage, out var text) ? text : "";
        if (lead.Length == 0) return "";

        try
        {
            if (_database != null)
            {
                var recent = await _database.GetTasksPageAsync(new TaskQuery(), 1, 12);
                var lines = recent.Items
                    .SelectMany(t => t.Description.Split('\n'))
                    .Select(l => l.Trim().TrimStart('-', ' ').Trim())
                    .Where(l => l.Length > 2)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(8);
                lead += " " + string.Join(". ", lines) + ".";
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Recent entries could not be read for the dictation prompt");
        }

        return lead.Length > 600 ? lead.Substring(0, 600) : lead;
    }

    /// <summary>Transcribes one phrase (16 kHz mono samples). Public so the whole chain can be tested with a recording.</summary>
    public async Task<string> TranscribeAsync(float[] samples, string language, CancellationToken token = default)
    {
        var factory = await LoadFactoryAsync(token);
        var prepared = Prepare(samples);
        var whisperLanguage = WhisperLanguage(language);
        var prompt = await BuildPromptAsync(whisperLanguage);

        // Whisper always looks at a 30 second window; shrinking it to the length of the phrase (plus a generous margin)
        // makes short phrases several times faster on a CPU (1500 context frames = 30 s)
        var audioContext = Math.Min(1500, (int)Math.Ceiling(prepared.Length / (double)SampleRate * 50) + 200);

        var builder = factory.CreateBuilder()
            .WithLanguage(whisperLanguage)
            .WithThreads(Math.Clamp(Environment.ProcessorCount / 2, 2, 8))
            .WithNoSpeechThreshold(0.6f)
            .WithAudioContextSize(audioContext);
        // The default temperature fallback stays on: a failed first decode is retried instead of dropping words
        if (prompt.Length > 0) builder = builder.WithPrompt(prompt);
        await using var processor = builder.Build();

        var text = new StringBuilder();
        await foreach (var segment in processor.ProcessAsync(prepared, token))
        {
            // Only skip what Whisper is nearly sure is not speech; a quiet microphone must not lose words
            if (segment.NoSpeechProbability > 0.95f && segment.Text.Trim().Length < 20) continue;
            text.Append(segment.Text);
        }

        return CleanUp(text.ToString(), samples.Length);
    }

    /// <summary>Pads very short clips and lifts quiet recordings, both of which make Whisper guess</summary>
    private static float[] Prepare(float[] samples)
    {
        var peak = 0f;
        foreach (var s in samples) peak = Math.Max(peak, Math.Abs(s));
        var gain = peak > 0.001f && peak < 0.5f ? Math.Min(0.8f / peak, 8f) : 1f;

        var length = Math.Max(samples.Length, MinWhisperSamples);
        var result = new float[length];
        for (var i = 0; i < samples.Length; i++) result[i] = samples[i] * gain;
        return result;
    }

    public static string CleanUp(string text, int sampleCount)
    {
        text = Regex.Replace(text, @"\s+", " ").Trim();
        if (text.Length == 0 || NoiseOnly.IsMatch(text)) return "";
        // Sound descriptions such as "(singing in foreign language)" or "[music]" are not speech
        if (Regex.IsMatch(text, @"^[\(\[\*].*[\)\]\*]$")) return "";
        if (sampleCount < SampleRate * 3 && Hallucinations.Contains(text)) return "";

        // Whisper sometimes loops on noise: "Hvala. Hvala. Hvala." or one word over and over
        text = Regex.Replace(text, @"\b(\w+[.,!?]?)(?:\s+\1){2,}(?=\s|$)", "$1", RegexOptions.IgnoreCase);
        return Hallucinations.Contains(text) && sampleCount < SampleRate * 3 ? "" : text;
    }

    // ---- Listening -----------------------------------------------------------------------------------------

    public async Task<string?> RecognizeSpeechAsync(int timeoutSeconds = 30)
    {
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnText(object? _, string text) => tcs.TrySetResult(text);
        void OnError(object? _, string __) => tcs.TrySetResult(null);

        SpeechRecognized += OnText;
        RecognitionError += OnError;
        try
        {
            await StartContinuousRecognitionAsync();
            var finished = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds)));
            return finished == tcs.Task ? await tcs.Task : null;
        }
        finally
        {
            SpeechRecognized -= OnText;
            RecognitionError -= OnError;
            await StopContinuousRecognitionAsync();
        }
    }

    public Task StartContinuousRecognitionAsync()
    {
        lock (_gate)
        {
            if (IsListening) return Task.CompletedTask;

            if (!IsSupported)
            {
                RecognitionError?.Invoke(this, "Dictation needs a microphone. Connect one and check the Windows sound settings.");
                return Task.CompletedTask;
            }

            // Listening is reported right away; the model may still have to download or load
            IsListening = true;
            _cts = new CancellationTokenSource();
            _queue = Channel.CreateUnbounded<float[]>();
            var token = _cts.Token;
            _worker = Task.Run(() => RunSessionAsync(_queue, token));
        }

        return Task.CompletedTask;
    }

    private async Task RunSessionAsync(Channel<float[]> queue, CancellationToken token)
    {
        try
        {
            var language = await ResolveLanguageAsync();
            await LoadFactoryAsync(token);

            _cutter = new PhraseCutter(phrase => queue.Writer.TryWrite(phrase));
            var mic = new WaveInEvent
            {
                WaveFormat = new WaveFormat(SampleRate, 16, 1),
                BufferMilliseconds = ChunkMilliseconds
            };
            mic.DataAvailable += (_, e) => _cutter?.Feed(e.Buffer, e.BytesRecorded);
            mic.RecordingStopped += (_, e) =>
            {
                if (e.Exception != null) _logger.LogWarning(e.Exception, "The microphone stopped unexpectedly");
            };
            mic.StartRecording();
            _mic = mic;
            StatusChanged?.Invoke(this, "Listening... speak now");
            _logger.LogInformation("Dictation started (language {Language})", language);

            var previous = "";
            await foreach (var phrase in queue.Reader.ReadAllAsync(CancellationToken.None))
            {
                try
                {
                    var text = await TranscribeAsync(phrase, language, CancellationToken.None);
                    if (string.Equals(text, previous, StringComparison.OrdinalIgnoreCase)) text = ""; // an echo of the last phrase
                    previous = text.Length > 0 ? text : previous;
                    _logger.LogInformation("Phrase of {Seconds:0.0} s transcribed to {Length} characters", phrase.Length / (double)SampleRate, text.Length);
                    if (text.Length > 0)
                    {
                        _logger.LogInformation("Dictated {Length} characters", text.Length);
                        SpeechRecognized?.Invoke(this, text);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "A dictated phrase could not be transcribed");
                    RecognitionError?.Invoke(this, "A phrase could not be transcribed: " + ex.Message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Stopped while the model was still preparing
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dictation could not be started");
            lock (_gate) IsListening = false;
            RecognitionError?.Invoke(this, FriendlyMessage(ex));
        }
    }

    private static string FriendlyMessage(Exception ex) => ex switch
    {
        HttpRequestException => "The speech model could not be downloaded. Check your internet connection and try again.",
        IOException => "The speech model could not be saved: " + ex.Message,
        DllNotFoundException or BadImageFormatException => "The speech engine could not be loaded on this system.",
        _ => "Dictation could not be started: " + ex.Message
    };

    public async Task StopContinuousRecognitionAsync()
    {
        Task? worker;
        lock (_gate)
        {
            if (!IsListening && _worker == null) return;
            worker = _worker;
            _worker = null;
        }

        try
        {
            if (_mic != null)
            {
                _mic.StopRecording();
                _mic.Dispose();
                _mic = null;
            }

            // Whatever was said last still gets transcribed
            _cutter?.Flush();
            _cutter = null;
            _cts?.Cancel();
            _queue?.Writer.TryComplete();

            if (worker != null)
            {
                StatusChanged?.Invoke(this, "Finishing the last words...");
                await Task.WhenAny(worker, Task.Delay(TimeSpan.FromSeconds(60)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping dictation");
        }
        finally
        {
            lock (_gate) IsListening = false;
            _cts?.Dispose();
            _cts = null;
            _queue = null;
            ScheduleUnload();
            _logger.LogInformation("Dictation stopped");
        }
    }

    public Task<List<string>> GetAvailableLanguagesAsync() =>
        Task.FromResult(DictationSettings.Languages.Select(l => l.Key).ToList());

    public Task SetLanguageAsync(string languageCode)
    {
        _languageOverride = string.IsNullOrWhiteSpace(languageCode) ? null : languageCode;
        _logger.LogInformation("Dictation language set to {Language}", languageCode);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        StopContinuousRecognitionAsync().GetAwaiter().GetResult();
        _unloadTimer?.Dispose();
        _factory?.Dispose();
        _modelLock.Dispose();
    }

    /// <summary>
    /// Cuts the microphone stream into phrases: a phrase ends after a pause, or when it gets too long.
    /// A simple level detector with an adaptive noise floor is enough for a quiet desk microphone.
    /// </summary>
    public sealed class PhraseCutter
    {
        private readonly Action<float[]> _onPhrase;
        private readonly List<float> _buffer = new();
        private bool _inSpeech;
        private int _speechSamples;
        private int _silenceSamples;
        private float _noiseFloor = 0.002f;

        public PhraseCutter(Action<float[]> onPhrase) => _onPhrase = onPhrase;

        public void Feed(byte[] bytes, int count)
        {
            var n = count / 2;
            if (n == 0) return;

            var chunk = new float[n];
            double sum = 0;
            for (var i = 0; i < n; i++)
            {
                chunk[i] = BitConverter.ToInt16(bytes, i * 2) / 32768f;
                sum += chunk[i] * chunk[i];
            }

            var rms = (float)Math.Sqrt(sum / n);
            var threshold = Math.Max(0.004f, _noiseFloor * 2.5f);
            _buffer.AddRange(chunk);

            if (rms > threshold)
            {
                _inSpeech = true;
                _speechSamples += n;
                _silenceSamples = 0;
            }
            else
            {
                _silenceSamples += n;
                if (!_inSpeech)
                {
                    _noiseFloor = _noiseFloor * 0.95f + rms * 0.05f;
                    if (_buffer.Count > PreRollSamples) _buffer.RemoveRange(0, _buffer.Count - PreRollSamples);
                }
            }

            if (_inSpeech && (_silenceSamples >= PauseSamples || _buffer.Count >= MaxPhraseSamples)) Emit();
        }

        /// <summary>
        /// Hands over what is left when dictation stops. The last moments are the click on Stop, so they are dropped,
        /// and what remains must be a real phrase, not a rustle.
        /// </summary>
        public void Flush()
        {
            if (!_inSpeech) return;

            var drop = Math.Min(_buffer.Count, StopClickSamples);
            _buffer.RemoveRange(_buffer.Count - drop, drop);
            _speechSamples = Math.Max(0, _speechSamples - drop);
            if (_speechSamples >= MinFlushSamples) Emit();
            else Reset();
        }

        private void Emit()
        {
            if (_speechSamples >= MinSpeechSamples)
            {
                // Keep a little of the pause, drop the rest
                var keep = Math.Max(0, _buffer.Count - Math.Max(0, _silenceSamples - PreRollSamples));
                _onPhrase(_buffer.Take(keep).ToArray());
            }

            Reset();
        }

        private void Reset()
        {
            _buffer.Clear();
            _inSpeech = false;
            _speechSamples = 0;
            _silenceSamples = 0;
        }
    }
}
