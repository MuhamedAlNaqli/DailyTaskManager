using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Infrastructure.Data
{
    /// <summary>
    /// Entity Framework Core database context for DailyTaskManager
    /// </summary>
    public class DailyTaskManagerDbContext : DbContext
    {
        private readonly IConfiguration? _configuration;

        public DailyTaskManagerDbContext(DbContextOptions<DailyTaskManagerDbContext> options) : base(options)
        {
        }

        public DailyTaskManagerDbContext(DbContextOptions<DailyTaskManagerDbContext> options, IConfiguration configuration) 
            : base(options)
        {
            _configuration = configuration;
        }

        // DbSets for entity types
        public DbSet<User> Users { get; set; }
        public DbSet<DailyTask> DailyTasks { get; set; }
        public DbSet<Setting> Settings { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured && _configuration != null)
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (!string.IsNullOrEmpty(connectionString))
                {
                    optionsBuilder.UseNpgsql(connectionString);
                }
            }
        }

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
                
                // Configure timestamps
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(u => u.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                // Configure relationships
                entity.HasMany(u => u.DailyTasks)
                      .WithOne(d => d.User)
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure DailyTask entity
            modelBuilder.Entity<DailyTask>(entity =>
            {
                entity.ToTable("daily_tasks", "public");
                
                // Configure indexes
                entity.HasIndex(d => d.UserId);
                entity.HasIndex(d => d.Date);
                entity.HasIndex(d => new { d.UserId, d.Date });
                
                // Configure properties
                entity.Property(d => d.Description).HasMaxLength(1000);
                entity.Property(d => d.Hours).HasColumnType("decimal(5,2)");
                
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
                entity.Property(s => s.Value).HasMaxLength(1000).IsRequired();
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
        /// Applies pending migrations to the database
        /// </summary>
        public async Task MigrateDatabaseAsync()
        {
            await Database.MigrateAsync();
            
            // Fix schema issues if needed
            await FixSchemaIssuesAsync();
        }
        
        /// <summary>
        /// Fix database schema issues by adding missing columns
        /// </summary>
        private async Task FixSchemaIssuesAsync()
        {
            try
            {
                // Check if created_at and updated_at columns exist in daily_tasks table
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
                
                // Update existing records to have timestamps if they're null
                await Database.ExecuteSqlRawAsync(@"
                    UPDATE public.daily_tasks 
                    SET created_at = CURRENT_TIMESTAMP 
                    WHERE created_at IS NULL");
                    
                await Database.ExecuteSqlRawAsync(@"
                    UPDATE public.daily_tasks 
                    SET updated_at = CURRENT_TIMESTAMP 
                    WHERE updated_at IS NULL");
            }
            catch (Exception ex)
            {
                // If schema fix fails, we might need to recreate the database
                Console.WriteLine($"Schema fix failed: {ex.Message}. Recreating database...");
                await Database.EnsureDeletedAsync();
                await Database.EnsureCreatedAsync();
            }
        }
    }
}