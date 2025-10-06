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
                filePath = Path.Combine(downloadPath, $"RadniNalog_{DateTime.Now:MMyy}.docx");
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
        return $"RadniNalog_{DateTime.Now:MMyy}.pdf";
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
        var maxTaskDate = tasks.Any() ? tasks.Max(t => t.Date) : DateTime.Now;
        var currentMonth = _months[maxTaskDate.ToString("MMMM")];
        var currentYear = maxTaskDate.ToString("yyyy");
        var period = tasks.Any() ? $"{currentMonth} {currentYear}." : DateTime.Now.ToString("MMMM yyyy.");
        var contractNumber = $"{settings.ContractPrefix}/{DateTime.Now:yy}";

        // Company info
        var companyPara = body.AppendChild(new Paragraph());
        var companyParaProps = new ParagraphProperties(
            new SpacingBetweenLines() { Before = "0", After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto }
        );
        companyPara.AppendChild(companyParaProps);
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
        var clientParaProps = new ParagraphProperties(
            new SpacingBetweenLines() { Before = "0", After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto }
        );
        clientPara.AppendChild(clientParaProps);
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
        var titleProp = new ParagraphProperties(
            new Justification() { Val = JustificationValues.Center },
            new SpacingBetweenLines() { Before = "0", After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto }
        );
        titlePara.AppendChild(titleProp);
        var titleRun = titlePara.AppendChild(new Run());
        titleRun.AppendChild(new RunProperties(
            new Bold(),
            new FontSize() { Val = "36" }, // 18pt = 36 half-points
            new RunFonts() { Ascii = "Trebuchet MS", HighAnsi = "Trebuchet MS" },
            new Color() { Val = "FF4B4B" }
        ));
        titleRun.AppendChild(new Text(settings.DocumentTitle));

        // Contract info - centered and styled with spacing after
        var contractPara = body.AppendChild(new Paragraph());
        var contractProp = new ParagraphProperties(
            new Justification() { Val = JustificationValues.Center },
            new SpacingBetweenLines() { Before = "0", After = "300", Line = "240", LineRule = LineSpacingRuleValues.Auto }
        );
        contractPara.AppendChild(contractProp);
        var contractRun = contractPara.AppendChild(new Run());
        contractRun.AppendChild(new RunProperties(
            new Bold(),
            new FontSize() { Val = "22" }, // 11pt = 22 half-points
            new RunFonts() { Ascii = "Trebuchet MS", HighAnsi = "Trebuchet MS" }
        ));
        contractRun.AppendChild(new Text($"Ugovor o pružanju usluga broj: {contractNumber}"));

        // Period info - styled with minimal spacing after to stay close to table
        var periodPara = body.AppendChild(new Paragraph());
        var periodParaProps = new ParagraphProperties(
            new SpacingBetweenLines() { Before = "0", After = "80", Line = "240", LineRule = LineSpacingRuleValues.Auto }
        );
        periodPara.AppendChild(periodParaProps);
        var periodRun = periodPara.AppendChild(new Run());
        periodRun.AppendChild(new RunProperties(
            new Bold(),
            new FontSize() { Val = "22" }, // 11pt = 22 half-points
            new RunFonts() { Ascii = "Trebuchet MS", HighAnsi = "Trebuchet MS" }
        ));
        periodRun.AppendChild(new Text($"Izvještajni period: {period}"));

        // Table
        var table = new Table();

        // Table properties
        var tableProps = new TableProperties(
            new TableBorders(
                new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new InsideHorizontalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new InsideVerticalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 }
            ),
            new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct }
        );
        table.AppendChild(tableProps);

        // Table grid - define column widths (in twips: 1440 twips = 1 inch)
        var tableGrid = new TableGrid();
        tableGrid.AppendChild(new GridColumn() { Width = "600" });   // Red br - narrow
        tableGrid.AppendChild(new GridColumn() { Width = "5500" });  // Kratak opis - wide
        tableGrid.AppendChild(new GridColumn() { Width = "1200" });  // Datum - medium
        tableGrid.AppendChild(new GridColumn() { Width = "1200" });  // Ukupan broj sati - medium
        table.AppendChild(tableGrid);

        // Header row
        var headerRow = new TableRow();
        headerRow.AppendChild(CreateTableCell("Red\nbr", true, true, false));
        headerRow.AppendChild(CreateTableCell("Kratak opis", true, true, false));
        headerRow.AppendChild(CreateTableCell("Datum", true, true, false));
        headerRow.AppendChild(CreateTableCell("Ukupan broj sati", true, true, false));
        table.AppendChild(headerRow);

        // Data rows
        var index = 1;
        foreach (var task in tasks)
        {
            var dataRow = new TableRow();
            dataRow.AppendChild(CreateTableCell(index++.ToString(), false, true, false, false)); // Centered H & V
            dataRow.AppendChild(CreateTableCell($"{task.Description}", false, false, false, true)); // Left-aligned H, Center V
            dataRow.AppendChild(CreateTableCell($"{task.Date:d. M. yyyy}.", false, false, false, true)); // Left-aligned H, Center V
            dataRow.AppendChild(CreateTableCell(task.Hours.ToString(), false, true, false, false)); // Centered H & V
            table.AppendChild(dataRow);
        }

        // Total row
        var totalRow = new TableRow();
        totalRow.AppendChild(CreateTableCell("", false, true, false, false));
        totalRow.AppendChild(CreateTableCell("", false, false, false, false));
        totalRow.AppendChild(CreateTableCell("Ukupan broj sati", true, false, false, true)); // Bold, Left-aligned H, Center V
        totalRow.AppendChild(CreateTableCell(total.ToString(), true, true, false, false)); // Bold, Centered H & V
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

            // Signatures - using a table for proper left/right alignment
            body.AppendChild(CreateSimpleParagraph(""));
            body.AppendChild(CreateSimpleParagraph(""));

            // Create a 2-column table for signatures
            var sigTable = new Table();
            var sigTableProps = new TableProperties(
                new TableBorders(
                    new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.None) },
                    new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.None) },
                    new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.None) },
                    new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.None) },
                    new InsideHorizontalBorder { Val = new EnumValue<BorderValues>(BorderValues.None) },
                    new InsideVerticalBorder { Val = new EnumValue<BorderValues>(BorderValues.None) }
                ),
                new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct }
            );
            sigTable.AppendChild(sigTableProps);

            var sigTableGrid = new TableGrid();
            sigTableGrid.AppendChild(new GridColumn() { Width = "4250" });  // Left column
            sigTableGrid.AppendChild(new GridColumn() { Width = "4250" });  // Right column
            sigTable.AppendChild(sigTableGrid);

            // Row 1: Labels
            var labelRow = new TableRow();
            var izvrsCell = new TableCell();
            var izvrsLabelPara = new Paragraph();
            izvrsLabelPara.AppendChild(new Run(new Text("Izvršilac")));
            izvrsCell.AppendChild(izvrsLabelPara);
            izvrsCell.AppendChild(new TableCellProperties(new TableCellBorders(
                new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.None) },
                new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.None) },
                new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.None) },
                new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.None) }
            )));

            var narucCell = new TableCell();
            var narucLabelPara = new Paragraph(new ParagraphProperties(new Justification() { Val = JustificationValues.Right }));
            narucLabelPara.AppendChild(new Run(new Text("Za Naručioca")));
            narucCell.AppendChild(narucLabelPara);
            narucCell.AppendChild(new TableCellProperties(new TableCellBorders(
                new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.None) },
                new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.None) },
                new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.None) },
                new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.None) }
            )));

            labelRow.AppendChild(izvrsCell);
            labelRow.AppendChild(narucCell);
            sigTable.AppendChild(labelRow);

            // Row 2: Empty space
            var spaceRow1 = new TableRow();
            var spaceCell1L = new TableCell();
            spaceCell1L.AppendChild(new Paragraph(new Run(new Text(""))));
            spaceCell1L.AppendChild(new TableCellProperties(new TableCellBorders(
                new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.None) },
                new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.None) },
                new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.None) },
                new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.None) }
            )));
            var spaceCell1R = new TableCell();
            spaceCell1R.AppendChild(new Paragraph(new Run(new Text(""))));
            spaceCell1R.AppendChild(new TableCellProperties(new TableCellBorders(
                new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.None) },
                new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.None) },
                new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.None) },
                new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.None) }
            )));
            spaceRow1.AppendChild(spaceCell1L);
            spaceRow1.AppendChild(spaceCell1R);
            sigTable.AppendChild(spaceRow1);

            // Row 3: Signature lines
            var lineRow = new TableRow();
            var lineCell1 = new TableCell();
            var linePara1 = new Paragraph();
            linePara1.AppendChild(new Run(new Text("_".PadRight(30, '_'))));
            lineCell1.AppendChild(linePara1);
            lineCell1.AppendChild(new TableCellProperties(new TableCellBorders(
                new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.None) },
                new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.None) },
                new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.None) },
                new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.None) }
            )));

            var lineCell2 = new TableCell();
            var linePara2 = new Paragraph(new ParagraphProperties(new Justification() { Val = JustificationValues.Right }));
            linePara2.AppendChild(new Run(new Text("_".PadRight(30, '_'))));
            lineCell2.AppendChild(linePara2);
            lineCell2.AppendChild(new TableCellProperties(new TableCellBorders(
                new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.None) },
                new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.None) },
                new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.None) },
                new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.None) }
            )));

            lineRow.AppendChild(lineCell1);
            lineRow.AppendChild(lineCell2);
            sigTable.AppendChild(lineRow);

            body.AppendChild(sigTable);
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

        // Add spacing properties to remove all spacing
        var paragraphProps = new ParagraphProperties(
            new SpacingBetweenLines() { Before = "0", After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto }
        );
        paragraph.AppendChild(paragraphProps);

        var run = paragraph.AppendChild(new Run());
        run.AppendChild(new RunProperties(
            new FontSize() { Val = "22" }, // 11pt = 22 half-points
            new RunFonts() { Ascii = "Trebuchet MS", HighAnsi = "Trebuchet MS" }
        ));
        run.AppendChild(new Text(text));
        return paragraph;
    }

    private static TableCell CreateTableCell(string text, bool bold = false, bool centered = true, bool rightAligned = false, bool leftAligned = false)
    {
        var cell = new TableCell();
        var paragraph = new Paragraph();

        // Add paragraph properties for alignment and spacing
        var paragraphProps = new ParagraphProperties(
            new SpacingBetweenLines() { Before = "0", After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto }
        );

        if (rightAligned)
        {
            paragraphProps.AppendChild(new Justification() { Val = JustificationValues.Right });
        }
        else if (leftAligned)
        {
            paragraphProps.AppendChild(new Justification() { Val = JustificationValues.Left });
        }
        else if (centered)
        {
            paragraphProps.AppendChild(new Justification() { Val = JustificationValues.Center });
        }

        paragraph.AppendChild(paragraphProps);

        var run = new Run();

        var runProperties = new RunProperties(
            new FontSize() { Val = "20" }, // 10pt = 20 half-points (reduced from 22/11pt)
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

        // Cell properties for borders, minimal padding, and vertical alignment
        var cellProps = new TableCellProperties(
            new TableCellBorders(
                new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 }
            ),
            new TableCellMargin(
                new TopMargin() { Width = "20", Type = TableWidthUnitValues.Dxa },
                new BottomMargin() { Width = "20", Type = TableWidthUnitValues.Dxa },
                new LeftMargin() { Width = "40", Type = TableWidthUnitValues.Dxa },
                new RightMargin() { Width = "40", Type = TableWidthUnitValues.Dxa }
            ),
            new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Center }
        );
        cell.AppendChild(cellProps);

        return cell;
    }
}