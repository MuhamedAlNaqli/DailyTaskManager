using System;

namespace DailyTaskManager.Core.Models
{
    /// <summary>
    /// A client that time entries can be assigned to
    /// </summary>
    public class Client
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Name { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public override string ToString() => Name;
    }
}
