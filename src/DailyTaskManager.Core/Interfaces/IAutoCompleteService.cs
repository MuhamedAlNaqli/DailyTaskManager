using System.Collections.Generic;
using System.Threading.Tasks;

namespace DailyTaskManager.Core.Interfaces
{
    /// <summary>
    /// Service for providing auto-complete suggestions for task descriptions
    /// </summary>
    public interface IAutoCompleteService
    {
        /// <summary>
        /// Gets auto-complete suggestions based on user input
        /// </summary>
        /// <param name="input">The partial text input from user</param>
        /// <param name="maxResults">Maximum number of suggestions to return (default: 10)</param>
        /// <returns>List of matching suggestions</returns>
        Task<List<string>> GetSuggestionsAsync(string input, int maxResults = 10);

        /// <summary>
        /// Refreshes the internal cache of available descriptions
        /// </summary>
        Task RefreshCacheAsync();

        /// <summary>
        /// Clears the internal cache
        /// </summary>
        void ClearCache();
    }
}