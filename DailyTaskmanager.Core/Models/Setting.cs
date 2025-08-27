using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DailyTaskManager.Core.Models
{
    [Table("settings", Schema = "public")]
    public class Setting
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("key")]
        public string Key { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("value")]
        public string Value { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}