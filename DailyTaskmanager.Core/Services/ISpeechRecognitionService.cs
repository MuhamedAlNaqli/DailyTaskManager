using System;
using System.Threading.Tasks;

namespace DailyTaskManager.Core.Services
{
    public interface ISpeechRecognitionService
    {
        event EventHandler<SpeechRecognizedEventArgs> SpeechRecognized;
        event EventHandler<SpeechRecognitionErrorEventArgs> SpeechRecognitionError;
        event EventHandler<SpeechRecognitionStateEventArgs> StateChanged;

        bool IsListening { get; }
        bool IsAvailable { get; }
        
        Task<bool> StartListeningAsync();
        Task StopListeningAsync();
        Task<bool> InitializeAsync();
        void Dispose();
    }

    public class SpeechRecognizedEventArgs : EventArgs
    {
        public string Text { get; set; }
        public float Confidence { get; set; }
        
        public SpeechRecognizedEventArgs(string text, float confidence)
        {
            Text = text;
            Confidence = confidence;
        }
    }

    public class SpeechRecognitionErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }
        
        public SpeechRecognitionErrorEventArgs(string errorMessage, Exception exception = null)
        {
            ErrorMessage = errorMessage;
            Exception = exception;
        }
    }

    public class SpeechRecognitionStateEventArgs : EventArgs
    {
        public SpeechRecognitionState State { get; set; }
        
        public SpeechRecognitionStateEventArgs(SpeechRecognitionState state)
        {
            State = state;
        }
    }

    public enum SpeechRecognitionState
    {
        Stopped,
        Starting,
        Listening,
        Stopping,
        Error
    }
}