namespace VainTools.App.Services;

/// <summary>
/// Reads installed programs from the Windows uninstall registry.
/// </summary>
public interface IInstalledAppsService
{
    /// <summary>
    /// Enumerates programs across the uninstall registry hives.
    /// </summary>
    IReadOnlyList<InstalledApp> GetInstalledApps();
}
