using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace VainTools.App.Services;

/// <summary>
/// Implements <see cref="IInstalledAppsService"/> by reading the uninstall registry
/// directly (D-03): HKLM 64-bit, HKLM 32-bit (WOW6432Node), and HKCU.
///
/// Registry values are treated as display data only — they are never executed by this
/// service (T-06-05). Execution happens in the view model via an explicit user-confirmed
/// <c>Process.Start</c> (T-06-02, T-06-03).
///
/// Exceptions are deliberately NOT caught here — they propagate to the view model,
/// which reports them through the error InfoBar (D-12).
/// </summary>
public sealed class InstalledAppsService : IInstalledAppsService
{
    /// <summary>
    /// Uninstall key locations, in display order: machine 64-bit, machine 32-bit, current user.
    /// </summary>
    public static readonly (RegistryKey Hive, string Path)[] UninstallKeyLocations =
    [
        (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
        (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"),
        (Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
    ];

    private readonly ILogger<InstalledAppsService> _logger;
    private readonly IReadOnlyList<(RegistryKey Hive, string Path)> _locations;

    public InstalledAppsService(ILogger<InstalledAppsService> logger)
        : this(logger, UninstallKeyLocations)
    {
    }

    /// <summary>
    /// Test seam: unit tests pass throwaway <c>HKCU\Software\VainTools\Test\&lt;guid&gt;</c>
    /// roots here so the real uninstall keys are never read or written.
    /// </summary>
    public InstalledAppsService(
        ILogger<InstalledAppsService> logger,
        IReadOnlyList<(RegistryKey Hive, string Path)> locations)
    {
        _logger = logger;
        _locations = locations;
    }

    /// <inheritdoc />
    public IReadOnlyList<InstalledApp> GetInstalledApps()
    {
        var apps = new List<InstalledApp>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (hive, path) in _locations)
        {
            using var key = hive.OpenSubKey(path);
            if (key is null)
            {
                continue;
            }

            foreach (var subKeyName in key.GetSubKeyNames())
            {
                using var subKey = key.OpenSubKey(subKeyName);
                if (subKey is null)
                {
                    continue;
                }

                var displayName = subKey.GetValue("DisplayName")?.ToString();
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    continue;
                }

                // Deduplicate by display name (x86/x64 twins register twice); keep the first.
                if (!seen.Add(displayName))
                {
                    continue;
                }

                apps.Add(new InstalledApp(
                    displayName,
                    subKey.GetValue("DisplayVersion")?.ToString() ?? string.Empty,
                    subKey.GetValue("Publisher")?.ToString() ?? string.Empty,
                    subKey.GetValue("InstallLocation")?.ToString() ?? string.Empty,
                    subKey.GetValue("UninstallString")?.ToString() ?? string.Empty,
                    subKey.GetValue("QuietUninstallString")?.ToString() ?? string.Empty,
                    $@"{hive.Name}\{path}\{subKeyName}"));
            }
        }

        _logger.LogInformation("Enumerated {Count} installed programs", apps.Count);
        return apps;
    }
}
