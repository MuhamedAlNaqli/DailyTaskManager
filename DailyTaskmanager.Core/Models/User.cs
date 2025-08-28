using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DailyTaskManager.Core.Models
{
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
        public string Username { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("Email")]
        public string Email { get; set; }

        [MaxLength(255)]
        [Column("PasswordHash")]
        public string PasswordHash { get; set; }

        [MaxLength(50)]
        [Column("OAuthProvider")]
        public string OAuthProvider { get; set; } // "google", "microsoft", null for local auth

        [MaxLength(255)]
        [Column("OAuthId")]
        public string OAuthId { get; set; }

        [MaxLength(100)]
        [Column("DisplayName")]
        public string DisplayName { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("UpdatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("IsActive")]
        public bool IsActive { get; set; } = true;
    }
}