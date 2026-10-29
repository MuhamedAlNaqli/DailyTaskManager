using System;

namespace DailyTaskManager.Core.Models
{
    /// <summary>
    /// A contract with a client. A client has one contract at a time; a renewal is a new row with a new number.
    /// The contract valid in a month supplies the number printed on that month's documents.
    /// </summary>
    public class ClientContract
    {
        public int Id { get; set; }

        public int ClientId { get; set; }

        /// <summary>The number without the year, for example "078-01"; documents print it as "078-01/26"</summary>
        public string ContractNumber { get; set; } = "";

        /// <summary>Optional; hours-only clients have none</summary>
        public decimal? HourlyRate { get; set; }

        public DateTime ValidFrom { get; set; } = DateTime.Today;

        /// <summary>Null = open ended</summary>
        public DateTime? ValidTo { get; set; }

        public bool Covers(DateTime date) =>
            date.Date >= ValidFrom.Date && (!ValidTo.HasValue || date.Date <= ValidTo.Value.Date);

        public override string ToString() => ContractNumber;
    }
}
