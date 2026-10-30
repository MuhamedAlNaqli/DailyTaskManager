using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Models;
using DailyTaskManager.Infrastructure.Data;

namespace DailyTaskManager.Infrastructure.Services;

/// <summary>
/// The consultant's own company, clients and contracts, and the assembly of the data documents are made from
/// </summary>
public class BusinessService : IBusinessService
{
    private const long MaxImageBytes = 5 * 1024 * 1024;

    private readonly IDbContextFactory<DailyTaskManagerDbContext> _contextFactory;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<BusinessService> _logger;

    public BusinessService(IDbContextFactory<DailyTaskManagerDbContext> contextFactory, IAuthenticationService authService, ILogger<BusinessService> logger)
    {
        _contextFactory = contextFactory;
        _authService = authService;
        _logger = logger;
    }

    private int UserId
    {
        get
        {
            if (!_authService.IsAuthenticated || _authService.CurrentUser == null)
            {
                throw new InvalidOperationException("User must be authenticated");
            }

            return _authService.CurrentUser.Id;
        }
    }

    private static string ImagesFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DailyTaskManager", "images");

    // ---- Business profile -------------------------------------------------------------------------------------

    public async Task<BusinessProfile> GetProfileAsync()
    {
        var userId = UserId;
        await using var db = _contextFactory.CreateDbContext();
        return await db.BusinessProfiles.AsNoTracking().FirstOrDefaultAsync(b => b.UserId == userId)
               ?? new BusinessProfile { UserId = userId };
    }

    public async Task SaveProfileAsync(BusinessProfile profile)
    {
        var userId = UserId;
        await using var db = _contextFactory.CreateDbContext();
        var stored = await db.BusinessProfiles.FirstOrDefaultAsync(b => b.UserId == userId);
        if (stored == null)
        {
            stored = new BusinessProfile { UserId = userId };
            db.BusinessProfiles.Add(stored);
        }

        stored.CompanyName = Clip(profile.CompanyName, 200);
        stored.CompanyId = Clip(profile.CompanyId, 50);
        stored.VatNumber = Clip(profile.VatNumber, 50);
        stored.AddressLine1 = Clip(profile.AddressLine1, 200);
        stored.AddressLine2 = Clip(profile.AddressLine2, 200);
        stored.Phone = Clip(profile.Phone, 50);
        stored.Email = Clip(profile.Email, 255);
        stored.DecisionNumber = Clip(profile.DecisionNumber, 100);
        // The image paths are only changed through the image methods
        await db.SaveChangesAsync();
    }

    public Task<string> SetSignatureImageAsync(string sourcePath) => SetImageAsync(sourcePath, "signature", (p, v) => p.SignatureImagePath = v);

    public Task<string> SetStampImageAsync(string sourcePath) => SetImageAsync(sourcePath, "stamp", (p, v) => p.StampImagePath = v);

    public Task RemoveSignatureImageAsync() => RemoveImageAsync(p => p.SignatureImagePath, (p, v) => p.SignatureImagePath = v);

    public Task RemoveStampImageAsync() => RemoveImageAsync(p => p.StampImagePath, (p, v) => p.StampImagePath = v);

    private async Task<string> SetImageAsync(string sourcePath, string kind, Action<BusinessProfile, string> assign)
    {
        var userId = UserId;
        var info = new FileInfo(sourcePath);
        if (!info.Exists) throw new FileNotFoundException("The image file was not found", sourcePath);
        if (info.Length > MaxImageBytes) throw new InvalidOperationException("The image is larger than 5 MB.");

        var bytes = await File.ReadAllBytesAsync(sourcePath);
        var extension = ImageInfo.Detect(bytes) switch
        {
            ImageKind.Png => ".png",
            ImageKind.Jpeg => ".jpg",
            _ => throw new InvalidOperationException("Only PNG and JPEG images can be used.")
        };

        Directory.CreateDirectory(ImagesFolder);
        // A new name each time, so a document being generated never reads a half-replaced file
        var target = Path.Combine(ImagesFolder, $"{kind}-{userId}-{DateTime.UtcNow:yyyyMMddHHmmssfff}{extension}");
        await File.WriteAllBytesAsync(target, bytes);

        await using var db = _contextFactory.CreateDbContext();
        var profile = await db.BusinessProfiles.FirstOrDefaultAsync(b => b.UserId == userId);
        if (profile == null)
        {
            profile = new BusinessProfile { UserId = userId };
            db.BusinessProfiles.Add(profile);
        }

        var previous = kind == "signature" ? profile.SignatureImagePath : profile.StampImagePath;
        assign(profile, target);
        await db.SaveChangesAsync();
        DeleteQuietly(previous);
        return target;
    }

