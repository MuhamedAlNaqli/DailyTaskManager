using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DailyTaskManager.Core.Models
{
    /// <summary>
    /// Represents a user in the system with support for local and OAuth authentication
    /// </summary>
    [Table("users", Schema = "public")]
    public class User
    {
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("Username")]
        public string Username { get; set; } = "";

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        [Column("Email")]
        public string Email { get; set; } = "";

        [MaxLength(255)]
        [Column("PasswordHash")]
        public string? PasswordHash { get; set; }

        [MaxLength(50)]
        [Column("OAuthProvider")]
        public string? OAuthProvider { get; set; } // "google", "microsoft", null for local auth

        [MaxLength(255)]
        [Column("OAuthId")]
        public string? OAuthId { get; set; }

        [MaxLength(100)]
        [Column("DisplayName")]
        public string? DisplayName { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("UpdatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>SHA-256 hash of the "remember me" token (the token itself is only kept on the user's machine)</summary>
        [MaxLength(100)]
        [Column("RememberTokenHash")]
        public string? RememberTokenHash { get; set; }

        [Column("RememberTokenExpiresAt")]
        public DateTime? RememberTokenExpiresAt { get; set; }

        [Column("IsActive")]
        public bool IsActive { get; set; } = true;

        // Navigation properties (will be configured in EF Core)
        public virtual ICollection<DailyTask> DailyTasks { get; set; } = new List<DailyTask>();

        // Helper methods
        public bool IsOAuthUser => !string.IsNullOrEmpty(OAuthProvider);
        
        public bool IsLocalUser => string.IsNullOrEmpty(OAuthProvider);

        public string GetDisplayName() => DisplayName ?? Username;

        public void UpdateTimestamp() => UpdatedAt = DateTime.UtcNow;

        public override string ToString() => $"{Username} ({Email})";
    }
}