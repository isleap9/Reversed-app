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
    /// Gets the total number of CPU cores available.
    /// </summary>
    int GetCpuCount();
}

/// <summary>
/// Represents a running process with basic information.
/// </summary>
public sealed record ProcessInfo(int Id, string Name, int CpuCount);
