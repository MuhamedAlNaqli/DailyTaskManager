using System;

namespace DailyTaskManager.Core.Models
{
    /// <summary>
    /// The consultant's own company: the "issuer" half of every document, plus the signature and stamp images
    /// that are placed on the work order.
    /// </summary>
    public class BusinessProfile
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string CompanyName { get; set; } = "";

        public string CompanyId { get; set; } = "";

        public string VatNumber { get; set; } = "";

        public string AddressLine1 { get; set; } = "";

        public string AddressLine2 { get; set; } = "";

        public string Phone { get; set; } = "";

        public string Email { get; set; } = "";

        /// <summary>"Broj rješenja", printed under the company in the work order</summary>
        public string DecisionNumber { get; set; } = "";

        /// <summary>Image files (PNG or JPEG) kept in the application's data folder</summary>
        public string SignatureImagePath { get; set; } = "";

        public string StampImagePath { get; set; } = "";
    }
}
