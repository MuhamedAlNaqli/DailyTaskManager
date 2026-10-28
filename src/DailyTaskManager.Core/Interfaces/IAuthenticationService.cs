using System;
using System.Threading.Tasks;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Core.Interfaces
{
    /// <summary>
    /// Interface for user authentication operations
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Gets the currently authenticated user
        /// </summary>
        User? CurrentUser { get; }

        /// <summary>
        /// Indicates whether a user is currently authenticated
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Authenticates a user with username/password
        /// </summary>
        Task<AuthResult> LoginAsync(LoginRequest request);

        /// <summary>
        /// Authenticates a user via OAuth
        /// </summary>
        Task<AuthResult> LoginWithOAuthAsync(OAuthResult oauthResult);

        /// <summary>
        /// Registers a new user account
        /// </summary>
        Task<AuthResult> RegisterAsync(string username, string email, string password);

        /// <summary>
        /// Logs out the current user
        /// </summary>
        Task LogoutAsync();

        /// <summary>
        /// Signs in using the "remember me" session saved on this machine, if it is still valid.
        /// Background jobs pass <c>false</c> so that they do not keep the session alive on their own.
        /// </summary>
        Task<AuthResult> RestoreSessionAsync(bool extendExpiry = true);

        /// <summary>
        /// Remembers the currently signed-in user on this machine
        /// </summary>
        Task EnableRememberMeAsync();

        /// <summary>
        /// Forgets any saved "remember me" session
        /// </summary>
        Task ClearRememberedSessionAsync();

        /// <summary>
        /// Changes the current user's password
        /// </summary>
        Task<bool> ChangePasswordAsync(string currentPassword, string newPassword);

        /// <summary>
        /// Validates a user's credentials
        /// </summary>
        Task<bool> ValidateCredentialsAsync(string username, string password);

        /// <summary>
        /// Refreshes the current user's data
        /// </summary>
        Task RefreshCurrentUserAsync();

        /// <summary>
        /// Event raised when authentication state changes
        /// </summary>
        event EventHandler<AuthResult>? AuthenticationChanged;

        /// <summary>
        /// Adds password authentication to an existing user (useful for OAuth users who want password login)
        /// </summary>
        Task<bool> AddPasswordToUserAsync(string username, string password);
    }
}