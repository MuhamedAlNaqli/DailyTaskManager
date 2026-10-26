namespace DailyTaskManager.Core.Interfaces
{
    /// <summary>
    /// A "remember me" session stored on this machine
    /// </summary>
    public class RememberedSession
    {
        public int UserId { get; set; }

        /// <summary>
        /// Random secret; only its hash is stored in the database
        /// </summary>
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>
    /// Local, per-user storage for the "remember me" session
    /// </summary>
    public interface ISessionStore
    {
        RememberedSession? Load();

        void Save(RememberedSession session);

        void Clear();
    }
}
