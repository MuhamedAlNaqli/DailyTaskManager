using System.Threading.Tasks;

namespace DailyTaskManager.Core.Interfaces
{
    /// <summary>
    /// Keeps a reminder running on the operating system while the app itself is closed
    /// </summary>
    public interface IBackgroundReminderService
    {
        /// <summary>False on platforms where no background reminder can be installed</summary>
        bool IsSupported { get; }

        /// <summary>Whether the operating system currently has the reminder job registered</summary>
        Task<bool> IsInstalledAsync();

        /// <summary>Registers the reminder job (replacing an outdated one) or removes it</summary>
        Task SetInstalledAsync(bool installed);

        /// <summary>Shows a sample notification right away so the user can see how it looks</summary>
        Task SendTestNotificationAsync();
    }
}
