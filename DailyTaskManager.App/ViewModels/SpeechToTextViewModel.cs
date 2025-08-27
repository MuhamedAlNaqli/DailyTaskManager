using DailyTaskManager.Core.Services;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

namespace DailyTaskManager.App.ViewModels
{
    public class SpeechToTextViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ISpeechRecognitionService _speechService;
        private string _recognizedText = string.Empty;
        private string _statusMessage = "Ready";
        private bool _isListening;
        private bool _isInitialized;
        private string _buttonText = "Start Dictation";
        private bool _isEnabled = true;
        private float _confidence;

        public SpeechToTextViewModel() : this(DailyTaskManager.Core.Services.ServiceContainer.Instance.IsRegistered<ISpeechRecognitionService>() 
            ? DailyTaskManager.Core.Services.ServiceContainer.Instance.Resolve<ISpeechRecognitionService>() 
            : new SpeechRecognitionService())
        {
        }

        public SpeechToTextViewModel(ISpeechRecognitionService speechService)
        {
            _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
            
            ToggleDictationCommand = new RelayCommand(async _ => await ToggleDictationAsync(), _ => IsEnabled);
            ClearTextCommand = new RelayCommand(ClearText, _ => !string.IsNullOrEmpty(RecognizedText));
            InitializeCommand = new RelayCommand(async _ => await InitializeAsync());

            // Subscribe to speech service events
            _speechService.SpeechRecognized += OnSpeechRecognized;
            _speechService.SpeechRecognitionError += OnSpeechRecognitionError;
            _speechService.StateChanged += OnStateChanged;

            // Initialize automatically
            _ = InitializeAsync();
        }

        #region Properties

        public string RecognizedText
        {
            get => _recognizedText;
            set
            {
                if (_recognizedText != value)
                {
                    _recognizedText = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasText));
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsListening
        {
            get => _isListening;
            private set
            {
                if (_isListening != value)
                {
                    _isListening = value;
                    OnPropertyChanged();
                    UpdateButtonText();
                }
            }
        }

        public bool IsInitialized
        {
            get => _isInitialized;
            private set
            {
                if (_isInitialized != value)
                {
                    _isInitialized = value;
                    OnPropertyChanged();
                    IsEnabled = value;
                }
            }
        }

        public string ButtonText
        {
            get => _buttonText;
            private set
            {
                if (_buttonText != value)
                {
                    _buttonText = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            private set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public float Confidence
        {
            get => _confidence;
            private set
            {
                if (Math.Abs(_confidence - value) > 0.01f)
                {
                    _confidence = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ConfidencePercentage));
                }
            }
        }

        public string ConfidencePercentage => $"{Confidence * 100:F0}%";

        public bool HasText => !string.IsNullOrEmpty(RecognizedText);

        public bool SpeechServiceAvailable => _speechService?.IsAvailable == true;

        #endregion

        #region Commands

        public ICommand ToggleDictationCommand { get; }
        public ICommand ClearTextCommand { get; }
        public ICommand InitializeCommand { get; }

        #endregion

        #region Private Methods

        private async Task InitializeAsync()
        {
            try
            {
                IsEnabled = false;
                StatusMessage = "Initializing speech recognition...";

                var success = await _speechService.InitializeAsync();
                
                if (success)
                {
                    IsInitialized = true;
                    StatusMessage = "Speech recognition ready";
                }
                else
                {
                    StatusMessage = "Failed to initialize speech recognition";
                    ShowError("Speech recognition initialization failed. Please check your microphone settings.");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Initialization error";
                ShowError($"Error initializing speech recognition: {ex.Message}");
            }
        }

        private async Task ToggleDictationAsync()
        {
            if (!IsInitialized)
            {
                await InitializeAsync();
                return;
            }

            try
            {
                if (IsListening)
                {
                    await StopDictationAsync();
                }
                else
                {
                    await StartDictationAsync();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Operation failed";
                ShowError($"Error toggling dictation: {ex.Message}");
            }
        }

        private async Task StartDictationAsync()
        {
            IsEnabled = false;
            StatusMessage = "Starting dictation...";
            
            var success = await _speechService.StartListeningAsync();
            
            if (!success)
            {
                StatusMessage = "Failed to start dictation";
                ShowError("Could not start speech recognition. Please check your microphone.");
                IsEnabled = true;
            }
        }

        private async Task StopDictationAsync()
        {
            IsEnabled = false;
            StatusMessage = "Stopping dictation...";
            
            await _speechService.StopListeningAsync();
        }

        private void ClearText(object parameter)
        {
            RecognizedText = string.Empty;
            Confidence = 0;
            StatusMessage = IsListening ? "Listening..." : "Ready";
        }

        private void UpdateButtonText()
        {
            ButtonText = IsListening ? "Stop Dictation" : "Start Dictation";
        }

        private void ShowError(string message)
        {
            // Using MessageBox for now - in a more sophisticated app you might use a dialog service
            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                MessageBox.Show(message, "Speech Recognition Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }));
        }

        #endregion

        #region Event Handlers

        private void OnSpeechRecognized(object sender, DailyTaskManager.Core.Services.SpeechRecognizedEventArgs e)
        {
            // Append recognized text (with space if not empty)
            if (!string.IsNullOrEmpty(RecognizedText))
            {
                RecognizedText += " " + e.Text;
            }
            else
            {
                RecognizedText = e.Text;
            }

            Confidence = e.Confidence;
            StatusMessage = $"Recognized: \"{e.Text}\" ({ConfidencePercentage})";
        }

        private void OnSpeechRecognitionError(object sender, DailyTaskManager.Core.Services.SpeechRecognitionErrorEventArgs e)
        {
            StatusMessage = "Recognition error occurred";
            ShowError(e.ErrorMessage);
        }

        private void OnStateChanged(object sender, DailyTaskManager.Core.Services.SpeechRecognitionStateEventArgs e)
        {
            switch (e.State)
            {
                case DailyTaskManager.Core.Services.SpeechRecognitionState.Starting:
                    StatusMessage = "Starting recognition...";
                    IsEnabled = false;
                    break;
                    
                case DailyTaskManager.Core.Services.SpeechRecognitionState.Listening:
                    IsListening = true;
                    StatusMessage = "Listening... Speak now!";
                    IsEnabled = true;
                    break;
                    
                case DailyTaskManager.Core.Services.SpeechRecognitionState.Stopping:
                    StatusMessage = "Stopping recognition...";
                    IsEnabled = false;
                    break;
                    
                case DailyTaskManager.Core.Services.SpeechRecognitionState.Stopped:
                    IsListening = false;
                    StatusMessage = IsInitialized ? "Ready" : "Not initialized";
                    IsEnabled = IsInitialized;
                    break;
                    
                case DailyTaskManager.Core.Services.SpeechRecognitionState.Error:
                    IsListening = false;
                    StatusMessage = "Error occurred";
                    IsEnabled = false;
                    break;
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_speechService != null)
            {
                _speechService.SpeechRecognized -= OnSpeechRecognized;
                _speechService.SpeechRecognitionError -= OnSpeechRecognitionError;
                _speechService.StateChanged -= OnStateChanged;
                
                _speechService.Dispose();
            }
        }

        #endregion
    }
}