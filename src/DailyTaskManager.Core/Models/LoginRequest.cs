using System.ComponentModel.DataAnnotations;

namespace DailyTaskManager.Core.Models
{
    /// <summary>
    /// Request object for user login operations
    /// </summary>
    public class LoginRequest
    {
        [Required(ErrorMessage = "Username is required")]
        [MaxLength(100)]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = "";

        public bool RememberMe { get; set; } = false;

        public override string ToString() => $"Login request for: {Username}";
    }
}