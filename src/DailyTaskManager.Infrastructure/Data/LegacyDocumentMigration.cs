using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Infrastructure.Data
{
    /// <summary>
    /// Before multiple clients existed, the company, the client and the contract prefix of the documents were one
    /// global "PdfSettings" value. This moves them, once, to the business profile, a client and a contract.
    /// Nothing is deleted: the old value stays in the settings for one more release.
    /// </summary>
    internal static class LegacyDocumentMigration
    {
        private const string DoneKey = "Migration.ClientDocuments";

        /// <summary>
        /// The settings as the documents printed them: values missing from the stored value come from these defaults,
        /// so the migrated documents are identical to the ones produced before.
        /// </summary>
        private sealed class LegacyPdfSettings
        {
            public string CompanyName { get; set; } = "Obrtnička djelatnost \"Smart Point\" vl. Alnaqli Muhamed";
            public string CompanyId { get; set; } = "430406870008";
            public string AddressLine1 { get; set; } = "Nahorevska 136A";
            public string AddressLine2 { get; set; } = "71000 Sarajevo";
            public string Phone { get; set; } = "+38760 338 3069";
            public string Email { get; set; } = "muhamed.alnaqli@gmail.com";
            public string ClientName { get; set; } = "Logosoft d.o.o";
            public string ClientId { get; set; } = "4200236190009";
            public string ContractPrefix { get; set; } = "081-01";
            public string DecisionNumber { get; set; } = "16-17-320/24";
            public string ClientVatNumber { get; set; } = "200236190009";
            public string ClientAddressLine1 { get; set; } = "Grbavička 4";
            public string ClientAddressLine2 { get; set; } = "71000 Sarajevo";
        }

        public static async Task RunAsync(DailyTaskManagerDbContext db)
        {
            if (await db.Settings.AnyAsync(s => s.Key == DoneKey)) return;

            var json = await db.Settings.Where(s => s.Key == "PdfSettings").Select(s => s.Value).FirstOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(json))
            {
                await MarkDoneAsync(db); // a new installation: nothing to move
                return;
            }

            // The settings were global, so they belong to the user who has been using the app: the one with most entries
            var ownerId = await db.Users
                .OrderByDescending(u => db.DailyTasks.Count(t => t.UserId == u.Id))
                .ThenBy(u => u.Id)
                .Select(u => (int?)u.Id)
                .FirstOrDefaultAsync();
            if (ownerId == null) return; // try again once there is a user

            LegacyPdfSettings legacy;
            try
            {
                legacy = JsonConvert.DeserializeObject<LegacyPdfSettings>(json) ?? new LegacyPdfSettings();
            }
            catch (JsonException)
            {
                legacy = new LegacyPdfSettings();
            }

            var userId = ownerId.Value;

            if (!await db.BusinessProfiles.AnyAsync(b => b.UserId == userId))
            {
                db.BusinessProfiles.Add(new BusinessProfile
                {
                    UserId = userId,
                    CompanyName = legacy.CompanyName,
                    CompanyId = legacy.CompanyId,
                    AddressLine1 = legacy.AddressLine1,
                    AddressLine2 = legacy.AddressLine2,
                    Phone = legacy.Phone,
                    Email = legacy.Email,
                    DecisionNumber = legacy.DecisionNumber
                });
            }

            var clients = await db.Clients.Where(c => c.UserId == userId).ToListAsync();
            var name = (legacy.ClientName ?? "").Trim();
            var client = clients.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))
                         ?? (clients.Count == 1 ? clients[0] : null);

            if (client == null && name.Length > 0)
            {
                client = new Client { UserId = userId, Name = name.Length > 150 ? name.Substring(0, 150) : name };
                db.Clients.Add(client);
            }

            if (client != null && string.IsNullOrEmpty(client.LegalName))
            {
                client.LegalName = name;
                client.CompanyId = legacy.ClientId;
                client.VatNumber = legacy.ClientVatNumber;
                client.AddressLine1 = legacy.ClientAddressLine1;
                client.AddressLine2 = legacy.ClientAddressLine2;
            }

            await db.SaveChangesAsync();

            if (client != null && !string.IsNullOrWhiteSpace(legacy.ContractPrefix)
                && !await db.ClientContracts.AnyAsync(c => c.ClientId == client.Id))
            {
                var first = await db.DailyTasks.Where(t => t.UserId == userId).MinAsync(t => (DateTime?)t.Date);
                db.ClientContracts.Add(new ClientContract
                {
                    ClientId = client.Id,
                    ContractNumber = legacy.ContractPrefix.Trim(),
                    ValidFrom = first?.Date ?? new DateTime(DateTime.Today.Year, 1, 1)
                });
            }

            await MarkDoneAsync(db);
        }

        private static async Task MarkDoneAsync(DailyTaskManagerDbContext db)
        {
            db.Settings.Add(new Setting { Key = DoneKey, Value = "1", Description = "Documents moved to business profile and clients", IsSystem = true });
            await db.SaveChangesAsync();
        }
    }
}
