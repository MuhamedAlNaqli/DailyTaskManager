using System.Threading.Tasks;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Core.Services
{
    public interface IAuthenticationService
    {
        Task<AuthResult> LoginAsync(LoginRequest request);
        Task<AuthResult> RegisterAsync(string username, string email, string password, string displayName);
        Task<AuthResult> AuthenticateWithOAuthAsync(OAuthResult oAuthResult);
        Task LogoutAsync();
        bool IsAuthenticated { get; }
        User CurrentUser { get; }
        event System.EventHandler<User> UserLoggedIn;
        event System.EventHandler UserLoggedOut;
    }
}