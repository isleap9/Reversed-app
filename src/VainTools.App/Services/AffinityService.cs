using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace VainTools.App.Services;

/// <summary>
/// Implements <see cref="IAffinityService"/> using P/Invoke to call
/// <c>GetProcessAffinityMask</c> and <c>SetProcessAffinityMask</c> from kernel32.
/// </summary>
public sealed partial class AffinityService : IAffinityService
{
    private readonly ILogger<AffinityService> _logger;

    public AffinityService(ILogger<AffinityService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<ProcessInfo> GetProcesses()
    {
        var processes = Process.GetProcesses();
        var result = new List<ProcessInfo>(processes.Length);

        foreach (var process in processes)
        {
            try
            {
                var cpuCount = GetAffinityMask(process.Id) is var mask
                    ? CountBits(mask)
                    : 0;

                result.Add(new ProcessInfo(process.Id, process.ProcessName, cpuCount));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to get affinity for process {ProcessId} ({ProcessName})",
                    process.Id, process.ProcessName);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public ulong GetAffinityMask(int processId)
    {
        var process = Process.GetProcessById(processId);
        var handle = process.Handle;

        if (!GetProcessAffinityMask(handle, out var processMask, out var systemMask))
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                $"GetProcessAffinityMask failed for PID {processId}. Win32 error: {error}");
        }

        return processMask;
    }

    /// <inheritdoc />
    public void SetAffinityMask(int processId, ulong mask)
    {
        var process = Process.GetProcessById(processId);
        var handle = process.Handle;

        if (!SetProcessAffinityMask(handle, mask))
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                $"SetProcessAffinityMask failed for PID {processId}. Win32 error: {error}");
        }

        _logger.LogInformation("Set affinity mask for PID {ProcessId} to {Mask:X}", processId, mask);
    }

    /// <inheritdoc />
    public int GetCpuCount() => Environment.ProcessorCount;

    private static int CountBits(ulong value)
    {
        var count = 0;
        while (value != 0)
        {
            count++;
            value &= value - 1;
        }
        return count;
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetProcessAffinityMask(
        IntPtr processHandle,
        out ulong processAffinityMask,
        out ulong systemAffinityMask);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetProcessAffinityMask(
        IntPtr processHandle,
        ulong processAffinityMask);
}
