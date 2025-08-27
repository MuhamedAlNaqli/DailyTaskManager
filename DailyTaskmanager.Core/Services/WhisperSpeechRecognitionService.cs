using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Diagnostics;
using Whisper.net;
using NAudio.Wave;

namespace DailyTaskManager.Core.Services
{
    /// <summary>
    /// Whisper.net implementation for high-accuracy offline speech recognition
    /// Includes automatic model downloading and audio recording capabilities
    /// </summary>
    public class WhisperSpeechRecognitionService : ISpeechRecognitionService, IDisposable
    {
        private WhisperProcessor _processor;
        private WaveInEvent _waveIn;
        private MemoryStream _audioStream;
        private bool _isListening;
        private bool _isDisposed;
        private bool _isInitialized;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly object _lock = new object();
        
        // Model configuration for speed/accuracy balance
        private const string MODEL_NAME = "ggml-base.bin"; // Good balance of speed and accuracy
        private const string MODEL_URL = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin";
        private string _modelPath;
        
        // Audio recording settings optimized for speech recognition
        private const int SAMPLE_RATE = 16000; // Whisper expects 16kHz
        private const int CHANNELS = 1; // Mono
        private const int BITS_PER_SAMPLE = 16;
        
        public event EventHandler<SpeechRecognizedEventArgs> SpeechRecognized;
        public event EventHandler<SpeechRecognitionErrorEventArgs> SpeechRecognitionError;
        public event EventHandler<SpeechRecognitionStateEventArgs> StateChanged;

        public bool IsListening
        {
            get { lock (_lock) { return _isListening; } }
            private set { lock (_lock) { _isListening = value; } }
        }

        public bool IsAvailable => _isInitialized && _processor != null;

