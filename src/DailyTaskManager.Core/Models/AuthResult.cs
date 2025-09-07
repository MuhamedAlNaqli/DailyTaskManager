namespace DailyTaskManager.Core.Models
{
    /// <summary>
    /// Result object for authentication operations
    /// </summary>
    public class AuthResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public User? User { get; set; }

        public static AuthResult Success(User user)
        {
            return new AuthResult
            {
                IsSuccess = true,
                User = user
            };
        }

        public static AuthResult Failure(string errorMessage)
        {
            return new AuthResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }

        public override string ToString() => 
            IsSuccess ? $"Success: {User?.Username}" : $"Failed: {ErrorMessage}";
    }
}