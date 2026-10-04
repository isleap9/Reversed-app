using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

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

    /// <summary>Default HKCU location for saved affinity rules.</summary>
    private const string DefaultRulesKeyPath = @"SOFTWARE\VainTools\AffinityRules";

    private readonly ILogger<AffinityService> _logger;
    private readonly string _rulesKeyPath;

    public AffinityService(ILogger<AffinityService> logger, string? rulesKeyPath = null)
    {
        _logger = logger;
        _rulesKeyPath = string.IsNullOrWhiteSpace(rulesKeyPath) ? DefaultRulesKeyPath : rulesKeyPath;
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

    /// <inheritdoc />
    public IReadOnlyList<AffinityRule> GetRules()
    {
        var rules = new List<AffinityRule>();

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(_rulesKeyPath);
            if (key is null)
            {
                return rules;
            }

            foreach (var valueName in key.GetValueNames())
            {
                if (string.IsNullOrWhiteSpace(valueName))
                {
                    continue;
                }

                try
                {
                    rules.Add(new AffinityRule(valueName, ReadMask(key, valueName)));
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Skipped unreadable affinity rule {Rule}", valueName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read affinity rules from {Path}", _rulesKeyPath);
        }

        return rules;
    }

    /// <inheritdoc />
    public void SaveRule(string processName, ulong mask)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            throw new ArgumentException("Process name must not be empty.", nameof(processName));
        }

        if (mask == 0)
        {
            throw new ArgumentException("Affinity mask must select at least one CPU.", nameof(mask));
        }

        using var key = Registry.CurrentUser.CreateSubKey(_rulesKeyPath)
            ?? throw new InvalidOperationException($"Cannot open {_rulesKeyPath} for writing.");

        // Registry QWord is signed; round-trip the bits without loss.
        key.SetValue(processName, unchecked((long)mask), RegistryValueKind.QWord);

        _logger.LogInformation("Saved affinity rule for {ProcessName}: {Mask:X}", processName, mask);
    }

    /// <inheritdoc />
    public void DeleteRule(string processName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(_rulesKeyPath, writable: true)
            ?? throw new InvalidOperationException($"Affinity rule \"{processName}\" no longer exists.");

        if (key.GetValue(processName) is null)
        {
            throw new InvalidOperationException($"Affinity rule \"{processName}\" no longer exists.");
        }

        key.DeleteValue(processName, throwOnMissingValue: false);

        _logger.LogInformation("Deleted affinity rule for {ProcessName}", processName);
    }

    /// <inheritdoc />
    public int ApplyRules()
    {
        var rules = GetRules();
        if (rules.Count == 0)
        {
            return 0;
        }

        var applied = 0;

        foreach (var rule in rules)
        {
            using var process = FindProcess(rule.ProcessName);
            if (process is null)
            {
                continue;
            }

            try
            {
                SetAffinityMask(process.Id, rule.Mask);
                applied++;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not apply affinity rule to {ProcessName}", rule.ProcessName);
            }
        }

        return applied;
    }

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

    /// <summary>Reads a rule mask back, tolerating the shapes a QWord can unbox as.</summary>
    private static ulong ReadMask(RegistryKey key, string valueName)
    {
        var raw = key.GetValue(valueName)
            ?? throw new InvalidOperationException($"Affinity rule \"{valueName}\" has no value.");

        return raw switch
        {
            long qword => unchecked((ulong)qword),
            int dword => unchecked((ulong)(uint)dword),
            string text when ulong.TryParse(text, out var parsed) => parsed,
            _ => throw new InvalidOperationException(
                $"Affinity rule \"{valueName}\" is not a numeric mask."),
        };
    }

    /// <summary>Finds the first running process with this name. Caller disposes.</summary>
    private static Process? FindProcess(string processName)
    {
        var wanted = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? processName[..^4]
            : processName;

        foreach (var process in Process.GetProcessesByName(wanted))
        {
            if (process.Id is IdleProcessId or SystemProcessId)
            {
                process.Dispose();
                continue;
            }

            return process;
        }

        return null;
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
