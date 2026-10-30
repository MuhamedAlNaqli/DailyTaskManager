using System;

namespace DailyTaskManager.Core.Models
{
    /// <summary>
    /// A Word or Excel file of the user (or of the client) with {{placeholders}} that is filled in for a month.
    /// The file is kept in the application's data folder.
    /// </summary>
    public class DocumentTemplate
    {
        public int Id { get; set; }

        public int ClientId { get; set; }

        public string Name { get; set; } = "";

        /// <summary>The copy of the user's file, in the application's data folder</summary>
        public string TemplatePath { get; set; } = "";

        /// <summary>Name of the generated file without extension; may use {Name} {Client} {YYYY} {MM} {Month} {InvoiceNumber}</summary>
        public string FileNamePattern { get; set; } = DefaultFileNamePattern;

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public const string DefaultFileNamePattern = "{Name} - {Client} - {YYYY}-{MM}";

        /// <summary>"docx" or "xlsx"</summary>
        public string Format
        {
            get
            {
                var extension = System.IO.Path.GetExtension(TemplatePath ?? "").TrimStart('.').ToLowerInvariant();
                return extension == "xlsx" ? "xlsx" : "docx";
            }
        }

        public override string ToString() => Name;
    }
}
