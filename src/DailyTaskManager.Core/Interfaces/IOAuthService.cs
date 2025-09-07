using System.Threading.Tasks;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Core.Interfaces
{
    /// <summary>
    /// Interface for OAuth authentication services
    /// </summary>
    public interface IOAuthService
    {
        /// <summary>
        /// Authenticates user with Google OAuth
        /// </summary>
        Task<OAuthResult> AuthenticateGoogleAsync();

        /// <summary>
        /// Authenticates user with Microsoft OAuth
        /// </summary>
        Task<OAuthResult> AuthenticateMicrosoftAsync();
    }
}