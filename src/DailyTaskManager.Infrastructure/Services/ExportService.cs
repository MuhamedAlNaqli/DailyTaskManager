using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OpenXmlDocument = DocumentFormat.OpenXml.Wordprocessing.Document;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Infrastructure.Services;

/// <summary>
/// Service for data export operations
/// </summary>
public class ExportService : IExportService
{
    private readonly Dictionary<string, string> _months = new Dictionary<string, string>
    {
        { "January", "Januar" },
        { "February", "Februar" },
        { "March", "Mart" },
        { "April", "April" },
        { "May", "Maj" },
        { "June", "Juni" },
        { "July", "Juli" },
        { "August", "August" },
        { "September", "Septembar" },
        { "October", "Oktobar" },
        { "November", "Novembar" },
        { "December", "Decembar" }
    };

    private readonly ILogger<ExportService> _logger;
    private readonly ISettingsService _settingsService;

    public ExportService(ILogger<ExportService> logger, ISettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
    }

    public async Task<string> ExportTasksToExcelAsync(List<DailyTask> tasks, string? filePath = null)
    {
        try
        {
            // Order list ascending
            tasks = tasks
                .OrderBy(x => x.Date)
                .ToList();

            if (string.IsNullOrEmpty(filePath))
            {
                var downloadPath = await _settingsService.GetExcelExportPathAsync();
                filePath = Path.Combine(downloadPath, $"Pregled utrošenih sati - MA - {DateTime.Now:MMyy}.xlsx");
            }

            _logger.LogInformation("Exporting {Count} tasks to Excel: {FilePath}", tasks.Count, filePath);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Sati");

            // Headers - matching reference format exactly
            worksheet.Cell(1, 1).Value = "Red br";
            worksheet.Cell(1, 2).Value = "Kratak opis aktivnosti";
            worksheet.Cell(1, 3).Value = "Datum";
            worksheet.Cell(1, 4).Value = "Broj sati";

            // Style headers to match reference format
            var headerRange = worksheet.Range(1, 1, 1, 4);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontSize = 11;
            headerRange.Style.Font.FontName = "Trebuchet MS";
            headerRange.Style.Fill.BackgroundColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Data rows - matching reference format exactly
            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                var row = i + 2;

                // Row number - centered
                worksheet.Cell(row, 1).Value = i + 1;
                worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(row, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                worksheet.Cell(row, 1).Style.Font.FontName = "Aptos Narrow";
                worksheet.Cell(row, 1).Style.Font.FontSize = 11;

                // Description - with prefix "Daily" and line break, left aligned
                worksheet.Cell(row, 2).Value = task.Description;
                worksheet.Cell(row, 2).Style.Alignment.WrapText = true;
                worksheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                worksheet.Cell(row, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                worksheet.Cell(row, 2).Style.Font.FontName = "Aptos Narrow";
                worksheet.Cell(row, 2).Style.Font.FontSize = 11;

                // Date - centered, formatted as d.M.yyyy
                worksheet.Cell(row, 3).Value = task.Date;
                worksheet.Cell(row, 3).Style.DateFormat.Format = "d.M.yyyy";
                worksheet.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(row, 3).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                worksheet.Cell(row, 3).Style.Font.FontName = "Aptos Narrow";
                worksheet.Cell(row, 3).Style.Font.FontSize = 11;

                // Hours - centered
                worksheet.Cell(row, 4).Value = (double)task.Hours;
                worksheet.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(row, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                worksheet.Cell(row, 4).Style.Font.FontName = "Aptos Narrow";
                worksheet.Cell(row, 4).Style.Font.FontSize = 11;

                // Add borders to data row
                var dataRowRange = worksheet.Range(row, 1, row, 4);
                dataRowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            // Add totals row - matching reference format
            if (tasks.Count > 0)
            {
                var totalRow = tasks.Count + 2;

                // Empty cells for columns 1 and 2
                worksheet.Cell(totalRow, 1).Value = "";
                worksheet.Cell(totalRow, 2).Value = "";

                // "Ukupan broj sati" label in column 3, right aligned
                worksheet.Cell(totalRow, 3).Value = "Ukupan broj sati";
                worksheet.Cell(totalRow, 3).Style.Font.Bold = true;
                worksheet.Cell(totalRow, 3).Style.Font.FontName = "Aptos Narrow";
                worksheet.Cell(totalRow, 3).Style.Font.FontSize = 11;
                worksheet.Cell(totalRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // Total value in column 4, centered
                worksheet.Cell(totalRow, 4).Value = (double)tasks.Sum(t => t.Hours);
                worksheet.Cell(totalRow, 4).Style.Font.Bold = true;
                worksheet.Cell(totalRow, 4).Style.Font.FontName = "Aptos Narrow";
                worksheet.Cell(totalRow, 4).Style.Font.FontSize = 11;
                worksheet.Cell(totalRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Add borders to totals row
                var totalRange = worksheet.Range(totalRow, 1, totalRow, 4);
                totalRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                totalRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            // Set column widths to match reference format precisely
            worksheet.Column(1).Width = 8;   // Red br - narrow but readable
            worksheet.Column(2).Width = 60;  // Kratak opis aktivnosti - wide for descriptions
            worksheet.Column(3).Width = 15;  // Datum - medium for date
            worksheet.Column(4).Width = 15;  // Broj sati - medium for numbers

            // Add borders to the entire data range including headers and totals
            if (tasks.Count > 0)
            {
                var fullDataRange = worksheet.Range(1, 1, tasks.Count + 2, 4);
                fullDataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                fullDataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            await Task.Run(() => workbook.SaveAs(filePath));

            _logger.LogInformation("Successfully exported tasks to Excel: {FilePath}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting tasks to Excel");
            throw;
        }
    }

    public async Task<string> ExportTasksToPdfAsync(List<DailyTask> tasks, PdfSettings settings, string? filePath = null)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
            {
                var downloadPath = await _settingsService.GetPdfExportPathAsync();
                filePath = Path.Combine(downloadPath, GetDefaultPdfFileName());
            }

            _logger.LogInformation("Exporting {Count} tasks to PDF: {FilePath}", tasks.Count, filePath);

            await Task.Run(() =>
            {
                // Sort tasks by date
                var sortedTasks = tasks.OrderBy(t => t.Date).ToList();

                // Generate PDF using QuestPDF
                GenerateQuestPdfReport(sortedTasks, settings, filePath);

                _logger.LogInformation("PDF report generated: {FilePath}", filePath);
            });

            _logger.LogInformation("Successfully exported tasks to PDF: {FilePath}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting tasks to PDF");
            throw;
        }
    }

    public async Task<string> ExportTasksToPdfAsync(List<DailyTask> tasks, string? filePath = null)
    {
        // Create default PDF settings
        var defaultSettings = new PdfSettings();
        return await ExportTasksToPdfAsync(tasks, defaultSettings, filePath);
    }

    public async Task<string> ExportTasksToCsvAsync(List<DailyTask> tasks, string? filePath = null)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
            {
                var downloadPath = await _settingsService.GetExcelExportPathAsync();
                filePath = Path.Combine(downloadPath, $"DailyTasks_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }

            _logger.LogInformation("Exporting {Count} tasks to CSV: {FilePath}", tasks.Count, filePath);

            var lines = new List<string>
            {
                "Date,Description,Hours,Created,Updated"
            };

            foreach (var task in tasks)
            {
                var line = $"{task.Date:yyyy-MM-dd}," +
                          $"\"{task.Description.Replace("\"", "\"\"")}\"," +
                          $"{task.Hours}," +
                          $"{task.CreatedAt:yyyy-MM-dd HH:mm:ss}," +
                          $"{task.UpdatedAt:yyyy-MM-dd HH:mm:ss}";
                lines.Add(line);
            }

            await File.WriteAllLinesAsync(filePath, lines);

            _logger.LogInformation("Successfully exported tasks to CSV: {FilePath}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting tasks to CSV");
            throw;
        }
    }

    public async Task<string> ExportTasksToWordAsync(List<DailyTask> tasks, PdfSettings settings, string? filePath = null)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
            {
                var downloadPath = await _settingsService.GetWordExportPathAsync();
                filePath = Path.Combine(downloadPath, $"DailyTasks_{DateTime.Now:yyyyMMdd_HHmmss}.docx");
            }

            _logger.LogInformation("Exporting {Count} tasks to Word: {FilePath}", tasks.Count, filePath);

            await Task.Run(() =>
            {
                // Sort tasks by date
                var sortedTasks = tasks.OrderBy(t => t.Date).ToList();

                // Generate Word document
                GenerateWordDocument(sortedTasks, settings, filePath);
            });

            _logger.LogInformation("Successfully exported tasks to Word: {FilePath}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting tasks to Word");
            throw;
        }
    }

    public List<string> GetAvailableFormats()
    {
        return new List<string> { "Excel", "CSV", "PDF", "Word" };
    }

    public bool ValidateExportParameters(List<DailyTask> tasks, string format)
    {
        if (tasks == null || tasks.Count == 0)
        {
            return false;
        }

        var supportedFormats = GetAvailableFormats();
        return supportedFormats.Contains(format, StringComparer.OrdinalIgnoreCase);
    }

    private string GetDefaultPdfFileName()
    {
        return $"DailyTasks_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
    }

    private void GenerateQuestPdfReport(List<DailyTask> tasks, PdfSettings settings, string filePath)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var total = tasks.Sum(t => t.Hours);
        var maxTaskDate = tasks.Any() ? tasks.Max(t => t.Date) : DateTime.Now;
        var currentMonth = _months[maxTaskDate.ToString("MMMM")];
        var currentYear = maxTaskDate.ToString("yyyy");
        var period = tasks.Any() ? $"{currentMonth} {currentYear}." : DateTime.Now.ToString("MMMM yyyy.");
        var contractNumber = $"{settings.ContractPrefix}/{DateTime.Now:yy}";

        string headingsFontColor = "#FF4B4B"; // Dark Red

        QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(70);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Trebuchet MS"));

                page.Content().Element(ComposeContent);


                void ComposeContent(IContainer container)
                {
                    container.Column(column =>
                    {
                        // Header content (only on first page)
                        column.Item().Column(headerColumn =>
                        {
                            // Company info
                            headerColumn.Item().PaddingBottom(15).Text(text =>
                            {
                                text.DefaultTextStyle(x => x.LineHeight(1.0f)); // <- sets line height to 1 (tight)

                                // header (bold, bigger)
                                text.Span(settings.CompanyName)
                                    .Bold()
                                    .FontSize(16)
                                    .FontColor(headingsFontColor);

                                // small lines separated with \n (no extra gap)
                                text.Span(
                                    $"\nID broj: {settings.CompanyId}\n" +
                                    $"Broj rješenja: 16-17-320/24\n" +
                                    $"{settings.AddressLine1}\n" +
                                    $"{settings.AddressLine2}\n" +
                                    $"Telefon: {settings.Phone}\n" +
                                    $"Email: {settings.Email}"
                                ).FontSize(11);
                            });

                            // Client info
                            headerColumn.Item().PaddingBottom(28).Text(text =>
                            {
                                text.DefaultTextStyle(x => x.LineHeight(1.0f)); // <- sets line height to 1 (tight)

                                text.Span(settings.ClientName)
                                .Bold()
                                .FontSize(16)
                                .FontColor(headingsFontColor);

                                text.Span($"\nID broj: {settings.ClientId}\n" +
                                            "PDV broj: 200236190009\n" +
                                            "Grbavička 4\n" +
                                            "71000 Sarajevo"
                                 ).FontSize(11);
                            });

                            // Document title
                            headerColumn.Item().AlignCenter().Text(settings.DocumentTitle).Bold().FontSize(18).FontColor(headingsFontColor);

                            // Contract info
                            headerColumn.Item().PaddingBottom(18).AlignCenter().Text($"Ugovor o pružanju usluga broj: {contractNumber}").Bold().FontSize(11);

                            // Period info
                            headerColumn.Item().PaddingBottom(10).Text($"Izvještajni period: {period}").Bold().FontSize(11);
                        });

                        // Tasks table
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1);   // Red br
                                columns.RelativeColumn(7);   // Kratak opis
                                columns.RelativeColumn(2);   // Datum
                                columns.RelativeColumn(2);   // Ukupan broj sati
                            });

                            // Header (first row only - not using table.Header to avoid repetition)
                            table.Cell().Border(1).Padding(5).AlignCenter().Text("Red\nbr").Bold().FontSize(11);
                            table.Cell().Border(1).Padding(5).AlignCenter().Text("Kratak opis").Bold().FontSize(11);
                            table.Cell().Border(1).Padding(5).AlignCenter().Text("Datum").Bold().FontSize(11);
                            table.Cell().Border(1).Padding(5).AlignCenter().Text("Ukupan broj sati").Bold().FontSize(11);

                            // Data rows
                            var index = 1;
                            foreach (var task in tasks)
                            {
                                table.Cell().Border(1).Padding(4).AlignCenter().Text(index++.ToString()).FontSize(11);
                                table.Cell().Border(1).Padding(4).Text($"{task.Description}").FontSize(11);
                                table.Cell().Border(1).Padding(4).AlignCenter().Text($"{task.Date:d. M. yyyy}.").FontSize(11);
                                table.Cell().Border(1).Padding(4).AlignCenter().Text(task.Hours.ToString()).FontSize(11);
                            }

                            // Total row
                            table.Cell().Border(1).Padding(4).Text("").FontSize(11);
                            table.Cell().Border(1).Padding(4).Text("").FontSize(11);
                            table.Cell().Border(1).Padding(4).AlignRight().Text("Ukupan broj sati").Bold().FontSize(11);
                            table.Cell().Border(1).Padding(4).AlignCenter().Text(total.ToString()).Bold().FontSize(11);
                        });

                        if (settings.IncludeSignatures)
                        {
                            // Additional notes
                            column.Item().PaddingTop(30).Text(text =>
                            {
                                text.Span("Dodatne\nnapomene:").Bold().FontSize(11);
                                text.EmptyLine();
                                text.Span("_".PadRight(70, '_'));
                                text.EmptyLine();
                                text.Span("_".PadRight(70, '_'));
                            });

                            // Date line
                            column.Item().PaddingTop(20).Text(text =>
                            {
                                text.Span("Dana: ").FontSize(11);
                                text.Span("_".PadRight(15, '_')).FontSize(11);
                                text.Span(", godine").FontSize(11);
                            });

                            // Signatures
                            column.Item().PaddingTop(40).Row(row =>
                            {
                                row.RelativeItem().Text(text =>
                                {
                                    text.Span("Izvršilac").FontSize(11);
                                    text.EmptyLine();
                                    text.EmptyLine();
                                    text.EmptyLine();
                                    text.EmptyLine();
                                    text.EmptyLine();
                                    text.Span("_".PadRight(25, '_'));
                                });

                                row.RelativeItem().AlignRight().Text(text =>
                                {
                                    text.Span("Za Naručioca").FontSize(11);
                                    text.EmptyLine();
                                    text.EmptyLine();
                                    text.EmptyLine();
                                    text.EmptyLine();
                                    text.EmptyLine();
                                    text.Span("_".PadRight(25, '_'));
                                });
                            });
                        }
                    });
                }
            });
        }).GeneratePdf(filePath);
    }

    private void GenerateWordDocument(List<DailyTask> tasks, PdfSettings settings, string filePath)
    {
        using var document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new OpenXmlDocument();
        var body = mainPart.Document.AppendChild(new Body());

        var total = tasks.Sum(t => t.Hours);
        var period = tasks.Any() ? $"{tasks.Min(t => t.Date):MMMM yyyy}." : DateTime.Now.ToString("MMMM yyyy.");
        var contractNumber = $"{settings.ContractPrefix}/{DateTime.Now:yy}";

        // Company info
        var companyPara = body.AppendChild(new Paragraph());
        var companyRun = companyPara.AppendChild(new Run());
        companyRun.AppendChild(new RunProperties(
            new Bold(),
            new FontSize() { Val = "32" }, // 16pt = 32 half-points
            new RunFonts() { Ascii = "Trebuchet MS", HighAnsi = "Trebuchet MS" },
            new Color() { Val = "FF4B4B" }
        ));
        companyRun.AppendChild(new Text(settings.CompanyName));

        body.AppendChild(CreateStyledParagraph($"ID broj: {settings.CompanyId}"));
        body.AppendChild(CreateStyledParagraph("Broj rješenja: 16-17-320/24"));
        body.AppendChild(CreateStyledParagraph(settings.AddressLine1));
        body.AppendChild(CreateStyledParagraph(settings.AddressLine2));
        body.AppendChild(CreateStyledParagraph($"Telefon: {settings.Phone}"));
        body.AppendChild(CreateStyledParagraph($"Email: {settings.Email}"));

        // Empty line
        body.AppendChild(CreateSimpleParagraph(""));

        // Client info
        var clientPara = body.AppendChild(new Paragraph());
        var clientRun = clientPara.AppendChild(new Run());
        clientRun.AppendChild(new RunProperties(
            new Bold(),
            new FontSize() { Val = "32" }, // 16pt = 32 half-points
            new RunFonts() { Ascii = "Trebuchet MS", HighAnsi = "Trebuchet MS" },
            new Color() { Val = "FF4B4B" }
        ));
        clientRun.AppendChild(new Text(settings.ClientName));

        body.AppendChild(CreateStyledParagraph($"ID broj: {settings.ClientId}"));
        body.AppendChild(CreateStyledParagraph("PDV broj: 200236190009"));
        body.AppendChild(CreateStyledParagraph("Grbavička 4"));
        body.AppendChild(CreateStyledParagraph("71000 Sarajevo"));

        // Empty line
        body.AppendChild(CreateSimpleParagraph(""));

        // Document title - centered and styled
        var titlePara = body.AppendChild(new Paragraph());
        var titleProp = new ParagraphProperties(new Justification() { Val = JustificationValues.Center });
        titlePara.AppendChild(titleProp);
        var titleRun = titlePara.AppendChild(new Run());
        titleRun.AppendChild(new RunProperties(
            new Bold(),
            new FontSize() { Val = "36" }, // 18pt = 36 half-points
            new RunFonts() { Ascii = "Trebuchet MS", HighAnsi = "Trebuchet MS" },
            new Color() { Val = "FF4B4B" }
        ));
        titleRun.AppendChild(new Text(settings.DocumentTitle));

        // Contract info - centered and styled
        var contractPara = body.AppendChild(new Paragraph());
        var contractProp = new ParagraphProperties(new Justification() { Val = JustificationValues.Center });
        contractPara.AppendChild(contractProp);
        var contractRun = contractPara.AppendChild(new Run());
        contractRun.AppendChild(new RunProperties(
            new Bold(),
            new FontSize() { Val = "22" }, // 11pt = 22 half-points
            new RunFonts() { Ascii = "Trebuchet MS", HighAnsi = "Trebuchet MS" }
        ));
        contractRun.AppendChild(new Text($"Ugovor o pružanju usluga broj: {contractNumber}"));

        // Period info - styled
        var periodPara = body.AppendChild(new Paragraph());
        var periodRun = periodPara.AppendChild(new Run());
        periodRun.AppendChild(new RunProperties(
            new Bold(),
            new FontSize() { Val = "22" }, // 11pt = 22 half-points
            new RunFonts() { Ascii = "Trebuchet MS", HighAnsi = "Trebuchet MS" }
        ));
        periodRun.AppendChild(new Text($"Izvještajni period: {period}"));

        // Empty line
        body.AppendChild(CreateSimpleParagraph(""));

        // Table
        var table = new Table();

        // Table properties
        var tableProps = new TableProperties(
            new TableBorders(
                new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                new InsideHorizontalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                new InsideVerticalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 }
            ),
            new TableWidth() { Width = "0", Type = TableWidthUnitValues.Auto }
        );
        table.AppendChild(tableProps);

        // Header row
        var headerRow = new TableRow();
        headerRow.AppendChild(CreateTableCell("Red\nbr", true));
        headerRow.AppendChild(CreateTableCell("Kratak opis", true));
        headerRow.AppendChild(CreateTableCell("Datum", true));
        headerRow.AppendChild(CreateTableCell("Ukupan broj sati", true));
        table.AppendChild(headerRow);

        // Data rows
        var index = 1;
        foreach (var task in tasks)
        {
            var dataRow = new TableRow();
            dataRow.AppendChild(CreateTableCell(index++.ToString()));
            dataRow.AppendChild(CreateTableCell($"{task.Description}"));
            dataRow.AppendChild(CreateTableCell($"{task.Date:d. M. yyyy}."));
            dataRow.AppendChild(CreateTableCell(task.Hours.ToString()));
            table.AppendChild(dataRow);
        }

        // Total row
        var totalRow = new TableRow();
        totalRow.AppendChild(CreateTableCell(""));
        totalRow.AppendChild(CreateTableCell(""));
        totalRow.AppendChild(CreateTableCell("Ukupan broj sati", true));
        totalRow.AppendChild(CreateTableCell(total.ToString(), true));
        table.AppendChild(totalRow);

        body.AppendChild(table);

        if (settings.IncludeSignatures)
        {
            // Additional notes
            body.AppendChild(CreateSimpleParagraph(""));
            body.AppendChild(CreateSimpleParagraph(""));
            var notesPara = body.AppendChild(new Paragraph());
            var notesRun = notesPara.AppendChild(new Run());
            notesRun.AppendChild(new RunProperties(new Bold()));
            notesRun.AppendChild(new Text("Dodatne napomene:"));

            body.AppendChild(CreateSimpleParagraph("_".PadRight(70, '_')));
            body.AppendChild(CreateSimpleParagraph("_".PadRight(70, '_')));

            // Date line
            body.AppendChild(CreateSimpleParagraph(""));
            body.AppendChild(CreateSimpleParagraph($"Dana: {"_".PadRight(15, '_')}, godine"));

            // Signatures
            body.AppendChild(CreateSimpleParagraph(""));
            body.AppendChild(CreateSimpleParagraph(""));

            var sigPara = body.AppendChild(new Paragraph());
            sigPara.AppendChild(new Run(new Text("Izvršilac")));
            sigPara.AppendChild(new Run(new TabChar()));
            sigPara.AppendChild(new Run(new TabChar()));
            sigPara.AppendChild(new Run(new TabChar()));
            sigPara.AppendChild(new Run(new Text("Za Naručioca")));

            body.AppendChild(CreateSimpleParagraph(""));
            body.AppendChild(CreateSimpleParagraph(""));
            body.AppendChild(CreateSimpleParagraph(""));

            var linePara = body.AppendChild(new Paragraph());
            linePara.AppendChild(new Run(new Text("_".PadRight(25, '_'))));
            linePara.AppendChild(new Run(new TabChar()));
            linePara.AppendChild(new Run(new TabChar()));
            linePara.AppendChild(new Run(new Text("_".PadRight(25, '_'))));
        }

        mainPart.Document.Save();
    }

    private static Paragraph CreateSimpleParagraph(string text)
    {
        var paragraph = new Paragraph();
        var run = paragraph.AppendChild(new Run());
        run.AppendChild(new Text(text));
        return paragraph;
    }

    private static Paragraph CreateStyledParagraph(string text)
    {
        var paragraph = new Paragraph();
        var run = paragraph.AppendChild(new Run());
        run.AppendChild(new RunProperties(
            new FontSize() { Val = "22" }, // 11pt = 22 half-points
            new RunFonts() { Ascii = "Trebuchet MS", HighAnsi = "Trebuchet MS" }
        ));
        run.AppendChild(new Text(text));
        return paragraph;
    }

    private static TableCell CreateTableCell(string text, bool bold = false)
    {
        var cell = new TableCell();
        var paragraph = new Paragraph();
        var run = new Run();

        var runProperties = new RunProperties(
            new FontSize() { Val = "22" }, // 11pt = 22 half-points
            new RunFonts() { Ascii = "Trebuchet MS", HighAnsi = "Trebuchet MS" }
        );

        if (bold)
        {
            runProperties.AppendChild(new Bold());
        }

        run.AppendChild(runProperties);

        if (text.Contains('\n'))
        {
            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                {
                    run.AppendChild(new Break());
                }
                run.AppendChild(new Text(lines[i]));
            }
        }
        else
        {
            run.AppendChild(new Text(text));
        }

        paragraph.AppendChild(run);
        cell.AppendChild(paragraph);

        // Cell properties for borders
        var cellProps = new TableCellProperties(
            new TableCellBorders(
                new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 }
            )
        );
        cell.AppendChild(cellProps);

        return cell;
    }
}