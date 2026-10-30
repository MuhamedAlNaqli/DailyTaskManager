using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Core.Interfaces
{
    /// <summary>What a document is generated for</summary>
    public class DocumentRequest
    {
        public int ClientId { get; set; }

        public DateTime PeriodFrom { get; set; }

        public DateTime PeriodTo { get; set; }

        public List<DailyTask> Tasks { get; set; } = new List<DailyTask>();

        public string InvoiceNumber { get; set; } = "";

        public DateTime IssueDate { get; set; } = DateTime.Today;

        public decimal? HourlyRate { get; set; }

        /// <summary>Overrides the currency of the client when set</summary>
        public string Currency { get; set; } = "";

        public decimal VatPercent { get; set; }

        /// <summary>Folder for the result</summary>
        public string Folder { get; set; } = "";
    }

    public class GeneratedDocument
    {
        public string Path { get; set; } = "";

        /// <summary>Placeholders in the template that have no value; they are left visible in the file</summary>
        public List<string> UnknownPlaceholders { get; set; } = new List<string>();
    }

    /// <summary>Fills the user's template files with the data of a client and month</summary>
    public interface IDocumentGenerator
    {
        Task<GeneratedDocument> GenerateAsync(DocumentTemplate template, DocumentRequest request);

        /// <summary>The placeholders a template can use, for display in the interface and for tests</summary>
        IReadOnlyList<string> KnownPlaceholders { get; }

        /// <summary>The placeholders found in a template file (known or not)</summary>
        Task<IReadOnlyList<string>> ScanPlaceholdersAsync(string templatePath);
    }
}
