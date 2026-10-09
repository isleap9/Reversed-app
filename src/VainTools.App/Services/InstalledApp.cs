namespace VainTools.App.Services;

/// <summary>
/// A program listed in the Windows uninstall registry.
/// </summary>
public sealed record InstalledApp(
    string DisplayName,
    string DisplayVersion,
    string Publisher,
    string InstallLocation,
    string UninstallString,
    string QuietUninstallString,
    string RegistryPath);
