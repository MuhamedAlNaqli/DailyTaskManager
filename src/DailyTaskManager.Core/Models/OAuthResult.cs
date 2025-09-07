namespace DailyTaskManager.Core.Models
{
    /// <summary>
    /// Result object for OAuth authentication operations
    /// </summary>
    public class OAuthResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
        public string? ProviderId { get; set; }
        public string? Provider { get; set; } // "google" or "microsoft"

        public static OAuthResult Success(string email, string displayName, string providerId, string provider)
        {
            return new OAuthResult
            {
                IsSuccess = true,
                Email = email,
                DisplayName = displayName,
                ProviderId = providerId,
                Provider = provider
            };
        }

        public static OAuthResult Failure(string errorMessage)
        {
            return new OAuthResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }

        public override string ToString() => 
            IsSuccess ? $"OAuth Success: {Email} via {Provider}" : $"OAuth Failed: {ErrorMessage}";
    }
}