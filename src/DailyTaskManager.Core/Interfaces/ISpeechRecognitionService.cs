using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DailyTaskManager.Core.Interfaces
{
    /// <summary>
    /// Interface for speech recognition operations
    /// </summary>
    public interface ISpeechRecognitionService
    {
        /// <summary>
        /// Indicates whether speech recognition is supported on the current platform
        /// </summary>
        bool IsSupported { get; }

        /// <summary>
        /// Indicates whether the service is currently listening
        /// </summary>
        bool IsListening { get; }

        /// <summary>
        /// Starts speech recognition and returns the recognized text
        /// </summary>
        Task<string?> RecognizeSpeechAsync(int timeoutSeconds = 30);

        /// <summary>
        /// Starts continuous speech recognition
        /// </summary>
        Task StartContinuousRecognitionAsync();

        /// <summary>
        /// Stops continuous speech recognition
        /// </summary>
        Task StopContinuousRecognitionAsync();

        /// <summary>
        /// Event raised when speech is recognized during continuous recognition
        /// </summary>
        event EventHandler<string>? SpeechRecognized;

        /// <summary>
        /// Event raised when an error occurs during speech recognition
        /// </summary>
        event EventHandler<string>? RecognitionError;

        /// <summary>
        /// Gets available speech recognition languages
        /// </summary>
        Task<List<string>> GetAvailableLanguagesAsync();

        /// <summary>
        /// Sets the recognition language
        /// </summary>
        Task SetLanguageAsync(string languageCode);
    }
}