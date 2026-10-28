using System.Threading.Tasks;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Core.Interfaces
{
    /// <summary>
    /// Backup of the user's data and import of entries from files
    /// </summary>
    public interface IBackupService
    {
        /// <summary>
        /// Writes the user's entries and templates to a JSON backup file and returns the path
        /// </summary>
        Task<string> CreateBackupAsync(string filePath);

        /// <summary>
        /// Reads a backup (.json), CSV (.csv) or Excel (.xlsx) file and reports what it would add
        /// </summary>
        Task<ImportPreview> ReadImportFileAsync(string filePath);

        /// <summary>
        /// Adds the previewed entries and templates; entries that already exist are skipped
        /// </summary>
        Task<ImportResult> ImportAsync(ImportPreview preview);
    }
}
