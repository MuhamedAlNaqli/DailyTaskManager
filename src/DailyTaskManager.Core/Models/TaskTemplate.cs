namespace DailyTaskManager.Core.Models
{
    /// <summary>
    /// A saved entry that can be added again with one click
    /// </summary>
    public class TaskTemplate
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Name { get; set; } = "";

        public string Description { get; set; } = "";

        public decimal Hours { get; set; } = 7;

        public int? ClientId { get; set; }

        public virtual Client? Client { get; set; }

        public override string ToString() => Name;
    }
}
