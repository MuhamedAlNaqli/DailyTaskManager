using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DailyTaskManager.Core.Models
{
    /// <summary>
    /// Represents a daily task with hours tracking
    /// </summary>
    [Table("daily_tasks", Schema = "public")]
    public class DailyTask
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("UserId")]
        public int UserId { get; set; }

        [Column("task_date")]
        public DateTime Date { get; set; } = DateTime.Today;

        [Column("description")]
        [MaxLength(1000)]
        public string Description { get; set; } = "";

        [Column("total_hours")]
        [Range(0.0, 24.0)]
        public decimal Hours { get; set; } = 7;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Optional client this entry is billed to (null = unassigned)</summary>
        [Column("ClientId")]
        public int? ClientId { get; set; }

        // Navigation property (will be configured in EF Core)
        public virtual User? User { get; set; }

        public virtual Client? Client { get; set; }

        // Helper methods
        public void UpdateTimestamp() => UpdatedAt = DateTime.UtcNow;

        public bool IsToday() => Date.Date == DateTime.Today;

        public override string ToString() => $"{Date:yyyy-MM-dd}: {Description} ({Hours}h)";
    }
}