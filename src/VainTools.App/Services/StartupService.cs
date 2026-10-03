using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Management;

namespace VainTools.App.Services;

/// <summary>
/// Implements <see cref="IStartupService"/> using registry Run keys and WMI scheduled tasks.
/// </summary>
public sealed partial class StartupService : IStartupService
{
    private readonly ILogger<StartupService> _logger;

    private static readonly string[] RunKeyPaths =
    [
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
    ];

    public StartupService(ILogger<StartupService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<StartupEntry> GetRunKeyEntries()
    {
        var entries = new List<StartupEntry>();

        // HKCU Run key
        using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPaths[0]))
        {
            if (key != null)
            {
                foreach (var valueName in key.GetValueNames())
                {
                    if (string.IsNullOrWhiteSpace(valueName))
                        continue;
                    var command = key.GetValue(valueName)?.ToString() ?? string.Empty;
                    entries.Add(new StartupEntry(valueName, command, "HKCU", IsEnabled(valueName)));
                }
            }
        }

        // HKLM Run key
        using (var key = Registry.LocalMachine.OpenSubKey(RunKeyPaths[0]))
        {
            if (key != null)
            {
                foreach (var valueName in key.GetValueNames())
                {
                    if (string.IsNullOrWhiteSpace(valueName))
                        continue;
                    var command = key.GetValue(valueName)?.ToString() ?? string.Empty;
                    entries.Add(new StartupEntry(valueName, command, "HKLM", IsEnabled(valueName)));
                }
            }
        }

        return entries;
    }

    /// <inheritdoc />
    public void ToggleRunKeyEntry(StartupEntry entry, bool isEnabled)
    {
        var hive = entry.Source == "HKCU" ? Registry.CurrentUser : Registry.LocalMachine;
        using var key = hive.OpenSubKey(RunKeyPaths[0], writable: true);
        if (key == null)
        {
            throw new InvalidOperationException($"Cannot open {entry.Source} Run key for writing.");
        }

        var currentName = entry.Name;
        var isEnabledCurrently = IsEnabled(currentName);

        if (isEnabled == isEnabledCurrently)
        {
            return; // No change needed
        }

        var command = key.GetValue(currentName)?.ToString() ?? string.Empty;

        if (isEnabled)
        {
            // Remove the '-' prefix to enable
            var enabledName = currentName.TrimStart('-');
            key.DeleteValue(currentName, throwOnMissingValue: false);
            key.SetValue(enabledName, command);
            _logger.LogInformation("Enabled Run key entry: {Name}", enabledName);
        }
        else
        {
            // Add the '-' prefix to disable
            var disabledName = "-" + currentName;
            key.DeleteValue(currentName, throwOnMissingValue: false);
            key.SetValue(disabledName, command);
            _logger.LogInformation("Disabled Run key entry: {Name}", disabledName);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<StartupEntry> GetScheduledTasks()
    {
        var entries = new List<StartupEntry>();

        try
        {
            // Query tasks that have a startup trigger
            const string query = "SELECT Name, Path FROM Win32_StartupCommand";
            using var searcher = new ManagementObjectSearcher(query);
            using var results = searcher.Get();

            foreach (ManagementObject task in results)
            {
                var name = task["Name"]?.ToString() ?? "Unknown";
                var path = task["Path"]?.ToString() ?? string.Empty;
                entries.Add(new StartupEntry(name, path, "Scheduled Task", IsTaskEnabled(name)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query scheduled tasks via WMI");
        }

        return entries;
    }

    /// <inheritdoc />
    public void ToggleScheduledTask(StartupEntry entry, bool isEnabled)
    {
        try
        {
            const string query = "SELECT * FROM Win32_StartupCommand";
            using var searcher = new ManagementObjectSearcher(query);
            using var results = searcher.Get();

            foreach (ManagementObject task in results)
            {
                var name = task["Name"]?.ToString();
                if (name == entry.Name)
                {
                    // Win32_StartupCommand doesn't have Enable/Disable methods
                    // We need to use the Task Scheduler COM API or schtasks.exe
                    // For now, log that this requires elevation
                    _logger.LogWarning(
                        "Toggling scheduled task '{Name}' requires Task Scheduler API or schtasks.exe",
                        entry.Name);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to toggle scheduled task '{Name}'", entry.Name);
        }
    }

    private static bool IsEnabled(string valueName) => !valueName.StartsWith('-');

    private static bool IsTaskEnabled(string taskName)
    {
        try
        {
            const string query = "SELECT * FROM Win32_StartupCommand";
            using var searcher = new ManagementObjectSearcher(query);
            using var results = searcher.Get();

            foreach (ManagementObject task in results)
            {
                var name = task["Name"]?.ToString();
                if (name == taskName)
                {
                    // Win32_StartupCommand doesn't expose enabled state directly
                    // Assume enabled if found
                    return true;
                }
            }
        }
        catch
        {
            // Ignore
        }

        return false;
    }
}
