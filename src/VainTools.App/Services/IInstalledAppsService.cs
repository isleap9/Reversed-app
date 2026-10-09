namespace VainTools.App.Services;

/// <summary>
/// Reads installed programs from the Windows uninstall registry.
/// </summary>
public interface IInstalledAppsService
{
    /// <summary>
    /// Enumerates programs across the uninstall registry hives (D-03): HKLM 64-bit,
    /// HKLM 32-bit (WOW6432Node) and HKCU.
    /// </summary>
    IReadOnlyList<InstalledApp> GetInstalledApps();
}
