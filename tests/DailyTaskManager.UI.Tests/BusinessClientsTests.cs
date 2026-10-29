using System.Reflection;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Models;
using DailyTaskManager.Infrastructure.Data;
using DailyTaskManager.Infrastructure.Services;

namespace DailyTaskManager.UI.Tests;

/// <summary>
/// The business profile, clients and contracts, and the documents made from them (real services, real database).
/// Everything a test creates is removed again and the profile is restored.
/// </summary>
[TestClass]
[DoNotParallelize]
public class BusinessClientsTests
{
    // A valid 1x1 PNG
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    private IHost? _host;
    private IBusinessService _business = null!;
    private IDatabaseService _database = null!;
    private BusinessProfile _originalProfile = null!;
    private readonly List<int> _clients = new();
    private readonly List<int> _tasks = new();
    private readonly List<string> _files = new();

    [TestInitialize]
    public async Task Setup()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                config.Sources.Clear();
                var appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Environment.CurrentDirectory;
                var avaloniaDir = Path.GetFullPath(Path.Combine(appDir, "..", "..", "..", "..", "..", "src", "DailyTaskManager.Avalonia"));
                config.SetBasePath(avaloniaDir)
                    .AddJsonFile("appsettings.json", optional: false)
                    .AddJsonFile("appsettings.local.json", optional: true);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddDbContextFactory<DailyTaskManagerDbContext>(options =>
                    options.UseNpgsql(context.Configuration.GetConnectionString("DefaultConnection")));
                services.AddSingleton<IDatabaseService, DatabaseService>();
                services.AddSingleton<ISessionStore, SessionStore>();
                services.AddSingleton<IAuthenticationService, AuthenticationService>();
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IExportService, ExportService>();
                services.AddSingleton<IBusinessService, BusinessService>();
            })
            .Build();

        var factory = _host.Services.GetRequiredService<IDbContextFactory<DailyTaskManagerDbContext>>();
        await using (var db = factory.CreateDbContext()) await db.MigrateDatabaseAsync();

        var auth = _host.Services.GetRequiredService<IAuthenticationService>();
        var login = await auth.LoginAsync(new LoginRequest { Username = "admin", Password = "admin123" });
        if (!login.IsSuccess)
        {
            await auth.RegisterAsync("admin", "admin@example.com", "admin123");
            login = await auth.LoginAsync(new LoginRequest { Username = "admin", Password = "admin123" });
        }

        Assert.IsTrue(login.IsSuccess, "A test user is needed");

        _business = _host.Services.GetRequiredService<IBusinessService>();
        _database = _host.Services.GetRequiredService<IDatabaseService>();
        _originalProfile = await _business.GetProfileAsync();
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        foreach (var id in _tasks) await _database.RemoveTaskAsync(id);
        foreach (var id in _clients) await _business.DeleteOrDeactivateClientAsync(id);
        await _business.RemoveSignatureImageAsync();
        await _business.RemoveStampImageAsync();
        await _business.SaveProfileAsync(_originalProfile);
        foreach (var file in _files) if (File.Exists(file)) File.Delete(file);
        _host?.Dispose();
    }

    private string TempFile(string extension, byte[]? content = null)
    {
        var path = Path.Combine(Path.GetTempPath(), $"dtm_{Guid.NewGuid():N}{extension}");
        if (content != null) File.WriteAllBytes(path, content);
        _files.Add(path);
        return path;
    }

    private async Task<Client> NewClientAsync(string? name = null)
    {
        var client = await _business.SaveClientAsync(new Client
        {
            Name = name ?? "Test client " + Guid.NewGuid().ToString("N")[..8],
            LegalName = "Test Company d.o.o.",
            CompanyId = "111",
            VatNumber = "222",
            AddressLine1 = "Street 1",
            AddressLine2 = "71000 Sarajevo"
        });
        _clients.Add(client.Id);
        return client;
    }

    [TestMethod]
    [DataRow("30,5", 30.5)]
    [DataRow("30.5", 30.5)]
    [DataRow("25", 25.0)]
    [DataRow("1.250,75", 1250.75)]
    [DataRow("1,250.75", 1250.75)]
    [DataRow("1,250", 1250.0)]
    [DataRow(" 18,00 ", 18.0)]
    public void Rate_ShouldBeReadWithEitherDecimalSeparator(string text, double expected)
    {
        Assert.IsTrue(DailyTaskManager.Avalonia.ViewModels.ClientsViewModel.TryParseRate(text, out var rate));
        Assert.AreEqual((decimal)expected, rate);
    }

    [TestMethod]
    public void Rate_ShouldBeOptionalAndRejectNonsense()
    {
        Assert.IsTrue(DailyTaskManager.Avalonia.ViewModels.ClientsViewModel.TryParseRate("  ", out var none));
        Assert.IsNull(none);
        Assert.IsFalse(DailyTaskManager.Avalonia.ViewModels.ClientsViewModel.TryParseRate("abc", out _));
        Assert.IsFalse(DailyTaskManager.Avalonia.ViewModels.ClientsViewModel.TryParseRate("-5", out _));
    }

    [TestMethod]
    public async Task Profile_ShouldBeSavedAndReadBack()
    {
        await _business.SaveProfileAsync(new BusinessProfile
        {
            CompanyName = "My Company", CompanyId = "123", VatNumber = "456", AddressLine1 = "A1", AddressLine2 = "A2",
            Phone = "+387", Email = "me@example.com", DecisionNumber = "99-1/26"
        });

        var profile = await _business.GetProfileAsync();
        Assert.AreEqual("My Company", profile.CompanyName);
        Assert.AreEqual("99-1/26", profile.DecisionNumber);
        Assert.AreEqual("456", profile.VatNumber);
    }

    [TestMethod]
    public async Task Images_ShouldBeStoredUsedAndRemoved()
    {
        var signature = TempFile(".png", Png);
        var stamp = TempFile(".png", Png);

        var stored = await _business.SetSignatureImageAsync(signature);
        await _business.SetStampImageAsync(stamp);
        Assert.IsTrue(File.Exists(stored), "The image is copied into the application's folder");
        Assert.AreNotEqual(signature, stored);

        var settings = await _business.GetDocumentSettingsAsync(null, DateTime.Today);
        Assert.AreEqual(stored, settings.SignatureImagePath);
        Assert.IsTrue(File.Exists(settings.StampImagePath));

        var unsigned = await _business.GetDocumentSettingsAsync(null, DateTime.Today, includeSignatures: false);
        Assert.AreEqual("", unsigned.SignatureImagePath, "A document can be made without the images");

        await _business.RemoveSignatureImageAsync();
        Assert.IsFalse(File.Exists(stored), "The stored copy is deleted with the image");
        Assert.AreEqual("", (await _business.GetProfileAsync()).SignatureImagePath);
    }

    [TestMethod]
    public async Task Images_ShouldRejectFilesThatAreNotPngOrJpeg()
    {
        var notAnImage = TempFile(".png", System.Text.Encoding.UTF8.GetBytes("this is text, not a picture"));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => _business.SetSignatureImageAsync(notAnImage));
    }

    [TestMethod]
    public async Task Clients_ShouldHaveUniqueNamesAndKeepHistory()
    {
        var client = await NewClientAsync();
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => _business.SaveClientAsync(new Client { Name = client.Name.ToUpperInvariant() }));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => _business.SaveClientAsync(new Client { Name = "  " }));

        var saved = await _business.SaveClientAsync(new Client { Id = client.Id, Name = client.Name, Currency = "eur", IsActive = true });
        Assert.AreEqual("EUR", saved.Currency, "Currency codes are normalised");
        Assert.AreEqual("", saved.LegalName, "A save replaces the details");

        var task = await _database.AddTaskAsync(new DailyTask { Description = "Test entry", Date = DateTime.Today, Hours = 1, ClientId = client.Id });
        _tasks.Add(task.Id);

        Assert.IsFalse(await _business.DeleteOrDeactivateClientAsync(client.Id), "A client with entries is not deleted");
        var kept = (await _business.GetClientsAsync()).Single(c => c.Id == client.Id);
        Assert.IsFalse(kept.IsActive);
        CollectionAssert.DoesNotContain((await _business.GetClientsAsync(includeInactive: false)).Select(c => c.Id).ToList(), client.Id);

        var empty = await NewClientAsync();
        Assert.IsTrue(await _business.DeleteOrDeactivateClientAsync(empty.Id), "A client without entries is deleted");
        _clients.Remove(empty.Id);
    }

    [TestMethod]
    public async Task Contracts_ShouldBePickedByDateAndMayNotOverlap()
    {
        var client = await NewClientAsync();
        var first = await _business.SaveContractAsync(new ClientContract
            { ClientId = client.Id, ContractNumber = "078-01", ValidFrom = new DateTime(2026, 1, 1), ValidTo = new DateTime(2026, 6, 30), HourlyRate = 25m });
        var second = await _business.SaveContractAsync(new ClientContract
            { ClientId = client.Id, ContractNumber = "078-02", ValidFrom = new DateTime(2026, 7, 1) });

        Assert.AreEqual("078-01", (await _business.GetContractOnAsync(client.Id, new DateTime(2026, 3, 31)))?.ContractNumber);
        Assert.AreEqual("078-02", (await _business.GetContractOnAsync(client.Id, new DateTime(2027, 1, 15)))?.ContractNumber, "An open contract covers later dates");
        Assert.IsNull(await _business.GetContractOnAsync(client.Id, new DateTime(2025, 12, 31)));

        var overlap = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => _business.SaveContractAsync(new ClientContract
            { ClientId = client.Id, ContractNumber = "X", ValidFrom = new DateTime(2026, 6, 1), ValidTo = new DateTime(2026, 8, 1) }));
        StringAssert.Contains(overlap.Message, "078-01");

        await Assert.ThrowsExceptionAsync<ArgumentException>(() => _business.SaveContractAsync(new ClientContract
            { ClientId = client.Id, ContractNumber = "", ValidFrom = new DateTime(2030, 1, 1) }));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => _business.SaveContractAsync(new ClientContract
            { ClientId = client.Id, ContractNumber = "Y", ValidFrom = new DateTime(2030, 2, 1), ValidTo = new DateTime(2030, 1, 1) }));

        // Editing a contract does not collide with itself
        first.ValidTo = new DateTime(2026, 5, 31);
        await _business.SaveContractAsync(first);
        Assert.IsNull(await _business.GetContractOnAsync(client.Id, new DateTime(2026, 6, 15)), "The gap is not covered");

        await _business.DeleteContractAsync(second.Id);
        Assert.AreEqual(1, (await _business.GetContractsAsync(client.Id)).Count);
    }

    [TestMethod]
    public async Task DocumentSettings_ShouldCombineProfileClientAndTheContractOfThePeriod()
    {
        await _business.SaveProfileAsync(new BusinessProfile { CompanyName = "Issuer", CompanyId = "1", DecisionNumber = "D-1" });
        var client = await NewClientAsync();
        await _business.SaveContractAsync(new ClientContract { ClientId = client.Id, ContractNumber = "C-1", ValidFrom = new DateTime(2026, 1, 1), ValidTo = new DateTime(2026, 6, 30) });
        await _business.SaveContractAsync(new ClientContract { ClientId = client.Id, ContractNumber = "C-2", ValidFrom = new DateTime(2026, 7, 1) });

        var march = await _business.GetDocumentSettingsAsync(client.Id, new DateTime(2026, 3, 31));
        Assert.AreEqual("Issuer", march.CompanyName);
        Assert.AreEqual("D-1", march.DecisionNumber);
        Assert.AreEqual("Test Company d.o.o.", march.ClientName, "The company name on documents is used");
        Assert.AreEqual("222", march.ClientVatNumber);
        Assert.AreEqual("C-1", march.ContractPrefix);
        Assert.AreEqual(2026, march.ContractYear);

        var october = await _business.GetDocumentSettingsAsync(client.Id, new DateTime(2026, 10, 31));
        Assert.AreEqual("C-2", october.ContractPrefix);

        var shortNameOnly = await _business.SaveClientAsync(new Client { Id = client.Id, Name = client.Name });
        var noLegal = await _business.GetDocumentSettingsAsync(shortNameOnly.Id, new DateTime(2026, 3, 31));
        Assert.AreEqual(client.Name, noLegal.ClientName, "The short name is printed when there is no company name");
    }

    [TestMethod]
    public async Task WorkOrder_ShouldContainTheContractNumberAndTheSignatureAndStampImages()
    {
        await _business.SaveProfileAsync(new BusinessProfile { CompanyName = "Issuer d.o.o.", CompanyId = "1", DecisionNumber = "D-1" });
        await _business.SetSignatureImageAsync(TempFile(".png", Png));
        await _business.SetStampImageAsync(TempFile(".png", Png));
        var client = await NewClientAsync();
        await _business.SaveContractAsync(new ClientContract { ClientId = client.Id, ContractNumber = "078-01", ValidFrom = new DateTime(2026, 1, 1) });

        var export = _host!.Services.GetRequiredService<IExportService>();
        var tasks = new List<DailyTask> { new() { Description = "Work", Date = new DateTime(2026, 3, 10), Hours = 8 } };
        var settings = await _business.GetDocumentSettingsAsync(client.Id, new DateTime(2026, 3, 31));

        var docx = TempFile(".docx");
        await export.ExportTasksToWordAsync(tasks, settings, docx);
        using (var word = WordprocessingDocument.Open(docx, false))
        {
            var text = word.MainDocumentPart!.Document.InnerText;
            StringAssert.Contains(text, "078-01/26", "Contract number and the year of the period");
            StringAssert.Contains(text, "Test Company d.o.o.");
            Assert.AreEqual(2, word.MainDocumentPart.ImageParts.Count(), "Signature and stamp are embedded");

            var invalid = new DocumentFormat.OpenXml.Validation.OpenXmlValidator().Validate(word)
                .Where(e => e.Description.Contains("Drawing", StringComparison.OrdinalIgnoreCase)
                            || e.Description.Contains("Inline", StringComparison.OrdinalIgnoreCase)
                            || e.Description.Contains("Picture", StringComparison.OrdinalIgnoreCase))
                .Select(e => e.Description).ToList();
            Assert.AreEqual(0, invalid.Count, "The embedded images are valid Word: " + string.Join("; ", invalid));
        }

        var pdf = TempFile(".pdf");
        await export.ExportTasksToPdfAsync(tasks, settings, pdf);
        Assert.AreEqual("%PDF", System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(pdf), 0, 4));

        var plain = TempFile(".docx");
        await export.ExportTasksToWordAsync(tasks, await _business.GetDocumentSettingsAsync(client.Id, new DateTime(2026, 3, 31), includeSignatures: false), plain);
        using var unsigned = WordprocessingDocument.Open(plain, false);
        Assert.AreEqual(0, unsigned.MainDocumentPart!.ImageParts.Count(), "An unsigned document has no images");
    }

    [TestMethod]
    public async Task DocumentSettings_ShouldUseTheOnlyClientWhenNoneIsChosen()
    {
        var all = await _business.GetClientsAsync(includeInactive: false);
        if (all.Count != 0) Assert.Inconclusive("The test user already has clients, so the single-client rule cannot be checked.");

        var client = await NewClientAsync();
        var settings = await _business.GetDocumentSettingsAsync(null, DateTime.Today);
        Assert.AreEqual("Test Company d.o.o.", settings.ClientName);
        Assert.IsNotNull(client);
    }
}
