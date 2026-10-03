namespace VainTools.App.Services;

/// <summary>
/// Provides startup entry management from registry Run keys and WMI scheduled tasks.
/// </summary>
public interface IStartupService
{
    /// <summary>
    /// Reads Run key entries from HKCU and HKLM.
    /// </summary>
    IReadOnlyList<StartupEntry> GetRunKeyEntries();

    /// <summary>
    /// Enables or disables a Run key entry by renaming with a '-' prefix.
    /// </summary>
    void ToggleRunKeyEntry(StartupEntry entry, bool isEnabled);

    /// <summary>
    /// Queries scheduled tasks that run at startup via WMI.
    /// </summary>
    IReadOnlyList<StartupEntry> GetScheduledTasks();

    /// <summary>
    /// Enables or disables a scheduled task via WMI.
    /// </summary>
    void ToggleScheduledTask(StartupEntry entry, bool isEnabled);
}

/// <summary>
/// Represents a startup entry from a Run key or scheduled task.
/// </summary>
public sealed record StartupEntry(string Name, string Command, string Source, bool IsEnabled);
