using System;
using System.Globalization;
using System.Speech.Recognition;
using System.Threading.Tasks;

namespace DailyTaskManager.Core.Services
{
    public class SpeechRecognitionService : ISpeechRecognitionService, IDisposable
    {
        private SpeechRecognitionEngine _recognizer;
        private bool _isListening;
        private bool _isDisposed;
        private readonly object _lock = new object();

        public event EventHandler<SpeechRecognizedEventArgs> SpeechRecognized;
        public event EventHandler<SpeechRecognitionErrorEventArgs> SpeechRecognitionError;
        public event EventHandler<SpeechRecognitionStateEventArgs> StateChanged;

        public bool IsListening
        {
            get { lock (_lock) { return _isListening; } }
            private set { lock (_lock) { _isListening = value; } }
        }

        public bool IsAvailable => _recognizer != null;

        public async Task<bool> InitializeAsync()
        {
            if (_isDisposed)
            {
                System.Diagnostics.Debug.WriteLine("SpeechRecognitionService: InitializeAsync called but service is disposed");
                return false;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("SpeechRecognitionService: Starting initialization...");
                
                await Task.Run(() =>
                {
                    // Create recognizer for default culture
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("SpeechRecognitionService: Creating SpeechRecognitionEngine with default culture...");
                        _recognizer = new SpeechRecognitionEngine();
                    }
                    catch (Exception ex)
                    {
                        // Try with current culture if default fails
                        System.Diagnostics.Debug.WriteLine($"SpeechRecognitionService: Default culture failed ({ex.Message}), trying current culture...");
                        _recognizer = new SpeechRecognitionEngine(CultureInfo.CurrentCulture);
                    }

                    // Create grammar for dictation
                    System.Diagnostics.Debug.WriteLine("SpeechRecognitionService: Loading dictation grammar...");
                    var dictationGrammar = new DictationGrammar();
                    _recognizer.LoadGrammar(dictationGrammar);

                    // Set up event handlers
                    System.Diagnostics.Debug.WriteLine("SpeechRecognitionService: Setting up event handlers...");
                    _recognizer.SpeechRecognized += OnSpeechRecognized;
                    _recognizer.SpeechRecognitionRejected += OnSpeechRecognitionRejected;
                    _recognizer.AudioLevelUpdated += OnAudioLevelUpdated;
                    _recognizer.RecognizerUpdateReached += OnRecognizerUpdateReached;

                    // Configure recognition settings
                    System.Diagnostics.Debug.WriteLine("SpeechRecognitionService: Configuring recognition settings...");
                    _recognizer.InitialSilenceTimeout = TimeSpan.FromSeconds(5);
                    _recognizer.BabbleTimeout = TimeSpan.FromSeconds(2);
                    _recognizer.EndSilenceTimeout = TimeSpan.FromSeconds(1);
                    _recognizer.EndSilenceTimeoutAmbiguous = TimeSpan.FromSeconds(1.5);

                    // Set input to default microphone
                    System.Diagnostics.Debug.WriteLine("SpeechRecognitionService: Setting input to default audio device...");
                    _recognizer.SetInputToDefaultAudioDevice();
                });

                System.Diagnostics.Debug.WriteLine("SpeechRecognitionService: Initialization completed successfully");
                OnStateChanged(SpeechRecognitionState.Stopped);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SpeechRecognitionService: Initialization failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                OnSpeechRecognitionError($"Failed to initialize speech recognition: {ex.Message}", ex);
                OnStateChanged(SpeechRecognitionState.Error);
                return false;
            }
        }

        public async Task<bool> StartListeningAsync()
        {
            System.Diagnostics.Debug.WriteLine($"SpeechRecognitionService: StartListeningAsync called. IsDisposed: {_isDisposed}, Recognizer null: {_recognizer == null}, IsListening: {IsListening}");
            
            if (_isDisposed || _recognizer == null || IsListening)
            {
                System.Diagnostics.Debug.WriteLine("SpeechRecognitionService: StartListening preconditions failed");
                return false;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("SpeechRecognitionService: Changing state to Starting");
                OnStateChanged(SpeechRecognitionState.Starting);

                await Task.Run(() =>
                {
                    System.Diagnostics.Debug.WriteLine("SpeechRecognitionService: Calling RecognizeAsync");
                    _recognizer.RecognizeAsync(RecognizeMode.Multiple);
                });

                IsListening = true;
                System.Diagnostics.Debug.WriteLine("SpeechRecognitionService: Changing state to Listening");
                OnStateChanged(SpeechRecognitionState.Listening);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SpeechRecognitionService: StartListening failed: {ex.Message}");
                OnSpeechRecognitionError($"Failed to start speech recognition: {ex.Message}", ex);
                OnStateChanged(SpeechRecognitionState.Error);
                return false;
            }
        }

