using DailyTaskManager.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
// using iTextSharp.text;
// using iTextSharp.text.pdf;

namespace DailyTaskManager.Core.Services
{
    public class ExportService : IExportService
    {
        public async Task ExportAsync(IEnumerable<DailyTask> tasks, ExportFormat format)
        {
            switch (format)
            {
                case ExportFormat.Excel:
                    await ExportToExcelAsync(tasks);
                    break;
                case ExportFormat.Pdf:
                    await ExportToPdfAsync(tasks);
                    break;
                default:
                    throw new ArgumentException($"Unsupported export format: {format}");
            }
        }

        public async Task ExportToExcelAsync(IEnumerable<DailyTask> tasks)
        {
            await Task.Run(() =>
            {
                var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Pregled radnih sati");

                // Header styling
                var headerRange = ws.Range(1, 1, 1, 4);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4CAF50");
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Headers
                ws.Cell(1, 1).Value = "Red br";
                ws.Cell(1, 2).Value = "Kratak opis aktivnosti";
                ws.Cell(1, 3).Value = "Datum";
                ws.Cell(1, 4).Value = "Broj sati";

                int row = 2;
                int total = 0;
                int index = 1;

                // Sort by date
                var sortedTasks = tasks.OrderBy(t => t.Date).ToList();

                foreach (var task in sortedTasks)
                {
                    ws.Cell(row, 1).Value = index++;
                    ws.Cell(row, 2).Value = task.Description;
                    ws.Cell(row, 3).Value = task.Date.ToString("d. M. yyyy.");
                    ws.Cell(row, 4).Value = task.Hours;
                    total += task.Hours;
                    row++;
                }

                // Total row
                var totalRange = ws.Range(row, 3, row, 4);
                totalRange.Style.Font.Bold = true;
                totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F5E8");
                
                ws.Cell(row, 3).Value = "Ukupno sati";
                ws.Cell(row, 4).Value = total;

                // Auto-fit columns
                ws.ColumnsUsed().AdjustToContents();

                // Borders
                var dataRange = ws.Range(1, 1, row, 4);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), GetDefaultFileName(ExportFormat.Excel));
                wb.SaveAs(path);
            });
        }

        public async Task ExportToPdfAsync(IEnumerable<DailyTask> tasks)
        {
            await Task.Run(() =>
            {
                // Get PDF settings from database
                var pdfSettings = GetPdfSettingsFromDatabase();
                
                // Simple HTML to PDF approach for now
                var sortedTasks = tasks.OrderBy(t => t.Date).ToList();
                var html = GenerateHtmlReport(sortedTasks, pdfSettings);
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), GetDefaultFileName(ExportFormat.Pdf).Replace(".pdf", ".html"));
                
                File.WriteAllText(path, html);
                
                // TODO: For production, implement proper PDF conversion using a library like PdfSharp or DinkToPdf
                // For now, save as HTML file that can be converted to PDF by the user
            });
        }

        private Models.PdfSettings GetPdfSettingsFromDatabase()
        {
            try
            {
                var settingsService = new SettingsService();
                var dbSettings = settingsService.GetAllAsync().Result;
                
                var pdfSettings = new Models.PdfSettings();
                
                foreach (var setting in dbSettings)
                {
                    switch (setting.Key)
                    {
                        case "PDF_CompanyName":
                            pdfSettings.CompanyName = setting.Value;
                            break;
                        case "PDF_CompanyId":
                            pdfSettings.CompanyId = setting.Value;
                            break;
                        case "PDF_AddressLine1":
                            pdfSettings.AddressLine1 = setting.Value;
                            break;
                        case "PDF_AddressLine2":
                            pdfSettings.AddressLine2 = setting.Value;
                            break;
                        case "PDF_Phone":
                            pdfSettings.Phone = setting.Value;
                            break;
                        case "PDF_Email":
                            pdfSettings.Email = setting.Value;
                            break;
                        case "PDF_ClientName":
                            pdfSettings.ClientName = setting.Value;
                            break;
                        case "PDF_ClientId":
                            pdfSettings.ClientId = setting.Value;
                            break;
                        case "PDF_DocumentTitle":
                            pdfSettings.DocumentTitle = setting.Value;
                            break;
                        case "PDF_ContractPrefix":
                            pdfSettings.ContractPrefix = setting.Value;
                            break;
                        case "PDF_ShowLogoBorder":
                            if (bool.TryParse(setting.Value, out bool showBorder))
                                pdfSettings.ShowLogoBorder = showBorder;
                            break;
                        case "PDF_IncludeSignatures":
                            if (bool.TryParse(setting.Value, out bool includeSignatures))
                                pdfSettings.IncludeSignatures = includeSignatures;
                            break;
                    }
                }
                
                return pdfSettings;
            }
            catch
            {
                // Return default settings if database is not available
                return new Models.PdfSettings();
            }
        }

        private string GenerateHtmlReport(List<DailyTask> tasks, Models.PdfSettings settings = null)
        {
            // Use default settings if none provided
            settings = settings ?? new Models.PdfSettings();
            
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
                html += "\n            <div class='blue-text' style='margin-top: 20px;'>OBR &quot;SMART POINT&quot;</div>";
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

        public string GetDefaultFileName(ExportFormat format)
        {
            var timestamp = DateTime.Now.ToString("MMyy");
            var baseName = $"Pregled utrosenih sati - MA - {timestamp}";
            
            switch (format)
            {
                case ExportFormat.Excel:
                    return $"{baseName}.xlsx";
                case ExportFormat.Pdf:
                    return $"{baseName}.pdf";
                default:
                    throw new ArgumentException($"Unsupported export format: {format}");
            }
        }
    }
}