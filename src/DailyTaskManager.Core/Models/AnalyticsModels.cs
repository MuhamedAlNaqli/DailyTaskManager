using System;

namespace DailyTaskManager.Core.Models
{
    /// <summary>
    /// Hours logged on one day
    /// </summary>
    public class DayHours
    {
        public DateTime Date { get; set; }

        public decimal Hours { get; set; }

        public int Entries { get; set; }
    }

    /// <summary>
    /// Hours logged for one client (ClientId null = entries without a client)
    /// </summary>
    public class ClientHours
    {
        public int? ClientId { get; set; }

        public string ClientName { get; set; } = "";

        public decimal Hours { get; set; }

        public int Entries { get; set; }
    }

    /// <summary>
    /// Everything needed to print an invoice for a period of work
    /// </summary>
    public class InvoiceData
    {
        public string InvoiceNumber { get; set; } = "";

        public DateTime IssueDate { get; set; } = DateTime.Today;

        /// <summary>First day of the invoiced period</summary>
        public DateTime PeriodFrom { get; set; }

        /// <summary>Last day of the invoiced period</summary>
        public DateTime PeriodTo { get; set; }

        public decimal Hours { get; set; }

        public decimal HourlyRate { get; set; }

        public string Currency { get; set; } = "BAM";

        /// <summary>VAT percentage added on top of the net amount (0 = no VAT)</summary>
        public decimal VatPercent { get; set; }

        /// <summary>Free text printed under the totals (bank account, payment deadline...)</summary>
        public string PaymentNote { get; set; } = "";

        public decimal NetAmount => Math.Round(Hours * HourlyRate, 2, MidpointRounding.AwayFromZero);

        public decimal VatAmount => Math.Round(NetAmount * VatPercent / 100m, 2, MidpointRounding.AwayFromZero);

        public decimal GrossAmount => NetAmount + VatAmount;
    }
}