        public async Task StopListeningAsync()
        {
            if (_isDisposed || _recognizer == null || !IsListening)
                return;

            try
            {
                OnStateChanged(SpeechRecognitionState.Stopping);

                await Task.Run(() =>
                {
                    _recognizer.RecognizeAsyncCancel();
                });

                IsListening = false;
                OnStateChanged(SpeechRecognitionState.Stopped);
            }
            catch (Exception ex)
            {
                OnSpeechRecognitionError($"Failed to stop speech recognition: {ex.Message}", ex);
                OnStateChanged(SpeechRecognitionState.Error);
            }
        }

        private void OnSpeechRecognized(object sender, System.Speech.Recognition.SpeechRecognizedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"SpeechRecognitionService: OnSpeechRecognized called. Text: '{e.Result.Text}', Confidence: {e.Result.Confidence}");
            
            if (_isDisposed)
            {
                System.Diagnostics.Debug.WriteLine("SpeechRecognitionService: Service is disposed, ignoring speech recognition");
                return;
            }

            // Only process if confidence is reasonable
            if (e.Result.Confidence >= 0.3f)
            {
                System.Diagnostics.Debug.WriteLine($"SpeechRecognitionService: Confidence {e.Result.Confidence} meets threshold, raising event");
                var args = new SpeechRecognizedEventArgs(e.Result.Text, e.Result.Confidence);
                
                // Raise event directly - UI threading will be handled by the consuming application
                SpeechRecognized?.Invoke(this, args);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"SpeechRecognitionService: Confidence {e.Result.Confidence} below threshold (0.3), ignoring");
            }
        }

        private void OnSpeechRecognitionRejected(object sender, System.Speech.Recognition.SpeechRecognitionRejectedEventArgs e)
        {
            if (_isDisposed)
                return;

            // Log low confidence recognition attempts but don't raise error
            System.Diagnostics.Debug.WriteLine($"Speech recognition rejected: {e.Result?.Text} (Confidence: {e.Result?.Confidence})");
        }

        private void OnAudioLevelUpdated(object sender, AudioLevelUpdatedEventArgs e)
        {
            // Can be used for visual audio level indicators in the future
            System.Diagnostics.Debug.WriteLine($"Audio Level: {e.AudioLevel}");
        }

        private void OnRecognizerUpdateReached(object sender, RecognizerUpdateReachedEventArgs e)
        {
            // Handle recognizer updates if needed
        }

        private void OnSpeechRecognitionError(string message, Exception exception = null)
        {
            if (_isDisposed)
                return;

            var args = new SpeechRecognitionErrorEventArgs(message, exception);
            
            // Raise event directly - UI threading will be handled by the consuming application
            SpeechRecognitionError?.Invoke(this, args);
        }

        private void OnStateChanged(SpeechRecognitionState state)
        {
            if (_isDisposed)
                return;

            var args = new SpeechRecognitionStateEventArgs(state);
            
            // Raise event directly - UI threading will be handled by the consuming application
            StateChanged?.Invoke(this, args);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            if (_recognizer != null)
            {
                if (IsListening)
                {
                    try
                    {
                        _recognizer.RecognizeAsyncCancel();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error stopping recognition during disposal: {ex.Message}");
                    }
                }

                try
                {
                    _recognizer.SpeechRecognized -= OnSpeechRecognized;
                    _recognizer.SpeechRecognitionRejected -= OnSpeechRecognitionRejected;
                    _recognizer.AudioLevelUpdated -= OnAudioLevelUpdated;
                    _recognizer.RecognizerUpdateReached -= OnRecognizerUpdateReached;
                    
                    _recognizer.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error disposing recognizer: {ex.Message}");
                }
                
                _recognizer = null;
            }

            IsListening = false;
        }
    }
}