using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace VainTools.App.Services;

/// <summary>
/// Implements <see cref="IStartupService"/> using registry Run keys and WMI scheduled tasks.
/// </summary>
public sealed partial class StartupService : IStartupService
{
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<StartupService> _logger;

    /// <summary>
    /// Run/RunOnce key locations, in the order they are displayed.
    /// A value name prefixed with '-' is a Vain Tools convention meaning "disabled".
    /// </summary>
    private static readonly (RegistryKey Hive, string Path, string Source)[] RunKeyLocations =
    [
        (Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "HKCU"),
        (Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", "HKCU"),
        (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "HKLM"),
        (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", "HKLM"),
        (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run", "HKLM"),
    ];

    public StartupService(IProcessRunner processRunner, ILogger<StartupService> logger)
    {
        _processRunner = processRunner;
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<StartupEntry> GetRunKeyEntries()
    {
        var entries = new List<StartupEntry>();

        foreach (var (hive, path, source) in RunKeyLocations)
        {
            try
            {
                using var key = hive.OpenSubKey(path);
                if (key is null)
                {
                    continue;
                }

                foreach (var valueName in key.GetValueNames())
                {
                    if (string.IsNullOrWhiteSpace(valueName))
                    {
                        continue;
                    }

                    var command = key.GetValue(valueName)?.ToString() ?? string.Empty;
                    entries.Add(new StartupEntry(
                        DisplayName(valueName),
                        command,
                        source,
                        IsEnabled(valueName)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read Run key {Source}\\{Path}", source, path);
            }
        }

        return entries;
    }

    /// <inheritdoc />
    public void ToggleRunKeyEntry(StartupEntry entry, bool isEnabled)
    {
        // Run-key entries are only ever written to the non-RunOnce Run key.
        var location = Array.Find(RunKeyLocations, l =>
            l.Source == entry.Source &&
            !l.Path.EndsWith("RunOnce", StringComparison.OrdinalIgnoreCase));

        var hive = location.Hive ?? (entry.Source == "HKCU" ? Registry.CurrentUser : Registry.LocalMachine);
        var path = location.Path ?? @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        using var key = hive.OpenSubKey(path, writable: true)
            ?? throw new InvalidOperationException(
                $"Cannot open {entry.Source}\\{path} for writing. Run Vain Tools as administrator.");

        // Look for the value under both the plain and the '-' prefixed name.
        var storedName = key.GetValue(entry.Name) is not null
            ? entry.Name
            : key.GetValue("-" + entry.Name) is not null
                ? "-" + entry.Name
                : null;

        if (storedName is null)
        {
            throw new InvalidOperationException($"Startup entry \"{entry.Name}\" no longer exists.");
        }

        var currentlyEnabled = IsEnabled(storedName);
        if (currentlyEnabled == isEnabled)
        {
            return;
        }

        var command = key.GetValue(storedName)?.ToString() ?? string.Empty;
        var newName = isEnabled ? entry.Name.TrimStart('-') : "-" + entry.Name.TrimStart('-');

        key.DeleteValue(storedName, throwOnMissingValue: false);
        key.SetValue(newName, command, RegistryValueKind.String);

        _logger.LogInformation("{Action} Run key entry {Name} in {Source}",
            isEnabled ? "Enabled" : "Disabled", entry.Name, entry.Source);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StartupEntry>> GetScheduledTasksAsync(CancellationToken cancellationToken = default)
    {
        var entries = new List<StartupEntry>();

        ProcessResult result;
        try
        {
            result = await _processRunner.RunAsync("schtasks.exe", "/Query /FO LIST /V").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to run schtasks.exe /Query");
            return entries;
        }

        if (result.ExitCode != 0)
        {
            _logger.LogWarning("schtasks.exe /Query exited with {ExitCode}: {Error}",
                result.ExitCode, result.StdErr.Trim());
            return entries;
        }

        foreach (var record in SplitRecords(result.StdOut))
        {
            var fullName = Field(record, "TaskName");
            if (string.IsNullOrWhiteSpace(fullName))
            {
                continue;
            }

            // Only tasks that run at boot or at logon belong on the Startup page.
            var scheduleType = Field(record, "Schedule Type") ?? string.Empty;
            if (!scheduleType.Contains("At system start up", StringComparison.OrdinalIgnoreCase) &&
                !scheduleType.Contains("At logon time", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var status = Field(record, "Status") ?? string.Empty;
            var (name, path) = SplitTaskPath(fullName);

            entries.Add(new StartupEntry(
                name,
                path,
                "Scheduled Task",
                !status.Equals("Disabled", StringComparison.OrdinalIgnoreCase)));
        }

        return entries;
    }

    /// <inheritdoc />
    public async Task ToggleScheduledTaskAsync(StartupEntry entry, bool isEnabled, CancellationToken cancellationToken = default)
    {
        // MSFT_ScheduledTask does not support a WMI write path (Set-CimInstance returns
        // "The requested operation is not supported"), so use schtasks.exe, which is the
        // documented way to change a task's enabled state.
        // entry.Command is the task's folder (e.g. "\Microsoft\Edge\" or "\"),
        // so concatenating keeps the separator intact.
        var taskName = entry.Command + entry.Name;

        var arguments = isEnabled
            ? $"/Change /TN \"{taskName}\" /ENABLE"
            : $"/Change /TN \"{taskName}\" /DISABLE";

        var result = await _processRunner.RunAsync("schtasks.exe", arguments).ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"schtasks.exe failed for \"{taskName}\" (exit {result.ExitCode}). {result.StdErr.Trim()}");
        }

        _logger.LogInformation("{Action} scheduled task {TaskName}",
            isEnabled ? "Enabled" : "Disabled", taskName);
    }

    /// <summary>Splits schtasks /FO LIST /V output into one string per task record.</summary>
    private static IEnumerable<string> SplitRecords(string stdout)
    {
        var current = new System.Text.StringBuilder();

        foreach (var line in stdout.Split('\n'))
        {
            var trimmed = line.TrimEnd('\r');

            // Each record starts at the "HostName:" field.
            if (trimmed.StartsWith("HostName:", StringComparison.OrdinalIgnoreCase) && current.Length > 0)
            {
                yield return current.ToString();
                current.Clear();
            }

            if (trimmed.Length > 0)
            {
                current.AppendLine(trimmed);
            }
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }

    /// <summary>Reads a "Field:   value" line out of one schtasks record.</summary>
    private static string? Field(string record, string fieldName)
    {
        foreach (var line in record.Split('\n'))
        {
            var trimmed = line.TrimEnd('\r');
            var colon = trimmed.IndexOf(':');
            if (colon < 0)
            {
                continue;
            }

            var key = trimmed[..colon].Trim();
            if (key.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
            {
                return trimmed[(colon + 1)..].Trim();
            }
        }

        return null;
    }

    /// <summary>Splits "\Microsoft\Edge\Update" into ("Update", "\Microsoft\Edge\").</summary>
    private static (string Name, string Path) SplitTaskPath(string fullName)
    {
        var lastSeparator = fullName.LastIndexOf('\\');

        return lastSeparator < 0
            ? (fullName, "\\")
            : (fullName[(lastSeparator + 1)..], fullName[..(lastSeparator + 1)]);
    }

    /// <summary>A value name prefixed with '-' is the Vain Tools "disabled" marker.</summary>
    private static bool IsEnabled(string valueName) => !valueName.StartsWith('-');

    /// <summary>Strips the '-' disabled marker for display.</summary>
    private static string DisplayName(string valueName) => valueName.TrimStart('-');
}