    private async Task RemoveImageAsync(Func<BusinessProfile, string> read, Action<BusinessProfile, string> assign)
    {
        var userId = UserId;
        await using var db = _contextFactory.CreateDbContext();
        var profile = await db.BusinessProfiles.FirstOrDefaultAsync(b => b.UserId == userId);
        if (profile == null) return;

        var previous = read(profile);
        assign(profile, "");
        await db.SaveChangesAsync();
        DeleteQuietly(previous);
    }

    private void DeleteQuietly(string path)
    {
        try
        {
            // Only files this service created are removed
            if (!string.IsNullOrEmpty(path) && path.StartsWith(ImagesFolder, StringComparison.OrdinalIgnoreCase) && File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete {Path}", path);
        }
    }

    // ---- Clients ----------------------------------------------------------------------------------------------

    public async Task<List<Client>> GetClientsAsync(bool includeInactive = true)
    {
        var userId = UserId;
        await using var db = _contextFactory.CreateDbContext();
        return await db.Clients.AsNoTracking()
            .Where(c => c.UserId == userId && (includeInactive || c.IsActive))
            .OrderByDescending(c => c.IsActive).ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Client> SaveClientAsync(Client client)
    {
        var userId = UserId;
        var name = (client.Name ?? "").Trim();
        if (name.Length == 0) throw new ArgumentException("A client needs a name.");

        await using var db = _contextFactory.CreateDbContext();
        var lower = name.ToLower();
        if (await db.Clients.AnyAsync(c => c.UserId == userId && c.Id != client.Id && c.Name.ToLower() == lower))
        {
            throw new InvalidOperationException($"A client named \"{name}\" already exists.");
        }

        Client stored;
        if (client.Id == 0)
        {
            stored = new Client { UserId = userId, CreatedAt = DateTime.UtcNow };
            db.Clients.Add(stored);
        }
        else
        {
            stored = await db.Clients.FirstOrDefaultAsync(c => c.Id == client.Id && c.UserId == userId)
                     ?? throw new InvalidOperationException("Client not found.");
        }

        stored.Name = Clip(name, 150);
        stored.LegalName = Clip(client.LegalName, 200);
        stored.CompanyId = Clip(client.CompanyId, 50);
        stored.VatNumber = Clip(client.VatNumber, 50);
        stored.AddressLine1 = Clip(client.AddressLine1, 200);
        stored.AddressLine2 = Clip(client.AddressLine2, 200);
        stored.Notes = Clip(client.Notes, 1000);
        stored.Currency = NormalizeCurrency(client.Currency);
        stored.ExportFolder = Clip(client.ExportFolder, 500);
        stored.Color = NormalizeColor(client.Color);
        stored.IsActive = client.IsActive;
        await db.SaveChangesAsync();
        return stored;
    }

    public async Task<bool> DeleteOrDeactivateClientAsync(int clientId)
    {
        var userId = UserId;
        await using var db = _contextFactory.CreateDbContext();
        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == clientId && c.UserId == userId);
        if (client == null) return false;

        if (await db.DailyTasks.AnyAsync(t => t.ClientId == clientId))
        {
            client.IsActive = false; // the entries keep their history
            await db.SaveChangesAsync();
            return false;
        }

        db.Clients.Remove(client);
        await db.SaveChangesAsync();
        return true;
    }

    // ---- Contracts --------------------------------------------------------------------------------------------

    public async Task<List<ClientContract>> GetContractsAsync(int clientId)
    {
        var userId = UserId;
        await using var db = _contextFactory.CreateDbContext();
        if (!await db.Clients.AnyAsync(c => c.Id == clientId && c.UserId == userId)) return new List<ClientContract>();

        return await db.ClientContracts.AsNoTracking()
            .Where(c => c.ClientId == clientId)
            .OrderByDescending(c => c.ValidFrom)
            .ToListAsync();
    }

    public async Task<ClientContract> SaveContractAsync(ClientContract contract)
    {
        var userId = UserId;
        var number = (contract.ContractNumber ?? "").Trim();
        if (number.Length == 0) throw new ArgumentException("A contract needs a number.");
        if (contract.ValidTo.HasValue && contract.ValidTo.Value.Date < contract.ValidFrom.Date)
            throw new ArgumentException("The contract cannot end before it starts.");
        if (contract.HourlyRate is < 0) throw new ArgumentException("The hourly rate cannot be negative.");

        await using var db = _contextFactory.CreateDbContext();
        if (!await db.Clients.AnyAsync(c => c.Id == contract.ClientId && c.UserId == userId))
            throw new InvalidOperationException("Client not found.");

        var from = contract.ValidFrom.Date;
        var to = contract.ValidTo?.Date;
        var others = await db.ClientContracts.AsNoTracking()
            .Where(c => c.ClientId == contract.ClientId && c.Id != contract.Id).ToListAsync();
        var overlap = others.FirstOrDefault(o => o.ValidFrom.Date <= (to ?? DateTime.MaxValue.Date) && from <= (o.ValidTo?.Date ?? DateTime.MaxValue.Date));
        if (overlap != null)
        {
            throw new InvalidOperationException($"The period overlaps contract {overlap.ContractNumber}. End that contract first.");
        }

        ClientContract stored;
        if (contract.Id == 0)
        {
            stored = new ClientContract { ClientId = contract.ClientId };
            db.ClientContracts.Add(stored);
        }
        else
        {
            stored = await db.ClientContracts.FirstOrDefaultAsync(c => c.Id == contract.Id && c.ClientId == contract.ClientId)
                     ?? throw new InvalidOperationException("Contract not found.");
        }

        stored.ContractNumber = Clip(number, 50);
        stored.HourlyRate = contract.HourlyRate;
        stored.ValidFrom = from;
        stored.ValidTo = to;
        await db.SaveChangesAsync();
        return stored;
    }

    public async Task DeleteContractAsync(int contractId)
    {
        var userId = UserId;
        await using var db = _contextFactory.CreateDbContext();
        var contract = await db.ClientContracts.FirstOrDefaultAsync(c => c.Id == contractId
            && db.Clients.Any(cl => cl.Id == c.ClientId && cl.UserId == userId));
        if (contract == null) return;

        db.ClientContracts.Remove(contract);
        await db.SaveChangesAsync();
    }

    public async Task<ClientContract?> GetContractOnAsync(int clientId, DateTime date)
    {
        var contracts = await GetContractsAsync(clientId);
        return contracts.FirstOrDefault(c => c.Covers(date));
    }

    // ---- Template files ---------------------------------------------------------------------------------------

    private static string TemplatesFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DailyTaskManager", "templates");

    private async Task EnsureOwnClientAsync(DailyTaskManagerDbContext db, int clientId)
    {
        var userId = UserId;
        if (!await db.Clients.AnyAsync(c => c.Id == clientId && c.UserId == userId))
            throw new InvalidOperationException("Client not found.");
    }

    public async Task<List<DocumentTemplate>> GetTemplatesAsync(int clientId)
    {
        await using var db = _contextFactory.CreateDbContext();
        await EnsureOwnClientAsync(db, clientId);
        return await db.DocumentTemplates.AsNoTracking()
            .Where(t => t.ClientId == clientId).OrderBy(t => t.SortOrder).ThenBy(t => t.Name).ToListAsync();
    }

    public async Task<DocumentTemplate> AddTemplateAsync(int clientId, string sourcePath, string? name = null)
    {
        var info = new FileInfo(sourcePath);
        if (!info.Exists) throw new FileNotFoundException("The template file was not found", sourcePath);

        var extension = info.Extension.ToLowerInvariant();
        if (extension != ".docx" && extension != ".xlsx") throw new InvalidOperationException("A template must be a Word (.docx) or Excel (.xlsx) file.");
        if (info.Length > 10 * 1024 * 1024) throw new InvalidOperationException("The template is larger than 10 MB.");

        await using var db = _contextFactory.CreateDbContext();
        await EnsureOwnClientAsync(db, clientId);

        Directory.CreateDirectory(TemplatesFolder);
        var target = Path.Combine(TemplatesFolder, $"client{clientId}-{DateTime.UtcNow:yyyyMMddHHmmssfff}{extension}");
        File.Copy(sourcePath, target, overwrite: false);

        var template = new DocumentTemplate
        {
            ClientId = clientId,
            Name = Clip(string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(sourcePath) : name, 150),
            TemplatePath = target,
            SortOrder = (await db.DocumentTemplates.Where(t => t.ClientId == clientId).MaxAsync(t => (int?)t.SortOrder) ?? 0) + 1
        };
        db.DocumentTemplates.Add(template);
        await db.SaveChangesAsync();
        return template;
    }

    public async Task<DocumentTemplate> UpdateTemplateAsync(DocumentTemplate template)
    {
        var name = (template.Name ?? "").Trim();
        if (name.Length == 0) throw new ArgumentException("A template needs a name.");

        var userId = UserId;
        await using var db = _contextFactory.CreateDbContext();
        var stored = await db.DocumentTemplates.FirstOrDefaultAsync(t => t.Id == template.Id
                         && db.Clients.Any(c => c.Id == t.ClientId && c.UserId == userId))
                     ?? throw new InvalidOperationException("Template not found.");

        stored.Name = Clip(name, 150);
        stored.FileNamePattern = string.IsNullOrWhiteSpace(template.FileNamePattern)
            ? DocumentTemplate.DefaultFileNamePattern : Clip(template.FileNamePattern, 200);
        stored.IsActive = template.IsActive;
        await db.SaveChangesAsync();
        return stored;
    }

    public async Task DeleteTemplateAsync(int templateId)
    {
        var userId = UserId;
        await using var db = _contextFactory.CreateDbContext();
        var stored = await db.DocumentTemplates.FirstOrDefaultAsync(t => t.Id == templateId
            && db.Clients.Any(c => c.Id == t.ClientId && c.UserId == userId));
        if (stored == null) return;

        db.DocumentTemplates.Remove(stored);
        await db.SaveChangesAsync();

        try
        {
            if (stored.TemplatePath.StartsWith(TemplatesFolder, StringComparison.OrdinalIgnoreCase) && File.Exists(stored.TemplatePath))
                File.Delete(stored.TemplatePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete {Path}", stored.TemplatePath);
        }
    }

    // ---- Document data ----------------------------------------------------------------------------------------

    public async Task<PdfSettings> GetDocumentSettingsAsync(int? clientId, DateTime periodEnd, bool includeSignatures = true)
    {
        var profile = await GetProfileAsync();
        var clients = await GetClientsAsync();

        // "All clients" with a single client is unambiguous
        Client? client = clientId is > 0
            ? clients.FirstOrDefault(c => c.Id == clientId)
            : clients.Count(c => c.IsActive) == 1 ? clients.First(c => c.IsActive) : null;
        var contract = client == null ? null : await GetContractOnAsync(client.Id, periodEnd);

        var settings = new PdfSettings
        {
            CompanyName = profile.CompanyName,
            CompanyId = profile.CompanyId,
            AddressLine1 = profile.AddressLine1,
            AddressLine2 = profile.AddressLine2,
            Phone = profile.Phone,
            Email = profile.Email,
            DecisionNumber = profile.DecisionNumber,
            ClientName = client?.PrintedName ?? "",
            ClientId = client?.CompanyId ?? "",
            ClientVatNumber = client?.VatNumber ?? "",
            ClientAddressLine1 = client?.AddressLine1 ?? "",
            ClientAddressLine2 = client?.AddressLine2 ?? "",
            ContractPrefix = contract?.ContractNumber ?? "",
            ContractYear = periodEnd.Year,
            IncludeSignatures = includeSignatures
        };

        if (includeSignatures)
        {
            if (File.Exists(profile.SignatureImagePath)) settings.SignatureImagePath = profile.SignatureImagePath;
            if (File.Exists(profile.StampImagePath)) settings.StampImagePath = profile.StampImagePath;
        }

        return settings;
    }

    // ---- Helpers ----------------------------------------------------------------------------------------------

    private static string Clip(string? value, int max)
    {
        var text = (value ?? "").Trim();
        return text.Length > max ? text.Substring(0, max) : text;
    }

    private static string NormalizeColor(string? color)
    {
        var text = (color ?? "").Trim();
        return System.Text.RegularExpressions.Regex.IsMatch(text, "^#[0-9A-Fa-f]{6}$") ? text.ToUpperInvariant() : "";
    }

    private static string NormalizeCurrency(string? code)
    {
        var text = (code ?? "").Trim().ToUpperInvariant();
        return text.Length == 3 && text.All(char.IsLetter) ? text : "BAM";
    }
}
