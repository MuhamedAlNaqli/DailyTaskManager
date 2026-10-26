using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using DailyTaskManager.Core.Interfaces;

namespace DailyTaskManager.Infrastructure.Services;

/// <summary>
/// Stores the "remember me" session in the user's local application data folder.
/// On Windows the file is encrypted with DPAPI so it can only be read by the same Windows user.
/// </summary>
public class SessionStore : ISessionStore
{
    private readonly ILogger<SessionStore> _logger;
    private readonly string _filePath;

    public SessionStore(ILogger<SessionStore> logger)
    {
        _logger = logger;
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DailyTaskManager");
        _filePath = Path.Combine(folder, "session.dat");
    }

    public RememberedSession? Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return null;

            var bytes = File.ReadAllBytes(_filePath);
            if (OperatingSystem.IsWindows())
            {
                bytes = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            }

            return JsonConvert.DeserializeObject<RememberedSession>(Encoding.UTF8.GetString(bytes));
        }
        catch (Exception ex)
        {
            // Unreadable or tampered file: treat as no session
            _logger.LogWarning(ex, "Could not read the saved session, ignoring it");
            Clear();
            return null;
        }
    }

    public void Save(RememberedSession session)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(session));
            if (OperatingSystem.IsWindows())
            {
                bytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            }

            File.WriteAllBytes(_filePath, bytes);

            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(_filePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not save the session");
        }
    }

    public void Clear()
    {
        try
        {
            if (File.Exists(_filePath)) File.Delete(_filePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete the saved session");
        }
    }
}
