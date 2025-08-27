using System.Threading.Tasks;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Core.Services
{
    public interface IOAuthService
    {
        Task<OAuthResult> AuthenticateGoogleAsync();
        Task<OAuthResult> AuthenticateMicrosoftAsync();
    }
}