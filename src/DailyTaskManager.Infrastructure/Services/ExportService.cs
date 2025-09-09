using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ClosedXML.Excel;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Infrastructure.Services;

/// <summary>
/// Service for data export operations
/// </summary>
public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;

    public ExportService(ILogger<ExportService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExportTasksToExcelAsync(List<DailyTask> tasks, string? filePath = null)
    {
        try
        {
            filePath ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 
                $"DailyTasks_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

            _logger.LogInformation("Exporting {Count} tasks to Excel: {FilePath}", tasks.Count, filePath);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Daily Tasks");

            // Headers
            worksheet.Cell(1, 1).Value = "Date";
            worksheet.Cell(1, 2).Value = "Description";
            worksheet.Cell(1, 3).Value = "Hours";
            worksheet.Cell(1, 4).Value = "Created";
            worksheet.Cell(1, 5).Value = "Updated";

            // Style headers
            var headerRange = worksheet.Range(1, 1, 1, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // Data rows
            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                var row = i + 2;

                worksheet.Cell(row, 1).Value = task.Date.ToString("yyyy-MM-dd");
                worksheet.Cell(row, 2).Value = task.Description;
                worksheet.Cell(row, 3).Value = (double)task.Hours;
                worksheet.Cell(row, 4).Value = task.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell(row, 5).Value = task.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Add totals
            if (tasks.Count > 0)
            {
                var totalRow = tasks.Count + 3;
                worksheet.Cell(totalRow, 2).Value = "Total Hours:";
                worksheet.Cell(totalRow, 3).Value = (double)tasks.Sum(t => t.Hours);
                worksheet.Cell(totalRow, 2).Style.Font.Bold = true;
                worksheet.Cell(totalRow, 3).Style.Font.Bold = true;
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
            filePath ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 
                GetDefaultPdfFileName());

            _logger.LogInformation("Exporting {Count} tasks to PDF: {FilePath}", tasks.Count, filePath);

            await Task.Run(() =>
            {
                // Sort tasks by date
                var sortedTasks = tasks.OrderBy(t => t.Date).ToList();
                
                // Generate HTML report (same as old WPF version)
                var html = GenerateHtmlReport(sortedTasks, settings);
                
                // For now, save as HTML file that can be converted to PDF by the user
                // In the future, this could be enhanced with a proper PDF conversion library
                var htmlPath = filePath.Replace(".pdf", ".html");
                File.WriteAllText(htmlPath, html);
                
                _logger.LogInformation("HTML report generated: {HtmlPath}", htmlPath);
                
                // Return the HTML path since we're generating HTML for now
                filePath = htmlPath;
            });

            _logger.LogInformation("Successfully exported tasks to PDF (HTML): {FilePath}", filePath);
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
            filePath ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 
                $"DailyTasks_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

            _logger.LogInformation("Exporting {Count} tasks to CSV: {FilePath}", tasks.Count, filePath);

            var lines = new List<string>
            {
                "Date,Description,Hours,Created,Updated"
            };

            foreach (var task in tasks)
            {
                var line = $"{task.Date:yyyy-MM-dd}," +
                          $"\"{task.Description.Replace("\"", "\"\"")}\","+ 
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

    public List<string> GetAvailableFormats()
    {
        return new List<string> { "Excel", "CSV", "PDF" };
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

    private string GenerateHtmlReport(List<DailyTask> tasks, PdfSettings settings)
    {
        var total = tasks.Sum(t => t.Hours);
        var period = tasks.Any() ? $"{tasks.Min(t => t.Date):MMMM yyyy}." : DateTime.Now.ToString("MMMM yyyy.");
        var contractNumber = $"{settings.ContractPrefix}/{DateTime.Now:yy}";
        
        var html = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>{settings.DocumentTitle}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; font-size: 12px; line-height: 1.4; }}
        .header-section {{ margin-bottom: 40px; }}
        .company-info {{ {(settings.ShowLogoBorder ? "border-left: 4px solid #333; padding-left: 15px;" : "")} margin-bottom: 30px; }}
        .company-name {{ font-weight: bold; font-size: 14px; margin-bottom: 5px; }}
        .company-details {{ font-size: 11px; color: #333; line-height: 1.3; }}
        .client-info {{ font-weight: bold; font-size: 13px; margin-bottom: 20px; }}
        .document-title {{ text-align: center; font-size: 22px; font-weight: bold; margin: 40px 0 10px 0; }}
        .contract-info {{ text-align: center; font-size: 12px; margin-bottom: 30px; }}
        .period-info {{ font-weight: bold; margin-bottom: 20px; }}
        table {{ width: 100%; border-collapse: collapse; margin: 20px 0; font-size: 11px; }}
        th {{ background-color: #f5f5f5; border: 1px solid #333; padding: 10px 8px; text-align: center; font-weight: bold; }}
        td {{ border: 1px solid #333; padding: 8px; vertical-align: top; }}
        .row-number {{ text-align: center; width: 50px; }}
        .description {{ width: 60%; }}
        .date {{ text-align: center; width: 15%; }}
        .hours {{ text-align: center; width: 15%; }}
        .total-row {{ font-weight: bold; }}
        .additional-notes {{ margin: 40px 0; }}
        .signature-line {{ border-bottom: 1px solid #333; display: inline-block; width: 300px; margin-bottom: 5px; }}
        .signature-labels {{ display: flex; justify-content: space-between; margin-top: 60px; }}
        .signature-left {{ text-align: left; }}
        .signature-right {{ text-align: right; }}
        .blue-text {{ color: #0066cc; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='header-section'>
        <div class='company-info'>
            <div class='company-name'>{System.Net.WebUtility.HtmlEncode(settings.CompanyName)}</div>
            <div class='company-details'>
                ID broj: {System.Net.WebUtility.HtmlEncode(settings.CompanyId)}<br>
                {System.Net.WebUtility.HtmlEncode(settings.AddressLine1)}<br>
                {System.Net.WebUtility.HtmlEncode(settings.AddressLine2)}<br>
                Telefon: {System.Net.WebUtility.HtmlEncode(settings.Phone)}<br>
                Email: {System.Net.WebUtility.HtmlEncode(settings.Email)}
            </div>
        </div>
        
        <div class='client-info'>
            {System.Net.WebUtility.HtmlEncode(settings.ClientName)}<br>
            ID broj: {System.Net.WebUtility.HtmlEncode(settings.ClientId)}
        </div>
    </div>

    <div class='document-title'>{System.Net.WebUtility.HtmlEncode(settings.DocumentTitle)}</div>
    <div class='contract-info'>Ugovor o pružanju usluga broj: {contractNumber}</div>
    
    <div class='period-info'>Izvještajni period: {period}</div>

    <table>
        <thead>
            <tr>
                <th class='row-number'>Red<br>br</th>
                <th class='description'>Kratak opis</th>
                <th class='date'>Datum</th>
                <th class='hours'>Ukupan broj sati</th>
            </tr>
        </thead>
        <tbody>";

        var index = 1;
        foreach (var task in tasks)
        {
            html += $@"
            <tr>
                <td class='row-number'>{index++}</td>
                <td class='description'>- Daily<br>- {System.Net.WebUtility.HtmlEncode(task.Description)}</td>
                <td class='date'>{task.Date:d. M. yyyy}.</td>
                <td class='hours'>{task.Hours}</td>
            </tr>";
        }

        html += $@"
        </tbody>
        <tfoot>
            <tr class='total-row'>
                <td colspan='3' style='text-align: right; padding-right: 20px;'><strong>Ukupan broj sati</strong></td>
                <td class='hours'><strong>{total}</strong></td>
            </tr>
        </tfoot>
    </table>";

        if (settings.IncludeSignatures)
        {
            html += "\n    <div class='additional-notes'>";
            html += "\n        <strong>Dodatne<br>napomene:</strong>";
            html += "\n        <div class='signature-line'></div>";
            html += "\n        <br>";
            html += "\n        <div class='signature-line'></div>";
            html += "\n    </div>";
            html += "\n";
            html += "\n    <div style='margin-top: 60px;'>";
            html += "\n        <strong>Dana:</strong> <span style='display: inline-block; width: 100px; border-bottom: 1px solid #333;'></span><strong>, godine</strong>";
            html += "\n    </div>";
            html += "\n";
            html += "\n    <div class='signature-labels'>";
            html += "\n        <div class='signature-left'>";
            html += "\n            <div>Izvršilac</div>";
            html += "\n            <div class='blue-text' style='margin-top: 20px;'>OBR \"SMART POINT\"</div>";
            html += "\n            <div class='blue-text'>vl. Alnaqli Muhamed</div>";
            html += "\n            <div class='blue-text'>SARAJEVO</div>";
            html += "\n            <div style='margin-top: 30px; border-bottom: 1px solid #333; width: 200px;'></div>";
            html += "\n        </div>";
            html += "\n        <div class='signature-right'>";
            html += "\n            <div>Za Naručioca</div>";
            html += "\n            <div style='margin-top: 100px; border-bottom: 1px solid #333; width: 200px; margin-left: auto;'></div>";
            html += "\n        </div>";
            html += "\n    </div>";
        }

        html += "\n</body>\n</html>";

        return html;
    }
}