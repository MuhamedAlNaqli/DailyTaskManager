using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DailyTaskManager.Core.Models
{
    /// <summary>
    /// The contents of a backup file (JSON)
    /// </summary>
    public class BackupFile
    {
        public int Version { get; set; } = 1;

        public DateTime ExportedAt { get; set; } = DateTime.UtcNow;

        public string AppVersion { get; set; } = "";

        public List<BackupTask> Tasks { get; set; } = new List<BackupTask>();

        public List<BackupTemplate> Templates { get; set; } = new List<BackupTemplate>();
    }

    public class BackupTask
    {
        public DateTime Date { get; set; }

        public string Description { get; set; } = "";

        public decimal Hours { get; set; }

        public string? Client { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class BackupTemplate
    {
        public string Name { get; set; } = "";

        public string Description { get; set; } = "";

        public decimal Hours { get; set; }

        public string? Client { get; set; }
    }

    /// <summary>
    /// One entry read from a file that is about to be imported
    /// </summary>
    public class ImportEntry
    {
        public DateTime Date { get; set; }

        public string Description { get; set; } = "";

        public decimal Hours { get; set; }

        public string? Client { get; set; }

        public DateTime? CreatedAt { get; set; }
    }

    /// <summary>
    /// What a file would add, shown to the user before anything is written
    /// </summary>
    public class ImportPreview
    {
        public string SourceName { get; set; } = "";

        public List<ImportEntry> Entries { get; set; } = new List<ImportEntry>();

        public List<BackupTemplate> Templates { get; set; } = new List<BackupTemplate>();

        /// <summary>Rows that could not be read (no valid date, description or hours)</summary>
        public int SkippedRows { get; set; }

        /// <summary>Entries that already exist (same date, description and hours)</summary>
        public int DuplicateCount { get; set; }

        public int NewCount => Entries.Count - DuplicateCount;

        public DateTime? From => Entries.Count == 0 ? (DateTime?)null : Entries.Min(e => e.Date);

        public DateTime? To => Entries.Count == 0 ? (DateTime?)null : Entries.Max(e => e.Date);
    }

    public class ImportResult
    {
        public int Added { get; set; }

        public int Duplicates { get; set; }

        public int ClientsCreated { get; set; }

        public int TemplatesAdded { get; set; }
    }

    /// <summary>
    /// Builds the key that decides whether two entries are the same
    /// </summary>
    public static class TaskKeys
    {
        public static string For(DateTime date, string? description, decimal hours)
        {
            var text = (description ?? string.Empty).Replace("\r\n", "\n").Trim().ToLowerInvariant();
            return date.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "|" + text + "|" + hours.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
