using System;
using System.IO;

namespace DailyTaskManager.Avalonia.Services;

public enum Mode
{
    /// <summary>The personal app with its own local database (as always)</summary>
    Consultant,

    /// <summary>A company employee: signs in with the Windows account and works against the company API</summary>
    Employee
}

/// <summary>
/// The mode the app starts in. It is chosen on the sign-in screen and remembered per user in a small file, like the language.
/// Consultant mode never talks to the company API, and employee mode never touches the local database.
/// </summary>
public static class AppMode
{
    private static string PreferencesPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DailyTaskManager", "mode.txt");

    private static Mode? _current;

    public static Mode Current
    {
        get
        {
            if (_current.HasValue) return _current.Value;

            var mode = Mode.Consultant;
            try
            {
                if (File.Exists(PreferencesPath) && File.ReadAllText(PreferencesPath).Trim().Equals("employee", StringComparison.OrdinalIgnoreCase))
                    mode = Mode.Employee;
            }
            catch
            {
                // Unreadable preference: consultant, the safe default
            }

            _current = mode;
            return mode;
        }
    }

    public static bool IsEmployee => Current == Mode.Employee;

    public static void Set(Mode mode)
    {
        _current = mode;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PreferencesPath)!);
            File.WriteAllText(PreferencesPath, mode == Mode.Employee ? "employee" : "consultant");
        }
        catch
        {
            // The choice still applies for this session
        }
    }
}