        public async Task<bool> InitializeAsync()
        {
            if (_isDisposed)
            {
                Debug.WriteLine("WhisperSpeechRecognitionService: InitializeAsync called but service is disposed");
                return false;
            }

            try
            {
                Debug.WriteLine("WhisperSpeechRecognitionService: Starting initialization...");
                OnStateChanged(SpeechRecognitionState.Starting);
                
                // Set up model path
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var whisperPath = Path.Combine(appDataPath, "DailyTaskManager", "Whisper");
                Directory.CreateDirectory(whisperPath);
                _modelPath = Path.Combine(whisperPath, MODEL_NAME);

                // Download model if it doesn't exist
                if (!File.Exists(_modelPath))
                {
                    Debug.WriteLine("WhisperSpeechRecognitionService: Downloading Whisper model...");
                    await DownloadModelAsync();
                }

                Debug.WriteLine("WhisperSpeechRecognitionService: Loading Whisper model...");
                
                // For now, fail initialization to force fallback to System.Speech
                // until full Whisper integration is implemented
                Debug.WriteLine("WhisperSpeechRecognitionService: Full Whisper integration not yet implemented, falling back to System.Speech");
                throw new NotImplementedException("Full Whisper speech recognition integration is not yet complete. Using System.Speech fallback.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WhisperSpeechRecognitionService: Initialization failed: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                OnSpeechRecognitionError($"Failed to initialize Whisper speech recognition: {ex.Message}", ex);
                OnStateChanged(SpeechRecognitionState.Error);
                return false;
            }
        }

        private async Task DownloadModelAsync()
        {
            try
            {
                Debug.WriteLine($"WhisperSpeechRecognitionService: Downloading model from {MODEL_URL}");
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(10); // Large file download timeout
                    
                    using (var response = await client.GetAsync(MODEL_URL, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        
                        var totalBytes = response.Content.Headers.ContentLength ?? 0;
                        Debug.WriteLine($"WhisperSpeechRecognitionService: Model size: {totalBytes / (1024 * 1024)} MB");
                        
                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(_modelPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            var buffer = new byte[8192];
                            var totalRead = 0L;
                            int bytesRead;
                            
                            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                                totalRead += bytesRead;
                                
                                if (totalBytes > 0)
                                {
                                    var progress = (int)((totalRead * 100) / totalBytes);
                                    if (progress % 10 == 0) // Log every 10%
                                    {
                                        Debug.WriteLine($"WhisperSpeechRecognitionService: Download progress: {progress}%");
                                    }
                                }
                            }
                            
                            Debug.WriteLine("WhisperSpeechRecognitionService: Model download completed successfully");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WhisperSpeechRecognitionService: Model download failed: {ex.Message}");
                throw new InvalidOperationException($"Failed to download Whisper model: {ex.Message}", ex);
            }
        }

        private void SetupAudioRecording()
        {
            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(SAMPLE_RATE, BITS_PER_SAMPLE, CHANNELS)
            };
            
            _waveIn.DataAvailable += OnAudioDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;
            
            Debug.WriteLine($"WhisperSpeechRecognitionService: Audio recording setup completed - {SAMPLE_RATE}Hz, {CHANNELS} channel, {BITS_PER_SAMPLE}-bit");
        }

        public async Task<bool> StartListeningAsync()
        {
            Debug.WriteLine($"WhisperSpeechRecognitionService: StartListeningAsync called. IsDisposed: {_isDisposed}, IsInitialized: {_isInitialized}, IsListening: {IsListening}");
            
            if (_isDisposed || !_isInitialized || _processor == null || _waveIn == null || IsListening)
            {
                Debug.WriteLine("WhisperSpeechRecognitionService: StartListening preconditions failed");
                return false;
            }

            try
            {
                Debug.WriteLine("WhisperSpeechRecognitionService: Starting audio recording");
                OnStateChanged(SpeechRecognitionState.Starting);

                // Initialize audio buffer
                _audioStream = new MemoryStream();
                _cancellationTokenSource = new CancellationTokenSource();

                // Start recording
                _waveIn.StartRecording();
                
                IsListening = true;
                Debug.WriteLine("WhisperSpeechRecognitionService: Audio recording started");
                OnStateChanged(SpeechRecognitionState.Listening);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WhisperSpeechRecognitionService: StartListening failed: {ex.Message}");
                OnSpeechRecognitionError($"Failed to start Whisper speech recognition: {ex.Message}", ex);
                OnStateChanged(SpeechRecognitionState.Error);
                return false;
            }
        }

        public async Task StopListeningAsync()
        {
            if (_isDisposed || _waveIn == null || !IsListening)
                return;

            try
            {
                Debug.WriteLine("WhisperSpeechRecognitionService: Stopping audio recording");
                OnStateChanged(SpeechRecognitionState.Stopping);

                // Stop recording
                _waveIn.StopRecording();
                
                // Cancel any ongoing operations
                _cancellationTokenSource?.Cancel();
                
                IsListening = false;
                
                // Process the recorded audio with Whisper
                await ProcessRecordedAudioAsync();
                
                OnStateChanged(SpeechRecognitionState.Stopped);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WhisperSpeechRecognitionService: StopListening failed: {ex.Message}");
                OnSpeechRecognitionError($"Failed to stop Whisper speech recognition: {ex.Message}", ex);
                OnStateChanged(SpeechRecognitionState.Error);
            }
        }

        private void OnAudioDataAvailable(object sender, WaveInEventArgs e)
        {
            if (_audioStream != null && e.BytesRecorded > 0)
            {
                _audioStream.Write(e.Buffer, 0, e.BytesRecorded);
            }
        }

        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            Debug.WriteLine("WhisperSpeechRecognitionService: Audio recording stopped");
            if (e.Exception != null)
            {
                Debug.WriteLine($"WhisperSpeechRecognitionService: Recording stopped with exception: {e.Exception.Message}");
            }
        }

        private async Task ProcessRecordedAudioAsync()
        {
            if (_audioStream == null || _audioStream.Length == 0)
            {
                Debug.WriteLine("WhisperSpeechRecognitionService: No audio data to process");
                return;
            }

            try
            {
                Debug.WriteLine($"WhisperSpeechRecognitionService: Processing {_audioStream.Length} bytes of audio data");
                var stopwatch = Stopwatch.StartNew();
                
                // Convert audio stream to float array for Whisper
                var audioBytes = _audioStream.ToArray();
                var audioFloats = ConvertBytesToFloats(audioBytes);
                
                Debug.WriteLine($"WhisperSpeechRecognitionService: Converted to {audioFloats.Length} float samples");
                
                // Process with Whisper - simplified approach for C# 7.3 compatibility
                // Note: For now, we'll create a placeholder that demonstrates the integration
                // In a full implementation, this would use Whisper's synchronous API or 
                // convert the async enumerable to a compatible format
                
                var demoText = "Sample text recognition"; // Placeholder for actual Whisper processing
                stopwatch.Stop();
                var processingTime = stopwatch.ElapsedMilliseconds;
                var audioDuration = (audioFloats.Length / (float)SAMPLE_RATE) * 1000; // Duration in ms
                var speedRatio = audioDuration / processingTime;
                
                Debug.WriteLine($"WhisperSpeechRecognitionService: Audio processing completed");
                Debug.WriteLine($"WhisperSpeechRecognitionService: Processing time: {processingTime}ms for {audioDuration:F0}ms audio");
                
                // For now, return a demo message indicating Whisper integration is ready
                var processedText = $"Whisper integration ready - {audioFloats.Length} samples processed";
                if (!string.IsNullOrWhiteSpace(processedText))
                {
                    var args = new SpeechRecognizedEventArgs(processedText, 0.95f);
                    SpeechRecognized?.Invoke(this, args);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WhisperSpeechRecognitionService: Audio processing failed: {ex.Message}");
                OnSpeechRecognitionError($"Failed to process audio with Whisper: {ex.Message}", ex);
            }
            finally
            {
                // Clean up audio stream
                _audioStream?.Dispose();
                _audioStream = null;
            }
        }

        private float[] ConvertBytesToFloats(byte[] audioBytes)
        {
            var floats = new float[audioBytes.Length / 2]; // 16-bit samples = 2 bytes per sample
            for (int i = 0; i < floats.Length; i++)
            {
                // Convert 16-bit signed integer to float (-1.0 to 1.0)
                short sample = BitConverter.ToInt16(audioBytes, i * 2);
                floats[i] = sample / 32768f;
            }
            return floats;
        }

        private string PostProcessWhisperText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var processed = text.Trim();
            
            // Remove very short results (likely noise)
            if (processed.Length < 2)
                return string.Empty;

            // Capitalize first letter
            if (processed.Length > 0)
            {
                processed = char.ToUpper(processed[0]) + processed.Substring(1);
            }

            Debug.WriteLine($"WhisperSpeechRecognitionService: Text processed from '{text}' to '{processed}'");
            return processed;
        }


        private void OnSpeechRecognitionError(string message, Exception exception = null)
        {
            if (_isDisposed)
                return;

            var args = new SpeechRecognitionErrorEventArgs(message, exception);
            SpeechRecognitionError?.Invoke(this, args);
        }

        private void OnStateChanged(SpeechRecognitionState state)
        {
            if (_isDisposed)
                return;

            var args = new SpeechRecognitionStateEventArgs(state);
            StateChanged?.Invoke(this, args);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            try
            {
                if (IsListening && _waveIn != null)
                {
                    _waveIn.StopRecording();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WhisperSpeechRecognitionService: Error stopping recording during disposal: {ex.Message}");
            }

            try
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WhisperSpeechRecognitionService: Error cancelling operations during disposal: {ex.Message}");
            }

            try
            {
                if (_waveIn != null)
                {
                    _waveIn.DataAvailable -= OnAudioDataAvailable;
                    _waveIn.RecordingStopped -= OnRecordingStopped;
                    _waveIn.Dispose();
                    _waveIn = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WhisperSpeechRecognitionService: Error disposing wave input: {ex.Message}");
            }

            try
            {
                _processor?.Dispose();
                _processor = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WhisperSpeechRecognitionService: Error disposing Whisper processor: {ex.Message}");
            }

            try
            {
                _audioStream?.Dispose();
                _audioStream = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WhisperSpeechRecognitionService: Error disposing audio stream: {ex.Message}");
            }

            IsListening = false;
            _isInitialized = false;
            
            Debug.WriteLine("WhisperSpeechRecognitionService: Disposal completed");
        }
    }
}