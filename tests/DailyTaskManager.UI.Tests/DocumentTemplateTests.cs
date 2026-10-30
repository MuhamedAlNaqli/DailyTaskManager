using System.Reflection;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
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

/// <summary>Word and Excel template files with {{placeholders}} (real services, real database)</summary>
[TestClass]
[DoNotParallelize]
public class DocumentTemplateTests
{
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    private IHost? _host;
    private IBusinessService _business = null!;
    private IDocumentGenerator _generator = null!;
    private BusinessProfile _originalProfile = null!;
    private Client _client = null!;
    private string _folder = "";
    private readonly List<int> _templates = new();
    private readonly List<string> _files = new();

    private static readonly List<DailyTask> Tasks = new()
    {
        new() { Date = new DateTime(2026, 3, 2), Description = "First task", Hours = 7 },
        new() { Date = new DateTime(2026, 3, 3), Description = "Second task", Hours = 6.5m },
        new() { Date = new DateTime(2026, 3, 4), Description = "Third task", Hours = 7 },
    };

    [TestInitialize]
    public async Task Setup()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                config.Sources.Clear();
                var appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Environment.CurrentDirectory;
                var avaloniaDir = Path.GetFullPath(Path.Combine(appDir, "..", "..", "..", "..", "..", "src", "DailyTaskManager.Avalonia"));
                config.SetBasePath(avaloniaDir).AddJsonFile("appsettings.json", optional: false).AddJsonFile("appsettings.local.json", optional: true);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddDbContextFactory<DailyTaskManagerDbContext>(options => options.UseNpgsql(context.Configuration.GetConnectionString("DefaultConnection")));
                services.AddSingleton<IDatabaseService, DatabaseService>();
                services.AddSingleton<ISessionStore, SessionStore>();
                services.AddSingleton<IAuthenticationService, AuthenticationService>();
                services.AddSingleton<IBusinessService, BusinessService>();
                services.AddSingleton<IDocumentGenerator, DocumentGenerator>();
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

        Assert.IsTrue(login.IsSuccess);

        _business = _host.Services.GetRequiredService<IBusinessService>();
        _generator = _host.Services.GetRequiredService<IDocumentGenerator>();
        _originalProfile = await _business.GetProfileAsync();
        await _business.SaveProfileAsync(new BusinessProfile { CompanyName = "Issuer d.o.o.", CompanyId = "1", DecisionNumber = "D-1", VatNumber = "V-1" });

        _client = await _business.SaveClientAsync(new Client
        {
            Name = "Tpl " + Guid.NewGuid().ToString("N")[..6], LegalName = "Test Company d.o.o.", CompanyId = "111", VatNumber = "222",
            AddressLine1 = "Street 1", AddressLine2 = "71000 Sarajevo", Currency = "EUR"
        });
        await _business.SaveContractAsync(new ClientContract { ClientId = _client.Id, ContractNumber = "078-01", HourlyRate = 20m, ValidFrom = new DateTime(2026, 1, 1) });

        _folder = Path.Combine(Path.GetTempPath(), "dtm_tpl_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        foreach (var id in _templates) await _business.DeleteTemplateAsync(id);
        await _business.DeleteOrDeactivateClientAsync(_client.Id);
        await _business.RemoveSignatureImageAsync();
        await _business.SaveProfileAsync(_originalProfile);
        _host?.Dispose();
        try { Directory.Delete(_folder, true); } catch { /* temp */ }
        foreach (var file in _files) if (File.Exists(file)) File.Delete(file);
    }

    private DocumentRequest Request(string invoice = "") => new()
    {
        ClientId = _client.Id, PeriodFrom = new DateTime(2026, 3, 1), PeriodTo = new DateTime(2026, 3, 31),
        Tasks = Tasks, Folder = _folder, InvoiceNumber = invoice
    };

