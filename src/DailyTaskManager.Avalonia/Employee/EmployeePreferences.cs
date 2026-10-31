using System;
using System.IO;

namespace DailyTaskManager.Avalonia.EmployeeMode;

/// <summary>Small choices of the employee window that are remembered on this PC, like the language</summary>
public static class EmployeePreferences
{
    private static string ViewPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DailyTaskManager", "employee-view.txt");

    /// <summary>True when the employee chose the calendar instead of the list</summary>
    public static bool LoadCalendarView()
    {
        try
        {
            return File.Exists(ViewPath) && File.ReadAllText(ViewPath).Trim().Equals("calendar", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static void SaveCalendarView(bool calendar)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ViewPath)!);
            File.WriteAllText(ViewPath, calendar ? "calendar" : "list");
        }
        catch
        {
            // The choice still applies for this session
        }
    }
}
