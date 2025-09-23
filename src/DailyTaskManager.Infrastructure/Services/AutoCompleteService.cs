using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DailyTaskManager.Core.Interfaces;

namespace DailyTaskManager.Infrastructure.Services
{
    /// <summary>
    /// Service for providing auto-complete suggestions for task descriptions
    /// </summary>
    public class AutoCompleteService : IAutoCompleteService
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<AutoCompleteService> _logger;
        private List<string> _cachedDescriptions = new();
        private DateTime _lastCacheUpdate = DateTime.MinValue;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(10);

        public AutoCompleteService(
            IDatabaseService databaseService,
            ILogger<AutoCompleteService> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        public async Task<List<string>> GetSuggestionsAsync(string input, int maxResults = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(input) || input.Length < 2)
                {
                    return new List<string>();
                }

                await EnsureCacheIsValidAsync();

                var suggestions = FilterSuggestions(input.Trim(), maxResults);

                _logger.LogDebug("Retrieved {Count} suggestions for input '{Input}'",
                    suggestions.Count, input);

                return suggestions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting auto-complete suggestions for input '{Input}'", input);
                return new List<string>();
            }
        }

        public async Task RefreshCacheAsync()
        {
            try
            {
                _logger.LogDebug("Refreshing auto-complete cache");

                var descriptions = await _databaseService.GetUniqueDescriptionsAsync();
                _cachedDescriptions = descriptions;
                _lastCacheUpdate = DateTime.UtcNow;

                _logger.LogInformation("Auto-complete cache refreshed with {Count} descriptions",
                    descriptions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing auto-complete cache");
                throw;
            }
        }

        public void ClearCache()
        {
            _cachedDescriptions.Clear();
            _lastCacheUpdate = DateTime.MinValue;
            _logger.LogDebug("Auto-complete cache cleared");
        }

        private async Task EnsureCacheIsValidAsync()
        {
            if (!IsCacheValid())
            {
                await RefreshCacheAsync();
            }
        }

        private bool IsCacheValid()
        {
            return _cachedDescriptions.Any() &&
                   DateTime.UtcNow - _lastCacheUpdate < _cacheExpiry;
        }

        private List<string> FilterSuggestions(string input, int maxResults)
        {
            var inputLower = input.ToLowerInvariant();

            // Priority 1: Exact matches (case insensitive)
            var exactMatches = _cachedDescriptions
                .Where(d => d.Equals(input, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Priority 2: Starts with input
            var startsWithMatches = _cachedDescriptions
                .Where(d => d.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                .Except(exactMatches, StringComparer.OrdinalIgnoreCase)
                .OrderBy(d => d.Length) // Shorter matches first
                .ToList();

            // Priority 3: Contains input
            var containsMatches = _cachedDescriptions
                .Where(d => d.Contains(inputLower, StringComparison.OrdinalIgnoreCase))
                .Except(exactMatches.Concat(startsWithMatches), StringComparer.OrdinalIgnoreCase)
                .OrderBy(d => d.IndexOf(inputLower, StringComparison.OrdinalIgnoreCase)) // Earlier occurrence first
                .ThenBy(d => d.Length) // Then by length
                .ToList();

            // Combine all matches with priority order
            var allMatches = exactMatches
                .Concat(startsWithMatches)
                .Concat(containsMatches)
                .Take(maxResults)
                .ToList();

            return allMatches;
        }
    }
}