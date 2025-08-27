using DailyTaskManager.Core.Models;
using System.Data.Entity;

namespace DailyTaskManager.Core
{
    public class DailyTaskManagerDbContext : DbContext
    {
        public DailyTaskManagerDbContext() : base("PostgresConnection") { }

        public DbSet<User> Users { get; set; }
        public DbSet<DailyTask> DailyTasks { get; set; }
        public DbSet<Setting> Settings { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Configure relationships
            modelBuilder.Entity<DailyTask>()
                .HasRequired(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Setting>()
                .HasRequired(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .WillCascadeOnDelete(false);

            // Ensure unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// Initializes the database schema. Creates database if it doesn't exist.
        /// </summary>
        public static void InitializeDatabase()
        {
            using (var context = new DailyTaskManagerDbContext())
            {
                // This will create the database if it doesn't exist
                context.Database.CreateIfNotExists();
            }
        }
    }
}
