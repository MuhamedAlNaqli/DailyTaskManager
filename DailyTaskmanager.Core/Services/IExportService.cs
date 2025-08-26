using DailyTaskManager.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DailyTaskManager.Core.Services
{
    public enum ExportFormat
    {
        Excel,
        Pdf
    }

    public interface IExportService
    {
        Task ExportToExcelAsync(IEnumerable<DailyTask> tasks);
        Task ExportToPdfAsync(IEnumerable<DailyTask> tasks);
        Task ExportAsync(IEnumerable<DailyTask> tasks, ExportFormat format);
        string GetDefaultFileName(ExportFormat format);
    }
}