    private string MakeWordTemplate()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dtm_{Guid.NewGuid():N}.docx");
        _files.Add(path);
        using (var doc = WordprocessingDocument.Create(path, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
        {
            var main = doc.AddMainDocumentPart();
            main.Document = new Document();
            var body = main.Document.AppendChild(new Body());

            // A placeholder that Word split over two runs
            body.AppendChild(new Paragraph(new Run(new Text("Client: {{Client.") { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve }),
                                           new Run(new Text("LegalName}} / {{Contract.Number}}"))));
            body.AppendChild(new Paragraph(new Run(new Text("{{Signature}}"))));
            body.AppendChild(new Paragraph(new Run(new Text("Unknown {{Nope}} here"))));

            var table = new Table();
            table.AppendChild(new TableRow(Cell("No"), Cell("Date"), Cell("Description"), Cell("Hours")));
            table.AppendChild(new TableRow(Cell("{{Task.Index}}"), Cell("{{Task.Date}}"), Cell("{{Task.Description}}"), Cell("{{Task.Hours}}")));
            body.AppendChild(table);

            body.AppendChild(new Paragraph(new Run(new Text("Total {{TotalHours}} h = {{Amount}} {{Currency}} ({{InvoiceNumber}})"))));
            main.Document.Save();
        }

        return path;
    }

    private static TableCell Cell(string text) => new(new Paragraph(new Run(new Text(text))));

    [TestMethod]
    public async Task Word_ShouldFillPlaceholdersRepeatTaskRowsAndReportUnknownOnes()
    {
        var png = Path.Combine(Path.GetTempPath(), $"dtm_{Guid.NewGuid():N}.png");
        File.WriteAllBytes(png, Png);
        _files.Add(png);
        await _business.SetSignatureImageAsync(png);

        var template = await _business.AddTemplateAsync(_client.Id, MakeWordTemplate(), "Timesheet");
        _templates.Add(template.Id);

        var result = await _generator.GenerateAsync(template, Request("001/26"));

        CollectionAssert.AreEqual(new[] { "Nope" }, result.UnknownPlaceholders.ToArray(), "Only the unknown placeholder is reported");
        StringAssert.Contains(Path.GetFileName(result.Path), "Timesheet");

        using var doc = WordprocessingDocument.Open(result.Path, false);
        var text = doc.MainDocumentPart!.Document.InnerText;
        StringAssert.Contains(text, "Client: Test Company d.o.o. / 078-01/26", "A placeholder split over runs is found");
        StringAssert.Contains(text, "{{Nope}}", "An unknown placeholder stays visible");
        StringAssert.Contains(text, "Total 20,5 h = 410,00 EUR (001/26)");
        StringAssert.Contains(text, "Second task");
        StringAssert.Contains(text, "3. 3. 2026.");

        Assert.AreEqual(4, doc.MainDocumentPart.Document.Descendants<TableRow>().Count(), "Header and one row per entry");
        Assert.AreEqual(1, doc.MainDocumentPart.ImageParts.Count(), "{{Signature}} became the signature image");
        Assert.IsFalse(text.Contains("{{Signature}}"));
    }

    [TestMethod]
    public async Task Excel_ShouldFillCellsKeepNumbersAndRepeatTaskRows()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dtm_{Guid.NewGuid():N}.xlsx");
        _files.Add(path);
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Hours");
            sheet.Cell("A1").Value = "{{Client.Name}} - {{Month}} {{Year}}";
            sheet.Cell("A3").Value = "{{Task.Index}}";
            sheet.Cell("B3").Value = "{{Task.Date}}";
            sheet.Cell("C3").Value = "{{Task.Description}}";
            sheet.Cell("D3").Value = "{{Task.Hours}}";
            sheet.Cell("A4").Value = "Total";
            sheet.Cell("D4").Value = "{{TotalHours}}";
            sheet.Cell("D5").Value = "{{Amount}}";
            sheet.Cell("A6").Value = "{{Missing}}";
            workbook.SaveAs(path);
        }

        var template = await _business.AddTemplateAsync(_client.Id, path, "Hours sheet");
        _templates.Add(template.Id);
        Assert.AreEqual("xlsx", template.Format);

        var result = await _generator.GenerateAsync(template, Request());
        CollectionAssert.AreEqual(new[] { "Missing" }, result.UnknownPlaceholders.ToArray());

        using var output = new XLWorkbook(result.Path);
        var sheetOut = output.Worksheet("Hours");
        StringAssert.Contains(sheetOut.Cell("A1").GetString(), "Mart 2026");
        Assert.AreEqual("Second task", sheetOut.Cell("C4").GetString());
        Assert.AreEqual(6.5, sheetOut.Cell("D4").GetDouble(), "Hours stay numbers");
        Assert.AreEqual(2, sheetOut.Cell("A4").GetDouble());
        Assert.AreEqual(new DateTime(2026, 3, 4), sheetOut.Cell("B5").GetDateTime());

        Assert.AreEqual("Total", sheetOut.Cell("A6").GetString(), "The rows below move down by the added rows");
        Assert.AreEqual(20.5, sheetOut.Cell("D6").GetDouble(), "TotalHours is a number");
        Assert.AreEqual(410.0, sheetOut.Cell("D7").GetDouble(), "Amount is hours x contract rate");
        Assert.AreEqual("{{Missing}}", sheetOut.Cell("A8").GetString());
    }

    [TestMethod]
    public async Task Scan_ShouldListThePlaceholdersOfATemplate()
    {
        var template = await _business.AddTemplateAsync(_client.Id, MakeWordTemplate());
        _templates.Add(template.Id);

        var found = await _generator.ScanPlaceholdersAsync(template.TemplatePath);
        CollectionAssert.IsSubsetOf(new[] { "Client.LegalName", "Task.Hours", "Nope", "Signature" }, found.ToList());
        CollectionAssert.Contains(_generator.KnownPlaceholders.ToList(), "Contract.Number");
    }

    [TestMethod]
    public async Task Templates_ShouldBeManagedPerClientAndRejectOtherFiles()
    {
        var text = Path.Combine(Path.GetTempPath(), $"dtm_{Guid.NewGuid():N}.txt");
        File.WriteAllText(text, "x");
        _files.Add(text);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => _business.AddTemplateAsync(_client.Id, text));

        var template = await _business.AddTemplateAsync(_client.Id, MakeWordTemplate(), "Invoice");
        _templates.Add(template.Id);
        Assert.IsTrue(File.Exists(template.TemplatePath), "The file is copied into the application's folder");

        template.Name = "Renamed";
        template.FileNamePattern = "{Name} {InvoiceNumber}";
        template.IsActive = false;
        var saved = await _business.UpdateTemplateAsync(template);
        Assert.AreEqual("Renamed", saved.Name);
        Assert.IsFalse((await _business.GetTemplatesAsync(_client.Id)).Single().IsActive);

        var result = await _generator.GenerateAsync(saved, Request("001/26"));
        Assert.AreEqual("Renamed 001-26.docx", Path.GetFileName(result.Path), "Characters that are not allowed in file names are replaced");

        await _business.DeleteTemplateAsync(template.Id);
        Assert.IsFalse(File.Exists(template.TemplatePath), "The stored file is deleted with the template");
        Assert.AreEqual(0, (await _business.GetTemplatesAsync(_client.Id)).Count);
    }
}
