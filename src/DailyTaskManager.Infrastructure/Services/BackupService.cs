using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Infrastructure.Services;

/// <summary>
/// Creates JSON backups and imports entries from backup, CSV and Excel files
/// </summary>
public class BackupService : IBackupService
{
    private static readonly string[] DateHeaders = { "date", "datum", "task_date", "day", "dan" };
    private static readonly string[] DescriptionHeaders = { "description", "opis", "kratak opis", "kratak opis aktivnosti", "aktivnost", "task", "note", "napomena" };
    private static readonly string[] HoursHeaders = { "hours", "hour", "sati", "broj sati", "ukupan broj sati", "total_hours", "h" };
    private static readonly string[] ClientHeaders = { "client", "klijent", "customer", "naručilac", "narucilac" };

    private static readonly string[] DateFormats =
    {
        "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss", "d.M.yyyy", "d.M.yyyy.", "d. M. yyyy", "d. M. yyyy.", "dd.MM.yyyy", "dd.MM.yyyy.",
        "d/M/yyyy", "dd/MM/yyyy", "M/d/yyyy", "yyyy/MM/dd", "d-M-yyyy", "dd-MM-yyyy"
    };

    private readonly IDatabaseService _databaseService;
    private readonly ILogger<BackupService> _logger;

    public BackupService(IDatabaseService databaseService, ILogger<BackupService> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    // ------------------------------------------------------------------------------------ backup

    public async Task<string> CreateBackupAsync(string filePath)
    {
        var tasks = await _databaseService.GetTasksAsync(new TaskQuery());
        var templates = await _databaseService.GetTemplatesAsync();

        var backup = new BackupFile
        {
            ExportedAt = DateTime.UtcNow,
            AppVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "",
            Tasks = tasks
                .OrderBy(t => t.Date).ThenBy(t => t.CreatedAt)
                .Select(t => new BackupTask
                {
                    Date = DateTime.SpecifyKind(t.Date.Date, DateTimeKind.Unspecified),
                    Description = t.Description,
                    Hours = t.Hours,
                    Client = t.Client?.Name,
                    CreatedAt = t.CreatedAt
                })
                .ToList(),
            Templates = templates
                .Select(t => new BackupTemplate { Name = t.Name, Description = t.Description, Hours = t.Hours, Client = t.Client?.Name })
                .ToList()
        };

        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json, new UTF8Encoding(false));

        _logger.LogInformation("Backup written: {FilePath} ({Tasks} entries, {Templates} templates)", filePath, backup.Tasks.Count, backup.Templates.Count);
        return filePath;
    }

    // ------------------------------------------------------------------------------------ import

    public async Task<ImportPreview> ReadImportFileAsync(string filePath)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException("The file does not exist.", filePath);

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var preview = extension switch
        {
            ".json" => await ReadBackupAsync(filePath),
            ".xlsx" => await Task.Run(() => ReadRows(ReadExcelRows(filePath))),
            ".csv" or ".txt" => ReadRows(ReadCsvRows(await File.ReadAllTextAsync(filePath, Encoding.UTF8))),
            _ => throw new NotSupportedException("Supported files are .json (backup), .csv and .xlsx.")
        };

