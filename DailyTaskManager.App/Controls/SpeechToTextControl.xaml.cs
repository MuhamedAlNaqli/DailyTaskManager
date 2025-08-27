using DailyTaskManager.Core.Services;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DailyTaskManager.App.Controls
{
    /// <summary>
    /// A reusable speech-to-text control that can be integrated into any form
    /// </summary>
    public partial class SpeechToTextControl : UserControl, INotifyPropertyChanged, IDisposable
    {
        private ISpeechRecognitionService _speechService;
        private bool _isListening;
        private Storyboard _listeningAnimation;
        private bool _isInitialized;

        public SpeechToTextControl()
        {
            InitializeComponent();
            // Don't set DataContext = this, let it inherit from parent for proper binding
            
            _listeningAnimation = (Storyboard)SpeechButton.Resources["ListeningAnimation"];
            InitializeSpeechService();
        }

        #region Dependency Properties

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(SpeechToTextControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty HintTextProperty =
            DependencyProperty.Register(nameof(HintText), typeof(string), typeof(SpeechToTextControl),
                new PropertyMetadata("Enter text or use speech recognition..."));

        public static readonly DependencyProperty TextWrappingProperty =
            DependencyProperty.Register(nameof(TextWrapping), typeof(TextWrapping), typeof(SpeechToTextControl),
                new PropertyMetadata(TextWrapping.NoWrap));

        public static readonly DependencyProperty AcceptsReturnProperty =
            DependencyProperty.Register(nameof(AcceptsReturn), typeof(bool), typeof(SpeechToTextControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty MaxLinesProperty =
            DependencyProperty.Register(nameof(MaxLines), typeof(int), typeof(SpeechToTextControl),
                new PropertyMetadata(1));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string HintText
        {
            get => (string)GetValue(HintTextProperty);
            set => SetValue(HintTextProperty, value);
        }

        public TextWrapping TextWrapping
        {
            get => (TextWrapping)GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        public bool AcceptsReturn
        {
            get => (bool)GetValue(AcceptsReturnProperty);
            set => SetValue(AcceptsReturnProperty, value);
        }

        public int MaxLines
        {
            get => (int)GetValue(MaxLinesProperty);
            set => SetValue(MaxLinesProperty, value);
        }

        #endregion

        #region Properties

        public bool IsListening
        {
            get => _isListening;
            private set
            {
                if (_isListening != value)
                {
                    _isListening = value;
                    OnPropertyChanged();
                    UpdateVisualState();
                }
            }
        }

        #endregion

        #region Private Methods

        private async void InitializeSpeechService()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("SpeechToTextControl: Initializing speech service...");
                _speechService = DailyTaskManager.Core.Services.ServiceContainer.Instance.IsRegistered<ISpeechRecognitionService>() 
                    ? DailyTaskManager.Core.Services.ServiceContainer.Instance.Resolve<ISpeechRecognitionService>() 
                    : new SpeechRecognitionService();
                    
                System.Diagnostics.Debug.WriteLine("SpeechToTextControl: Wiring up events...");
                _speechService.SpeechRecognized += OnSpeechRecognized;
                _speechService.SpeechRecognitionError += OnSpeechRecognitionError;
                _speechService.StateChanged += OnStateChanged;

                System.Diagnostics.Debug.WriteLine("SpeechToTextControl: Calling InitializeAsync...");
                var success = await _speechService.InitializeAsync();
                _isInitialized = success;
                
                System.Diagnostics.Debug.WriteLine($"SpeechToTextControl: Initialization result: {success}");

                if (!success)
                {
                    SpeechButton.IsEnabled = false;
                    SpeechButton.ToolTip = "Speech recognition not available";
                    System.Diagnostics.Debug.WriteLine("SpeechToTextControl: Speech recognition disabled due to initialization failure");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize speech service: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                SpeechButton.IsEnabled = false;
                SpeechButton.ToolTip = "Speech recognition not available";
            }
        }

        private async void SpeechButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"SpeechToTextControl: Button clicked. IsInitialized: {_isInitialized}, IsListening: {IsListening}");
            
            if (!_isInitialized)
            {
                System.Diagnostics.Debug.WriteLine("SpeechToTextControl: Not initialized, returning");
                return;
            }

            try
            {
                if (IsListening)
                {
                    System.Diagnostics.Debug.WriteLine("SpeechToTextControl: Stopping listening...");
                    await _speechService.StopListeningAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SpeechToTextControl: Starting listening...");
                    var success = await _speechService.StartListeningAsync();
                    System.Diagnostics.Debug.WriteLine($"SpeechToTextControl: StartListening result: {success}");
                    
                    if (!success)
                    {
                        MessageBox.Show("Failed to start speech recognition. Please check your microphone settings.",
                                      "Speech Recognition Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SpeechToTextControl: Exception in button click: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Speech Recognition Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateVisualState()
        {
            if (IsListening)
            {
                MicrophoneIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.MicrophoneOff;
                SpeechButton.Foreground = new SolidColorBrush(Colors.Red);
                SpeechButton.ToolTip = "Click to stop dictation";
                _listeningAnimation.Begin();
            }
            else
            {
                MicrophoneIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Microphone;
                SpeechButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF673AB7"));
                SpeechButton.ToolTip = "Click to start dictation";
                _listeningAnimation.Stop();
                MicrophoneIcon.Opacity = 1.0;
            }
        }

        #endregion

        #region Event Handlers

        private void OnSpeechRecognized(object sender, DailyTaskManager.Core.Services.SpeechRecognizedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"SpeechToTextControl: OnSpeechRecognized called with text: '{e.Text}', confidence: {e.Confidence}");
            
            Dispatcher.BeginInvoke(new Action(() =>
            {
                System.Diagnostics.Debug.WriteLine($"SpeechToTextControl: Current Text: '{Text}'");
                
                if (!string.IsNullOrEmpty(Text))
                {
                    Text += " " + e.Text;
                }
                else
                {
                    Text = e.Text;
                }
                
                System.Diagnostics.Debug.WriteLine($"SpeechToTextControl: Updated Text: '{Text}'");
            }));
        }

        private void OnSpeechRecognitionError(object sender, DailyTaskManager.Core.Services.SpeechRecognitionErrorEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                System.Diagnostics.Debug.WriteLine($"Speech recognition error: {e.ErrorMessage}");
            }));
        }

        private void OnStateChanged(object sender, DailyTaskManager.Core.Services.SpeechRecognitionStateEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"SpeechToTextControl: OnStateChanged called with state: {e.State}");
            
            Dispatcher.BeginInvoke(new Action(() =>
            {
                switch (e.State)
                {
                    case DailyTaskManager.Core.Services.SpeechRecognitionState.Listening:
                        IsListening = true;
                        System.Diagnostics.Debug.WriteLine("SpeechToTextControl: State changed to Listening");
                        break;
                    case DailyTaskManager.Core.Services.SpeechRecognitionState.Stopped:
                        IsListening = false;
                        System.Diagnostics.Debug.WriteLine("SpeechToTextControl: State changed to Stopped");
                        break;
                    case DailyTaskManager.Core.Services.SpeechRecognitionState.Error:
                        IsListening = false;
                        System.Diagnostics.Debug.WriteLine("SpeechToTextControl: State changed to Error");
                        break;
                }
            }));
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
                _speechService = null;
            }
        }

        #endregion
    }
}