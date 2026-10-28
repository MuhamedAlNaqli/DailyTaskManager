using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Infrastructure.Data
{
    /// <summary>
    /// Entity Framework Core database context for DailyTaskManager
    /// </summary>
    public class DailyTaskManagerDbContext : DbContext
    {
        public DailyTaskManagerDbContext(DbContextOptions<DailyTaskManagerDbContext> options) : base(options)
        {
        }

        // DbSets for entity types
        public DbSet<User> Users { get; set; }
        public DbSet<DailyTask> DailyTasks { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<Client> Clients { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure default schema
            modelBuilder.HasDefaultSchema("public");

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users", "public");
                
                // Configure indexes
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.OAuthProvider);
                entity.HasIndex(u => u.OAuthId);
                
                // Configure properties
                entity.Property(u => u.Username).HasMaxLength(100).IsRequired();
                entity.Property(u => u.Email).HasMaxLength(255).IsRequired();
                entity.Property(u => u.PasswordHash).HasMaxLength(255);
                entity.Property(u => u.OAuthProvider).HasMaxLength(50);
                entity.Property(u => u.OAuthId).HasMaxLength(255);
                entity.Property(u => u.DisplayName).HasMaxLength(100);
                entity.Property(u => u.RememberTokenHash).HasMaxLength(100);
                
                // Configure timestamps
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(u => u.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                // Configure relationships
                entity.HasMany(u => u.DailyTasks)
                      .WithOne(d => d.User)
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Client entity
            modelBuilder.Entity<Client>(entity =>
            {
                entity.ToTable("clients", "public");
                entity.HasIndex(c => new { c.UserId, c.Name }).IsUnique();
                entity.Property(c => c.Name).HasMaxLength(150).IsRequired();
                entity.Property(c => c.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne<User>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            // Configure DailyTask entity
            modelBuilder.Entity<DailyTask>(entity =>
            {
                entity.ToTable("daily_tasks", "public");
                
                // Configure indexes
                entity.HasIndex(d => d.UserId);
                entity.HasIndex(d => d.Date);
                entity.HasIndex(d => new { d.UserId, d.Date });
                entity.HasIndex(d => d.ClientId);
                entity.HasOne(d => d.Client).WithMany().HasForeignKey(d => d.ClientId).OnDelete(DeleteBehavior.SetNull);
                
                // Configure properties
                entity.Property(d => d.Description).HasMaxLength(1000);
                entity.Property(d => d.Hours).HasColumnType("decimal(5,2)");
                // The column is a plain SQL date; mapping it as one keeps range filters free of time zone shifts
                entity.Property(d => d.Date).HasColumnType("date");
                
                // Configure timestamps
                entity.Property(d => d.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(d => d.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Configure Setting entity
            modelBuilder.Entity<Setting>(entity =>
            {
                entity.ToTable("settings", "public");
                
                // Configure indexes
                entity.HasIndex(s => s.Key).IsUnique();
                
                // Configure properties
                entity.Property(s => s.Key).HasMaxLength(100).IsRequired();
                entity.Property(s => s.Value).HasMaxLength(4000).IsRequired();
                entity.Property(s => s.Description).HasMaxLength(500);
            });

            // Seed default data
            SeedData(modelBuilder);
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            // Seed default settings
            modelBuilder.Entity<Setting>().HasData(
                new Setting 
                { 
                    Id = 1, 
                    Key = "DefaultHours", 
                    Value = "7", 
                    Description = "Default hours for new tasks",
                    IsSystem = true 
                },
                new Setting 
                { 
                    Id = 2, 
                    Key = "DateFormat", 
                    Value = "yyyy-MM-dd", 
                    Description = "Date display format",
                    IsSystem = true 
                },
                new Setting 
                { 
                    Id = 3, 
                    Key = "Theme", 
                    Value = "Light", 
                    Description = "Application theme (Light/Dark)",
                    IsSystem = false 
                }
            );
        }

        /// <summary>
        /// Saves changes and automatically updates timestamp fields
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified)
                .Select(e => e.Entity);

            foreach (var entity in entries)
            {
                if (entity is DailyTask task)
                {
                    task.UpdateTimestamp();
                }
                else if (entity is User user)
                {
                    user.UpdateTimestamp();
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Ensures the database is created and migrations are applied
        /// </summary>
        public async Task EnsureDatabaseCreatedAsync()
        {
            await Database.EnsureCreatedAsync();
        }

        /// <summary>
        /// Applies pending migrations, or creates the schema when the project has no migrations
        /// </summary>
        public async Task MigrateDatabaseAsync()
        {
            if (Database.GetMigrations().Any())
            {
                await Database.MigrateAsync();
            }
            else
            {
                await Database.EnsureCreatedAsync();
            }

            // Fix schema issues if needed
            await FixSchemaIssuesAsync();
        }

        /// <summary>
        /// Adds missing timestamp columns to an existing daily_tasks table.
        /// Never drops data: on failure the error is propagated.
        /// </summary>
        private async Task FixSchemaIssuesAsync()
        {
            var sql = @"
                SELECT column_name
                FROM information_schema.columns
                WHERE table_schema = 'public'
                AND table_name = 'daily_tasks'
                AND column_name IN ('created_at', 'updated_at')";

            var existingColumns = await Database.SqlQueryRaw<string>(sql).ToListAsync();

            if (!existingColumns.Contains("created_at"))
            {
                await Database.ExecuteSqlRawAsync(
                    "ALTER TABLE public.daily_tasks ADD COLUMN created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP");
            }

            if (!existingColumns.Contains("updated_at"))
            {
                await Database.ExecuteSqlRawAsync(
                    "ALTER TABLE public.daily_tasks ADD COLUMN updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP");
            }

            // "Remember me" session columns on existing databases
            // Settings values were originally limited to 1000 characters.
            // All idempotent statements go out in one round trip instead of five.
            await Database.ExecuteSqlRawAsync(@"
                ALTER TABLE IF EXISTS public.users ADD COLUMN IF NOT EXISTS ""RememberTokenHash"" character varying(100);
                ALTER TABLE IF EXISTS public.users ADD COLUMN IF NOT EXISTS ""RememberTokenExpiresAt"" timestamp with time zone;
                ALTER TABLE IF EXISTS public.settings ALTER COLUMN value TYPE character varying(4000);
                DO $$
                BEGIN
                    -- Older databases stored hours as a whole number, which silently rounded half hours
                    IF EXISTS (SELECT 1 FROM information_schema.columns
                               WHERE table_schema = 'public' AND table_name = 'daily_tasks'
                                 AND column_name = 'total_hours' AND data_type = 'integer') THEN
                        ALTER TABLE public.daily_tasks ALTER COLUMN total_hours TYPE numeric(5,2);
                    END IF;
                END $$;
                CREATE TABLE IF NOT EXISTS public.clients (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                    ""UserId"" integer NOT NULL REFERENCES public.users (""Id"") ON DELETE CASCADE,
                    ""Name"" character varying(150) NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP);
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_clients_UserId_Name"" ON public.clients (""UserId"", ""Name"");
                ALTER TABLE IF EXISTS public.daily_tasks ADD COLUMN IF NOT EXISTS ""ClientId"" integer REFERENCES public.clients (""Id"") ON DELETE SET NULL;
                CREATE INDEX IF NOT EXISTS ""IX_daily_tasks_ClientId"" ON public.daily_tasks (""ClientId"");
                UPDATE public.daily_tasks SET created_at = CURRENT_TIMESTAMP WHERE created_at IS NULL;
                UPDATE public.daily_tasks SET updated_at = CURRENT_TIMESTAMP WHERE updated_at IS NULL;");

            // Composite index matching the list query (filter by user, newest first) so paging stays fast as the table grows
            await Database.ExecuteSqlRawAsync(
                "CREATE INDEX IF NOT EXISTS ix_daily_tasks_user_date_created ON public.daily_tasks (\"UserId\", task_date DESC, created_at DESC)");
        }
    }
}