        preview.SourceName = Path.GetFileName(filePath);
        await MarkDuplicatesAsync(preview);
        return preview;
    }

    public async Task<ImportResult> ImportAsync(ImportPreview preview)
    {
        var result = new ImportResult();

        // Clients named in the file (case-insensitive)
        var names = preview.Entries.Select(e => e.Client)
            .Concat(preview.Templates.Select(t => t.Client))
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existing = (await _databaseService.GetClientsAsync()).ToDictionary(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);
        var ids = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            if (existing.TryGetValue(name, out var id))
            {
                ids[name] = id;
            }
            else
            {
                ids[name] = (await _databaseService.GetOrCreateClientAsync(name)).Id;
                result.ClientsCreated++;
            }
        }

        int? ClientIdOf(string? name) => !string.IsNullOrWhiteSpace(name) && ids.TryGetValue(name!.Trim(), out var id) ? id : (int?)null;

        var tasks = preview.Entries.Select(e => new DailyTask
        {
            Date = e.Date.Date,
            Description = e.Description,
            Hours = e.Hours,
            ClientId = ClientIdOf(e.Client),
            CreatedAt = e.CreatedAt ?? DateTime.UtcNow
        }).ToList();

        result.Added = await _databaseService.ImportTasksAsync(tasks);
        result.Duplicates = tasks.Count - result.Added;

        var existingTemplates = new HashSet<string>((await _databaseService.GetTemplatesAsync()).Select(t => t.Name), StringComparer.OrdinalIgnoreCase);
        foreach (var template in preview.Templates)
        {
            if (string.IsNullOrWhiteSpace(template.Name) || existingTemplates.Contains(template.Name.Trim())) continue;

            await _databaseService.SaveTemplateAsync(new TaskTemplate
            {
                Name = template.Name,
                Description = template.Description,
                Hours = template.Hours,
                ClientId = ClientIdOf(template.Client)
            });
            result.TemplatesAdded++;
        }

        _logger.LogInformation("Import finished: {Added} added, {Duplicates} duplicates, {Clients} clients, {Templates} templates",
            result.Added, result.Duplicates, result.ClientsCreated, result.TemplatesAdded);
        return result;
    }

    private async Task MarkDuplicatesAsync(ImportPreview preview)
    {
        if (preview.Entries.Count == 0) return;

        var from = preview.From!.Value.Date;
        var to = preview.To!.Value.Date;
        var existing = await _databaseService.GetTasksAsync(new TaskQuery(null, from, to));
        var keys = new HashSet<string>(existing.Select(t => TaskKeys.For(t.Date, t.Description, t.Hours)));

        var seen = new HashSet<string>(keys);
        var duplicates = 0;
        foreach (var entry in preview.Entries)
        {
            // Counts rows that already exist as well as repeats inside the file itself
            if (!seen.Add(TaskKeys.For(entry.Date, entry.Description, entry.Hours))) duplicates++;
        }

        preview.DuplicateCount = duplicates;
    }

    // ------------------------------------------------------------------------------------ readers

    private static async Task<ImportPreview> ReadBackupAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
        BackupFile? backup;
        try
        {
            backup = JsonSerializer.Deserialize<BackupFile>(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("This is not a valid Daily Task Manager backup file.", ex);
        }

        if (backup == null || backup.Tasks == null) throw new InvalidDataException("This is not a valid Daily Task Manager backup file.");

        var preview = new ImportPreview { Templates = backup.Templates ?? new List<BackupTemplate>() };
        foreach (var task in backup.Tasks)
        {
            if (string.IsNullOrWhiteSpace(task.Description) || task.Hours <= 0 || task.Hours > 24)
            {
                preview.SkippedRows++;
                continue;
            }

            preview.Entries.Add(new ImportEntry
            {
                Date = task.Date.Date,
                Description = task.Description,
                Hours = task.Hours,
                Client = task.Client,
                CreatedAt = task.CreatedAt == default ? (DateTime?)null : DateTime.SpecifyKind(task.CreatedAt, DateTimeKind.Utc)
            });
        }

        return preview;
    }

    private static List<List<object?>> ReadExcelRows(string filePath)
    {
        // Open a copy so a workbook that is open in Excel can still be read
        var temp = Path.Combine(Path.GetTempPath(), $"dtm_import_{Guid.NewGuid():N}.xlsx");
        File.Copy(filePath, temp, true);
        try
        {
            using var workbook = new XLWorkbook(temp);
            var sheet = workbook.Worksheets.FirstOrDefault(s => s.RangeUsed() != null);
            var rows = new List<List<object?>>();
            if (sheet == null) return rows;

            var used = sheet.RangeUsed()!;
            var firstColumn = used.FirstColumn().ColumnNumber();
            var lastColumn = used.LastColumn().ColumnNumber();
            foreach (var row in used.Rows())
            {
                var values = new List<object?>();
                for (var c = firstColumn; c <= lastColumn; c++)
                {
                    var cell = row.WorksheetRow().Cell(c);
                    if (cell.IsEmpty()) values.Add(null);
                    else if (cell.DataType == XLDataType.DateTime) values.Add(cell.GetDateTime());
                    else if (cell.DataType == XLDataType.Number) values.Add(cell.GetDouble());
                    else values.Add(cell.GetString());
                }

                rows.Add(values);
            }

            return rows;
        }
        finally
        {
            try { File.Delete(temp); } catch (IOException) { }
        }
    }

    private static List<List<object?>> ReadCsvRows(string text)
    {
        if (text.Length > 0 && text[0] == '﻿') text = text.Substring(1);

        var firstLine = text.Split('\n').FirstOrDefault() ?? "";
        var delimiter = new[] { ',', ';', '\t' }.OrderByDescending(d => firstLine.Count(c => c == d)).First();

        var rows = new List<List<object?>>();
        var row = new List<object?>();
        var field = new StringBuilder();
        var quoted = false;

        void EndField() { row.Add(field.ToString()); field.Clear(); }
        void EndRow()
        {
            EndField();
            if (row.Any(v => !string.IsNullOrWhiteSpace(v as string))) rows.Add(row);
            row = new List<object?>();
        }

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                else if (c == '"') quoted = false;
                else field.Append(c);
            }
            else if (c == '"') quoted = true;
            else if (c == delimiter) EndField();
            else if (c == '\r') { }
            else if (c == '\n') EndRow();
            else field.Append(c);
        }

        if (field.Length > 0 || row.Count > 0) EndRow();
        return rows;
    }

    /// <summary>
    /// Finds the header row (or assumes Date, Description, Hours) and turns the rows into entries
    /// </summary>
    private static ImportPreview ReadRows(List<List<object?>> rows)
    {
        var preview = new ImportPreview();
        if (rows.Count == 0) return preview;

        int dateCol = -1, descriptionCol = -1, hoursCol = -1, clientCol = -1, headerRow = -1;
        for (var r = 0; r < Math.Min(10, rows.Count) && headerRow < 0; r++)
        {
            var names = rows[r].Select(v => (v as string)?.Trim().ToLowerInvariant()).ToList();
            int Find(string[] candidates) => names.FindIndex(n => n != null && candidates.Contains(n));

            var d = Find(DateHeaders);
            var h = Find(HoursHeaders);
            if (d >= 0 && h >= 0)
            {
                headerRow = r;
                dateCol = d;
                hoursCol = h;
                descriptionCol = Find(DescriptionHeaders);
                clientCol = Find(ClientHeaders);
            }
        }

        if (headerRow < 0)
        {
            // No header: Date, Description, Hours in that order
            headerRow = -1;
            dateCol = 0;
            descriptionCol = 1;
            hoursCol = 2;
        }

        if (descriptionCol < 0)
        {
            // Pick the first column that is neither the date nor the hours
            descriptionCol = Enumerable.Range(0, rows[Math.Max(headerRow, 0)].Count).FirstOrDefault(i => i != dateCol && i != hoursCol);
        }

        for (var r = headerRow + 1; r < rows.Count; r++)
        {
            var row = rows[r];
            object? At(int i) => i >= 0 && i < row.Count ? row[i] : null;

            var date = ToDate(At(dateCol));
            var description = (At(descriptionCol) as string ?? Convert.ToString(At(descriptionCol), CultureInfo.InvariantCulture))?.Trim();
            var hours = ToHours(At(hoursCol));

            if (date == null || string.IsNullOrWhiteSpace(description) || hours == null || hours <= 0 || hours > 24)
            {
                // Totals rows ("Ukupan broj sati") and blank rows are expected; only count rows that had some content
                if (row.Any(v => !string.IsNullOrWhiteSpace(Convert.ToString(v, CultureInfo.InvariantCulture)))
                    && !(date == null && string.IsNullOrWhiteSpace(description)))
                {
                    preview.SkippedRows++;
                }

                continue;
            }

            preview.Entries.Add(new ImportEntry
            {
                Date = date.Value.Date,
                Description = description!,
                Hours = Math.Round(hours.Value, 2),
                Client = (At(clientCol) as string)?.Trim()
            });
        }

        return preview;
    }

    private static DateTime? ToDate(object? value)
    {
        switch (value)
        {
            case DateTime dt:
                return dt.Date;
            case double serial when serial > 20000 && serial < 80000:
                return DateTime.FromOADate(serial).Date;
            case string text when !string.IsNullOrWhiteSpace(text):
                text = text.Trim();
                if (DateTime.TryParseExact(text, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact)) return exact.Date;
                return null;
            default:
                return null;
        }
    }

    private static decimal? ToHours(object? value)
    {
        switch (value)
        {
            case double number:
                return (decimal)number;
            case decimal dec:
                return dec;
            case string text when !string.IsNullOrWhiteSpace(text):
                text = text.Trim().Replace("h", "", StringComparison.OrdinalIgnoreCase).Replace(',', '.');
                return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
            default:
                return null;
        }
    }
}
