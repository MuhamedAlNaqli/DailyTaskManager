
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DailyTaskManager.Core.Models
{
    [Table("daily_tasks", Schema = "public")]
    public class DailyTask
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("task_date")]
        public DateTime Date { get; set; } = DateTime.Today;

        [Column("description")]
        public string Description { get; set; } = "";

        [Column("total_hours")]
        public int Hours { get; set; } = 7;
    }
}
