using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Core.Interfaces
{
    /// <summary>
    /// Interface for data export operations
    /// </summary>
    public interface IExportService
    {
        /// <summary>
        /// Exports tasks to Excel format
        /// </summary>
        Task<string> ExportTasksToExcelAsync(List<DailyTask> tasks, string? filePath = null);

        /// <summary>
        /// Exports tasks to PDF format
        /// </summary>
        Task<string> ExportTasksToPdfAsync(List<DailyTask> tasks, PdfSettings settings, string? filePath = null);

        /// <summary>
        /// Exports tasks to PDF format with default settings
        /// </summary>
        Task<string> ExportTasksToPdfAsync(List<DailyTask> tasks, string? filePath = null);

        /// <summary>
        /// Exports tasks to CSV format
        /// </summary>
        Task<string> ExportTasksToCsvAsync(List<DailyTask> tasks, string? filePath = null);

        /// <summary>
        /// Gets available export formats
        /// </summary>
        List<string> GetAvailableFormats();

        /// <summary>
        /// Validates export parameters
        /// </summary>
        bool ValidateExportParameters(List<DailyTask> tasks, string format);
    }
}