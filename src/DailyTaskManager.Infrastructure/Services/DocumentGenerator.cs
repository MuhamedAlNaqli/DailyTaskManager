using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Infrastructure.Services;

/// <summary>
/// Fills the user's Word and Excel template files. A placeholder is written {{Name}}; a table row (Word) or sheet row
/// (Excel) that contains {{Task.Date}}, {{Task.Description}}, {{Task.Hours}} or {{Task.Index}} is repeated once per
/// entry. A placeholder without a value is left visible and reported, so a mistake in a template is noticed.
/// </summary>
public class DocumentGenerator : IDocumentGenerator
{
    private static readonly Regex PlaceholderPattern = new(@"\{\{\s*([A-Za-z0-9_.]+)\s*\}\}", RegexOptions.Compiled);
    private static readonly Regex TaskRowPattern = new(@"\{\{\s*Task\.", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly string[] MonthNames =
        { "Januar", "Februar", "Mart", "April", "Maj", "Juni", "Juli", "August", "Septembar", "Oktobar", "Novembar", "Decembar" };

    private static readonly CultureInfo Money = CreateCulture("de-DE");

    public IReadOnlyList<string> KnownPlaceholders { get; } = new[]
    {
        "Month", "Year", "PeriodFrom", "PeriodTo", "Today", "IssueDate",
        "Me.CompanyName", "Me.CompanyId", "Me.VatNumber", "Me.Address", "Me.Address1", "Me.Address2", "Me.Phone", "Me.Email", "Me.DecisionNumber",
        "Client.Name", "Client.LegalName", "Client.CompanyId", "Client.VatNumber", "Client.Address", "Client.Address1", "Client.Address2",
        "Contract.Number", "Contract.Prefix",
        "TotalHours", "Rate", "Amount", "NetAmount", "VatPercent", "VatAmount", "GrossAmount", "Currency", "InvoiceNumber",
        "Task.Index", "Task.Date", "Task.Description", "Task.Hours",
        "Signature", "Stamp"
    };

    private readonly IBusinessService _business;
    private readonly ILogger<DocumentGenerator> _logger;

    public DocumentGenerator(IBusinessService business, ILogger<DocumentGenerator> logger)
    {
        _business = business;
        _logger = logger;
    }

    private static CultureInfo CreateCulture(string name)
    {
        try { return new CultureInfo(name); } catch (CultureNotFoundException) { return CultureInfo.InvariantCulture; }
    }

    // ---- Data -------------------------------------------------------------------------------------------------

    private sealed class Data
    {
        public Dictionary<string, string> Text { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, decimal> Number { get; } = new(StringComparer.OrdinalIgnoreCase);
        public byte[]? Signature { get; set; }
        public byte[]? Stamp { get; set; }
        public List<DailyTask> Tasks { get; set; } = new();
        public string ClientName { get; set; } = "";
        public DateTime PeriodEnd { get; set; }
        public string InvoiceNumber { get; set; } = "";
    }

    private sealed record TaskContext(int Index, DailyTask Task);

    private async Task<Data> BuildDataAsync(DocumentRequest request)
    {
        var profile = await _business.GetProfileAsync();
        var client = (await _business.GetClientsAsync()).FirstOrDefault(c => c.Id == request.ClientId)
                     ?? throw new InvalidOperationException("Client not found.");
        var contract = await _business.GetContractOnAsync(client.Id, request.PeriodTo);
        var images = await _business.GetDocumentSettingsAsync(client.Id, request.PeriodTo);

        var tasks = request.Tasks.OrderBy(t => t.Date).ToList();
        var total = tasks.Sum(t => t.Hours);
        var rate = request.HourlyRate ?? contract?.HourlyRate;
        var currency = string.IsNullOrWhiteSpace(request.Currency) ? client.Currency : request.Currency.Trim();

        var data = new Data { Tasks = tasks, ClientName = client.Name, PeriodEnd = request.PeriodTo, InvoiceNumber = request.InvoiceNumber };
        var t = data.Text;

        t["Month"] = MonthNames[request.PeriodTo.Month - 1];
        t["Year"] = request.PeriodTo.Year.ToString(CultureInfo.InvariantCulture);
        t["PeriodFrom"] = Date(request.PeriodFrom);
        t["PeriodTo"] = Date(request.PeriodTo);
        t["Today"] = Date(DateTime.Today);
        t["IssueDate"] = Date(request.IssueDate);

        t["Me.CompanyName"] = profile.CompanyName;
        t["Me.CompanyId"] = profile.CompanyId;
        t["Me.VatNumber"] = profile.VatNumber;
        t["Me.Address1"] = profile.AddressLine1;
        t["Me.Address2"] = profile.AddressLine2;
        t["Me.Address"] = Join(profile.AddressLine1, profile.AddressLine2);
        t["Me.Phone"] = profile.Phone;
        t["Me.Email"] = profile.Email;
        t["Me.DecisionNumber"] = profile.DecisionNumber;

        t["Client.Name"] = client.Name;
        t["Client.LegalName"] = client.PrintedName;
        t["Client.CompanyId"] = client.CompanyId;
        t["Client.VatNumber"] = client.VatNumber;
        t["Client.Address1"] = client.AddressLine1;
        t["Client.Address2"] = client.AddressLine2;
        t["Client.Address"] = Join(client.AddressLine1, client.AddressLine2);

        t["Contract.Prefix"] = contract?.ContractNumber ?? "";
        t["Contract.Number"] = contract == null ? "" : $"{contract.ContractNumber}/{request.PeriodTo.Year % 100:00}";

        t["TotalHours"] = total.ToString("0.##", Money);
        data.Number["TotalHours"] = total;
        t["Currency"] = currency;
        t["InvoiceNumber"] = request.InvoiceNumber;
        t["VatPercent"] = request.VatPercent.ToString("0.##", Money);
        data.Number["VatPercent"] = request.VatPercent;

        t["Rate"] = t["Amount"] = t["NetAmount"] = t["VatAmount"] = t["GrossAmount"] = "";
        if (rate is > 0)
        {
            var net = Math.Round(total * rate.Value, 2, MidpointRounding.AwayFromZero);
            var vat = Math.Round(net * request.VatPercent / 100m, 2, MidpointRounding.AwayFromZero);
            t["Rate"] = rate.Value.ToString("N2", Money);
            t["Amount"] = t["NetAmount"] = net.ToString("N2", Money);
            t["VatAmount"] = vat.ToString("N2", Money);
            t["GrossAmount"] = (net + vat).ToString("N2", Money);
            data.Number["Rate"] = rate.Value;
            data.Number["Amount"] = data.Number["NetAmount"] = net;
            data.Number["VatAmount"] = vat;
            data.Number["GrossAmount"] = net + vat;
        }

        t["Signature"] = t["Stamp"] = "";
        if (File.Exists(images.SignatureImagePath)) data.Signature = File.ReadAllBytes(images.SignatureImagePath);
        if (File.Exists(images.StampImagePath)) data.Stamp = File.ReadAllBytes(images.StampImagePath);
        return data;
    }

    private static string Date(DateTime d) => $"{d.Day}. {d.Month}. {d.Year}.";

    private static string Join(string a, string b) => string.Join(", ", new[] { a, b }.Where(x => !string.IsNullOrWhiteSpace(x)));

    /// <summary>The text for a placeholder, or null when the name is not known</summary>
    private static string? Resolve(string name, Data data, TaskContext? task)
    {
        if (name.StartsWith("Task.", StringComparison.OrdinalIgnoreCase))
        {
            if (task == null) return null;
            return name.Substring(5).ToLowerInvariant() switch
            {
                "index" => task.Index.ToString(CultureInfo.InvariantCulture),
                "date" => Date(task.Task.Date),
                "description" => task.Task.Description,
                "hours" => task.Task.Hours.ToString("0.##", Money),
                _ => null
            };
        }

        return data.Text.TryGetValue(name, out var value) ? value : null;
    }

    private static string Replace(string text, Data data, TaskContext? task, List<string> unknown) =>
        PlaceholderPattern.Replace(text, m =>
        {
            var value = Resolve(m.Groups[1].Value, data, task);
            if (value != null) return value;
            unknown.Add(m.Groups[1].Value);
            return m.Value;
        });

    // ---- Public -----------------------------------------------------------------------------------------------

    public async Task<GeneratedDocument> GenerateAsync(DocumentTemplate template, DocumentRequest request)
    {
        if (!File.Exists(template.TemplatePath)) throw new FileNotFoundException("The template file is missing", template.TemplatePath);
        if (string.IsNullOrWhiteSpace(request.Folder)) throw new ArgumentException("A folder is needed.");

        var data = await BuildDataAsync(request);
        Directory.CreateDirectory(request.Folder);

        var name = Sanitize(ApplyPattern(template, data, request));
        var target = Path.Combine(request.Folder, name + "." + template.Format);
        File.Copy(template.TemplatePath, target, overwrite: true);

        var unknown = new List<string>();
        try
        {
            if (template.Format == "xlsx") FillExcel(target, data, unknown);
            else FillWord(target, data, unknown);
        }
        catch
        {
            try { File.Delete(target); } catch { /* a half-written file is of no use */ }
            throw;
        }

        _logger.LogInformation("Generated {Path} from template {Template} ({Unknown} unknown placeholders)", target, template.Name, unknown.Distinct().Count());
        return new GeneratedDocument { Path = target, UnknownPlaceholders = unknown.Distinct(StringComparer.OrdinalIgnoreCase).ToList() };
    }

    public Task<IReadOnlyList<string>> ScanPlaceholdersAsync(string templatePath)
    {
        var found = new List<string>();
        var texts = new List<string>();

        if (Path.GetExtension(templatePath).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            using var workbook = new XLWorkbook(templatePath);
            foreach (var cell in workbook.Worksheets.SelectMany(w => w.CellsUsed()).Where(c => c.DataType == XLDataType.Text))
                texts.Add(cell.GetString());
        }
        else
        {
            using var document = WordprocessingDocument.Open(templatePath, false);
            var roots = new List<OpenXmlElement> { document.MainDocumentPart!.Document.Body! };
            roots.AddRange(document.MainDocumentPart.HeaderParts.Select(h => (OpenXmlElement)h.Header));
            roots.AddRange(document.MainDocumentPart.FooterParts.Select(f => (OpenXmlElement)f.Footer));
            foreach (var paragraph in roots.SelectMany(r => r.Descendants<Paragraph>()))
                texts.Add(string.Concat(paragraph.Descendants<Text>().Select(t => t.Text)));
        }

        foreach (var text in texts)
            foreach (Match m in PlaceholderPattern.Matches(text)) found.Add(m.Groups[1].Value);

        return Task.FromResult<IReadOnlyList<string>>(found.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }

    private static string ApplyPattern(DocumentTemplate template, Data data, DocumentRequest request)
    {
        var pattern = string.IsNullOrWhiteSpace(template.FileNamePattern) ? DocumentTemplate.DefaultFileNamePattern : template.FileNamePattern;
        return pattern
            .Replace("{Name}", template.Name, StringComparison.OrdinalIgnoreCase)
            .Replace("{Client}", data.ClientName, StringComparison.OrdinalIgnoreCase)
            .Replace("{YYYY}", request.PeriodTo.Year.ToString("0000", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{MM}", request.PeriodTo.Month.ToString("00", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{Month}", MonthNames[request.PeriodTo.Month - 1], StringComparison.OrdinalIgnoreCase)
            .Replace("{InvoiceNumber}", request.InvoiceNumber, StringComparison.OrdinalIgnoreCase);
    }

    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string(name.Select(c => invalid.Contains(c) ? '-' : c).ToArray()).Trim().Trim('.');
        return clean.Length == 0 ? "Document" : (clean.Length > 120 ? clean.Substring(0, 120) : clean);
    }

    // ---- Word -------------------------------------------------------------------------------------------------

    private static void FillWord(string path, Data data, List<string> unknown)
    {
        using var document = WordprocessingDocument.Open(path, true);
        var main = document.MainDocumentPart!;

        var parts = new List<(OpenXmlElement Root, MainDocumentPart? ImagePart)> { (main.Document.Body!, main) };
        parts.AddRange(main.HeaderParts.Select(h => ((OpenXmlElement)h.Header, (MainDocumentPart?)null)));
        parts.AddRange(main.FooterParts.Select(f => ((OpenXmlElement)f.Footer, (MainDocumentPart?)null)));

        foreach (var (root, imagePart) in parts)
        {
            // Rows with {{Task.*}} become one row per entry
            foreach (var row in root.Descendants<TableRow>().Where(r => TaskRowPattern.IsMatch(r.InnerText)).ToList())
            {
                var index = 1;
                foreach (var task in data.Tasks)
                {
                    var clone = (TableRow)row.CloneNode(true);
                    var context = new TaskContext(index++, task);
                    foreach (var paragraph in clone.Descendants<Paragraph>().ToList()) FillParagraph(paragraph, data, context, imagePart, unknown);
                    row.InsertBeforeSelf(clone);
                }

                row.Remove();
            }

            foreach (var paragraph in root.Descendants<Paragraph>().ToList()) FillParagraph(paragraph, data, null, imagePart, unknown);
        }

        main.Document.Save();
        foreach (var header in main.HeaderParts) header.Header.Save();
        foreach (var footer in main.FooterParts) footer.Footer.Save();
    }

    private static void FillParagraph(Paragraph paragraph, Data data, TaskContext? task, MainDocumentPart? imagePart, List<string> unknown)
    {
        var texts = paragraph.Descendants<Text>().ToList();
        if (texts.Count == 0) return;

        // Placeholders are often split over several runs by Word; work on the joined text
        var full = string.Concat(texts.Select(t => t.Text));
        if (!full.Contains("{{", StringComparison.Ordinal)) return;

        var trimmed = full.Trim();
        var whole = PlaceholderPattern.Match(trimmed);
        if (whole.Success && whole.Value == trimmed)
        {
            var image = whole.Groups[1].Value.ToLowerInvariant() switch
            {
                "signature" => (Bytes: data.Signature, MaxWidth: 130d, MaxHeight: 60d),
                "stamp" => (Bytes: data.Stamp, MaxWidth: 85d, MaxHeight: 85d),
                _ => (Bytes: (byte[]?)null, MaxWidth: 0d, MaxHeight: 0d)
            };

            if (whole.Groups[1].Value.Equals("Signature", StringComparison.OrdinalIgnoreCase) || whole.Groups[1].Value.Equals("Stamp", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var t in texts) t.Remove();
                if (image.Bytes != null && imagePart != null)
                {
                    var (w, h) = ExportService.FitBox(image.Bytes, image.MaxWidth, image.MaxHeight);
                    var graphic = ExportService.BuildPictureGraphic(imagePart, image.Bytes, (long)(w * 12700), (long)(h * 12700), whole.Groups[1].Value);
                    var inline = new DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline(
                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent { Cx = (long)(w * 12700), Cy = (long)(h * 12700) },
                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties { Id = (uint)Math.Abs(whole.Value.GetHashCode() % 100000) + 200, Name = whole.Groups[1].Value },
                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.NonVisualGraphicFrameDrawingProperties(
                            new DocumentFormat.OpenXml.Drawing.GraphicFrameLocks { NoChangeAspect = true }),
                        graphic)
                    { DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U };
                    paragraph.AppendChild(new Run(new Drawing(inline)));
                }

                return;
            }
        }

        var replaced = Replace(full, data, task, unknown);
        texts[0].Text = replaced;
        texts[0].Space = SpaceProcessingModeValues.Preserve;
        foreach (var extra in texts.Skip(1)) extra.Remove();
    }

    // ---- Excel ------------------------------------------------------------------------------------------------

    private static void FillExcel(string path, Data data, List<string> unknown)
    {
        using var workbook = new XLWorkbook(path);
        foreach (var sheet in workbook.Worksheets)
        {
            var taskRows = sheet.RowsUsed()
                .Where(r => r.CellsUsed().Any(c => c.DataType == XLDataType.Text && TaskRowPattern.IsMatch(c.GetString())))
                .Select(r => r.RowNumber()).OrderByDescending(n => n).ToList();
            foreach (var number in taskRows) ExpandExcelRow(sheet, number, data, unknown);

            foreach (var cell in sheet.CellsUsed().Where(c => c.DataType == XLDataType.Text && c.GetString().Contains("{{", StringComparison.Ordinal)).ToList())
                FillCell(sheet, cell, cell.GetString(), data, null, unknown);
        }

        workbook.Save();
    }

    private static void ExpandExcelRow(IXLWorksheet sheet, int rowNumber, Data data, List<string> unknown)
    {
        var template = sheet.Row(rowNumber).CellsUsed()
            .Select(c => (Column: c.Address.ColumnNumber, Text: c.DataType == XLDataType.Text ? c.GetString() : null))
            .ToList();

        if (data.Tasks.Count == 0)
        {
            sheet.Row(rowNumber).Delete();
            return;
        }

        if (data.Tasks.Count > 1) sheet.Row(rowNumber).InsertRowsBelow(data.Tasks.Count - 1);

        for (var i = 0; i < data.Tasks.Count; i++)
        {
            var context = new TaskContext(i + 1, data.Tasks[i]);
            foreach (var (column, text) in template)
            {
                var cell = sheet.Cell(rowNumber + i, column);
                if (text != null) FillCell(sheet, cell, text, data, context, unknown);
            }
        }
    }

    private static void FillCell(IXLWorksheet sheet, IXLCell cell, string text, Data data, TaskContext? task, List<string> unknown)
    {
        var trimmed = text.Trim();
        var whole = PlaceholderPattern.Match(trimmed);
        if (whole.Success && whole.Value == trimmed)
        {
            var name = whole.Groups[1].Value;

            var image = name.ToLowerInvariant() switch
            {
                "signature" => (Known: true, Bytes: data.Signature, MaxWidth: 130d, MaxHeight: 60d),
                "stamp" => (Known: true, Bytes: data.Stamp, MaxWidth: 85d, MaxHeight: 85d),
                _ => (Known: false, Bytes: (byte[]?)null, MaxWidth: 0d, MaxHeight: 0d)
            };
            if (image.Known)
            {
                cell.Clear(XLClearOptions.Contents);
                if (image.Bytes != null)
                {
                    var (w, h) = ExportService.FitBox(image.Bytes, image.MaxWidth, image.MaxHeight);
                    sheet.AddPicture(new MemoryStream(image.Bytes)).MoveTo(cell).WithSize((int)(w * 96 / 72), (int)(h * 96 / 72));
                }

                return;
            }

            // A cell that holds only a number or a date stays a number or a date, so Excel can calculate with it
            if (task != null && name.Equals("Task.Hours", StringComparison.OrdinalIgnoreCase)) { cell.Value = task.Task.Hours; return; }
            if (task != null && name.Equals("Task.Index", StringComparison.OrdinalIgnoreCase)) { cell.Value = task.Index; return; }
            if (task != null && name.Equals("Task.Date", StringComparison.OrdinalIgnoreCase))
            {
                cell.Value = task.Task.Date.Date;
                if (cell.Style.NumberFormat.Format == "General") cell.Style.NumberFormat.Format = "d. m. yyyy.";
                return;
            }

            if (data.Number.TryGetValue(name, out var number) && data.Text.TryGetValue(name, out var shown) && shown.Length > 0)
            {
                cell.Value = number;
                return;
            }
        }

        cell.Value = Replace(text, data, task, unknown);
    }
}
