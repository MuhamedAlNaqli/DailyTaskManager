using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Core.Interfaces
{
    /// <summary>
    /// The consultant's business: own company details, clients with their contracts, and the data that
    /// documents are generated from.
    /// </summary>
    public interface IBusinessService
    {
        /// <summary>The current user's company; an empty profile when none was saved yet</summary>
        Task<BusinessProfile> GetProfileAsync();

        Task SaveProfileAsync(BusinessProfile profile);

        /// <summary>Copies an image into the application's data folder and stores it as the signature</summary>
        Task<string> SetSignatureImageAsync(string sourcePath);

        Task<string> SetStampImageAsync(string sourcePath);

        Task RemoveSignatureImageAsync();

        Task RemoveStampImageAsync();

        /// <summary>All clients of the current user, active first</summary>
        Task<List<Client>> GetClientsAsync(bool includeInactive = true);

        /// <summary>Creates (Id = 0) or updates a client; names are unique per user</summary>
        Task<Client> SaveClientAsync(Client client);

        /// <summary>Deletes a client without entries; one with entries is deactivated instead. Returns true when deleted.</summary>
        Task<bool> DeleteOrDeactivateClientAsync(int clientId);

        Task<List<ClientContract>> GetContractsAsync(int clientId);

        /// <summary>Creates (Id = 0) or updates a contract; contracts of one client may not overlap</summary>
        Task<ClientContract> SaveContractAsync(ClientContract contract);

        Task DeleteContractAsync(int contractId);

        /// <summary>The contract of the client that is valid on the date, or null</summary>
        Task<ClientContract?> GetContractOnAsync(int clientId, DateTime date);

        /// <summary>The template files of a client, in their display order</summary>
        Task<List<DocumentTemplate>> GetTemplatesAsync(int clientId);

        /// <summary>Copies the file into the application's data folder and adds it as a template of the client</summary>
        Task<DocumentTemplate> AddTemplateAsync(int clientId, string sourcePath, string? name = null);

        /// <summary>Changes name, file name pattern and active flag (the file itself stays)</summary>
        Task<DocumentTemplate> UpdateTemplateAsync(DocumentTemplate template);

        /// <summary>Removes the template and its stored file</summary>
        Task DeleteTemplateAsync(int templateId);

        /// <summary>
        /// Everything a document needs: the company, the client and the contract valid on the period's last day.
        /// Without a client the fields of the client are left empty.
        /// </summary>
        Task<PdfSettings> GetDocumentSettingsAsync(int? clientId, DateTime periodEnd, bool includeSignatures = true);
    }
}
