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
    /// Deletes a Run key entry (plain or '-'-prefixed disabled marker).
    /// </summary>
    void DeleteRunKeyEntry(StartupEntry entry);

    /// <summary>
    /// Queries scheduled tasks that run at boot or at logon.
    /// </summary>
    Task<IReadOnlyList<StartupEntry>> GetScheduledTasksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables a scheduled task.
    /// </summary>
    Task ToggleScheduledTaskAsync(StartupEntry entry, bool isEnabled, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a scheduled task. Irreversible: the caller must confirm first.
    /// </summary>
    Task DeleteScheduledTaskAsync(StartupEntry entry, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a startup entry from a Run key or scheduled task.
/// </summary>
public sealed record StartupEntry(string Name, string Command, string Source, bool IsEnabled);
