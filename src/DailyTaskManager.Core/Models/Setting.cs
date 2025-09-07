using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DailyTaskManager.Core.Models
{
    /// <summary>
    /// Represents application configuration settings
    /// </summary>
    [Table("settings", Schema = "public")]
    public class Setting
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("key")]
        public string Key { get; set; } = "";

        [Required]
        [MaxLength(1000)] // Increased to support larger values
        [Column("value")]
        public string Value { get; set; } = "";

        [MaxLength(500)]
        [Column("description")]
        public string? Description { get; set; }

        [Column("is_system")]
        public bool IsSystem { get; set; } = false;

        // Helper method for type-safe value access
        public T GetValue<T>()
        {
            if (typeof(T) == typeof(string))
                return (T)(object)Value;
            
            if (typeof(T) == typeof(bool))
                return (T)(object)bool.Parse(Value);
                
            if (typeof(T) == typeof(int))
                return (T)(object)int.Parse(Value);
                
            if (typeof(T) == typeof(decimal))
                return (T)(object)decimal.Parse(Value);

            throw new InvalidOperationException($"Unsupported type: {typeof(T)}");
        }

        public void SetValue<T>(T value)
        {
            Value = value?.ToString() ?? "";
        }

        public override string ToString() => $"{Key}: {Value}";
    }
}