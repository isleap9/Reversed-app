using System.IO;

namespace AkariTools.App.Helpers;

// Diagnostic startup log (mirrors the original's %TEMP%\VainToolbox-debug.txt).
// Every method is non-throwing: logging must never break the app.
internal static class LaunchLog
{
    private static readonly string Path = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(), "AkariToolbox-debug.txt");

    internal static void Write(string stage)
    {
        try
        {
            File.AppendAllText(Path,
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{Environment.ProcessId}] {stage}{Environment.NewLine}");
        }
        catch { }
    }

    internal static void WriteException(string stage, Exception ex)
    {
        try
        {
            File.AppendAllText(Path,
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{Environment.ProcessId}] {stage}: {ex.GetType().Name}: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}");
        }
        catch { }
    }
}
