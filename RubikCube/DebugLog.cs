using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TGL;

/// <summary>
/// Application type for log file naming.
/// </summary>
public enum AppType
{
    WinForms,
    Maui
}

/// <summary>
/// Simple debug logger that writes to both console and a log file.
/// </summary>
public static class DebugLog
{
    private static readonly object _lock = new();
    private static string? _logPath;
    private static AppType _appType = AppType.WinForms;

    /// <summary>
    /// Gets or sets the application type (WinForms or Maui).
    /// </summary>
    public static AppType ApplicationType
    {
        get => _appType;
        set
        {
            _appType = value;
            _logPath = null; // Reset path to regenerate with new app type
        }
    }

    /// <summary>
    /// Gets the current OS name for log file naming.
    /// </summary>
    private static string OsName
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "windows";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "mac";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "linux";
            return "unknown";
        }
    }

    /// <summary>
    /// Gets or sets the log file path. If not set, uses a default path based on OS and app type.
    /// </summary>
    public static string LogPath
    {
        get
        {
            if (_logPath == null)
            {
                // Build filename: rubik-{apptype}-{os}.log
                var appName = _appType == AppType.Maui ? "maui" : "winforms";
                var fileName = $"rubik-{appName}-{OsName}.log";

                // Default to user's home directory
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                _logPath = Path.Combine(home, fileName);
            }
            return _logPath;
        }
        set => _logPath = value;
    }

    /// <summary>
    /// Writes a message to the log with timestamp.
    /// </summary>
    public static void WriteLine(string message)
    {
        try
        {
            var line = $"{DateTime.Now:HH:mm:ss.fff} {message}";
            lock (_lock)
            {
                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
            System.Diagnostics.Debug.WriteLine(line);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Log error: {ex.Message}");
        }
    }

    /// <summary>
    /// Writes an exception to the log with full details.
    /// </summary>
    public static void WriteException(string context, Exception ex)
    {
        WriteLine($"{context}: {ex.GetType().Name}: {ex.Message}");
        WriteLine($"Stack trace: {ex.StackTrace}");
        if (ex.InnerException != null)
        {
            WriteLine($"Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            WriteLine($"Inner stack trace: {ex.InnerException.StackTrace}");
        }
    }

    /// <summary>
    /// Clears the log file.
    /// </summary>
    public static void Clear()
    {
        try
        {
            lock (_lock)
            {
                if (File.Exists(LogPath))
                {
                    File.Delete(LogPath);
                }
            }
        }
        catch { }
    }
}
