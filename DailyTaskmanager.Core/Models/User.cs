using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DailyTaskManager.Core.Models
{
    [Table("users", Schema = "public")]
    public class User
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("username")]
        public string Username { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("email")]
        public string Email { get; set; }

        [MaxLength(255)]
        [Column("password_hash")]
        public string PasswordHash { get; set; }

        [MaxLength(100)]
        [Column("salt")]
        public string Salt { get; set; }

        [MaxLength(50)]
        [Column("oauth_provider")]
        public string OAuthProvider { get; set; } // "google", "microsoft", null for local auth

        [MaxLength(255)]
        [Column("oauth_id")]
        public string OAuthId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("display_name")]
        public string DisplayName { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }
}