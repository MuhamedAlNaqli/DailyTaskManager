using System;

namespace DailyTaskManager.Core.Models
{
    /// <summary>
    /// A client that time entries can be assigned to, with the legal details printed on its documents
    /// </summary>
    public class Client
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        /// <summary>Short name shown in lists and filters</summary>
        public string Name { get; set; } = "";

        /// <summary>Company name as printed on documents; the short name is used when empty</summary>
        public string LegalName { get; set; } = "";

        public string CompanyId { get; set; } = "";

        public string VatNumber { get; set; } = "";

        public string AddressLine1 { get; set; } = "";

        public string AddressLine2 { get; set; } = "";

        public string Notes { get; set; } = "";

        /// <summary>ISO currency code</summary>
        public string Currency { get; set; } = "BAM";

        /// <summary>Folder for this client's documents; empty = the export folders from Settings</summary>
        public string ExportFolder { get; set; } = "";

        /// <summary>Tag colour as #RRGGBB shown next to the client's entries; empty = none</summary>
        public string Color { get; set; } = "";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string PrintedName => string.IsNullOrWhiteSpace(LegalName) ? Name : LegalName;

        public override string ToString() => Name;
    }
}
