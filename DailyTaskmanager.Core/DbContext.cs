using DailyTaskManager.Core.Models;
using System.Data.Entity;

namespace DailyTaskManager.Core
{
    public class DailyTaskManagerDbContext : DbContext
    {
        public DailyTaskManagerDbContext() : base("PostgresConnection") 
        {
            // Configure PostgreSQL to use migrations instead of initializer
            Database.SetInitializer<DailyTaskManagerDbContext>(null);
        }

        public DbSet<User> Users { get; set; }
        public DbSet<DailyTask> DailyTasks { get; set; }
        public DbSet<Setting> Settings { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Configure default schema
            modelBuilder.HasDefaultSchema("public");

            // Configure relationships
            modelBuilder.Entity<DailyTask>()
                .HasRequired(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .WillCascadeOnDelete(false);


            // Ensure unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Configure table names explicitly to ensure proper schema
            modelBuilder.Entity<User>().ToTable("users", "public");
            modelBuilder.Entity<DailyTask>().ToTable("daily_tasks", "public");
            modelBuilder.Entity<Setting>().ToTable("settings", "public");

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
