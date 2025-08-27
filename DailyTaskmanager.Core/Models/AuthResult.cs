namespace DailyTaskManager.Core.Models
{
    public class AuthResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public User User { get; set; }

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
    }
}