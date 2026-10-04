namespace VainTools.App.Services;

/// <summary>
/// Provides CPU affinity management for running processes via P/Invoke.
/// </summary>
public interface IAffinityService
{
    /// <summary>
    /// Enumerates all running processes.
    /// </summary>
    IReadOnlyList<ProcessInfo> GetProcesses();

    /// <summary>
    /// Gets the current affinity mask for a process.
    /// </summary>
    ulong GetAffinityMask(int processId);

    /// <summary>
    /// Sets the affinity mask for a process.
    /// </summary>
    void SetAffinityMask(int processId, ulong mask);

    /// <summary>
    /// Gets the system-wide affinity mask (which CPUs are available at all).
    /// </summary>
    ulong GetSystemAffinityMask();

    /// <summary>
    /// Gets the total number of CPU cores available.
    /// </summary>
    int GetCpuCount();

    /// <summary>
    /// Reads all saved affinity rules.
    /// </summary>
    IReadOnlyList<AffinityRule> GetRules();

    /// <summary>
    /// Saves an affinity rule for a process name. Overwrites any existing rule.
    /// </summary>
    void SaveRule(string processName, ulong mask);

    /// <summary>
    /// Deletes the saved affinity rule for a process name.
    /// </summary>
    void DeleteRule(string processName);

    /// <summary>
    /// Reapplies all saved rules to currently running processes with matching
    /// names. Returns the number of processes updated.
    /// </summary>
    int ApplyRules();
}

/// <summary>
/// A saved CPU affinity rule: processes with this name get this mask.
/// </summary>
public sealed record AffinityRule(string ProcessName, ulong Mask);

/// <summary>
/// Represents a running process with basic information.
/// </summary>
public sealed record ProcessInfo(int Id, string Name, int CpuCount);
