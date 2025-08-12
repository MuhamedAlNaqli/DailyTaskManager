using DailyTaskManager.Core.Models;
using System.Data.Entity;

namespace DailyTaskManager.Core
{
    public class DailyTaskManagerDbContext : DbContext
    {
        public DailyTaskManagerDbContext() : base("PostgresConnection") { }

        public DbSet<DailyTask> DailyTasks { get; set; }
    }
}
