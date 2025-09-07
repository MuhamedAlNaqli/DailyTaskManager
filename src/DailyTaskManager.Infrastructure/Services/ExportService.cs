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
                $"DailyTasks_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            _logger.LogInformation("Exporting {Count} tasks to PDF: {FilePath}", tasks.Count, filePath);

            // TODO: Implement PDF export using QuestPDF
            // This is a placeholder implementation
            await Task.Delay(100);

            _logger.LogWarning("PDF export not yet implemented");
            throw new NotImplementedException("PDF export functionality is not yet implemented");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting tasks to PDF");
            throw;
        }
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
}