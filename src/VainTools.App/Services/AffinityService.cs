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
    /// <summary>System Idle Process.</summary>
    private const int IdleProcessId = 0;

    /// <summary>The Windows "System" process — affinity cannot be read or changed.</summary>
    private const int SystemProcessId = 4;

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
            var id = process.Id;

            // PID 0 (System Idle) and PID 4 (System) have no user-mode affinity
            // mask — OpenProcess/GetProcessAffinityMask always fail for them.
            if (id == IdleProcessId || id == SystemProcessId)
            {
                continue;
            }

            string? name = null;
            try
            {
                name = process.ProcessName;
                var cpuCount = CountBits(GetAffinityMask(id));
                result.Add(new ProcessInfo(id, name, cpuCount));
            }
            catch (Exception ex)
            {
                // Protected processes reject the name or the affinity query.
                // Skip them rather than failing the whole enumeration.
                _logger.LogDebug(ex, "Skipped process {ProcessId} ({ProcessName})", id, name ?? "<unknown>");
            }
            finally
            {
                process.Dispose();
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
    public ulong GetSystemAffinityMask()
    {
        // The system mask is identical for every process, so read it from our own.
        using var process = Process.GetCurrentProcess();

        if (!GetProcessAffinityMask(process.Handle, out _, out var systemMask))
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                $"GetProcessAffinityMask failed for the system mask. Win32 error: {error}");
        }

        return systemMask;
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
