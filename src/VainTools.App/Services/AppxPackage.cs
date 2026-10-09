namespace VainTools.App.Services;

/// <summary>
/// An installed or provisioned Appx/MSIX package enumerated via WinRT <c>PackageManager</c>.
/// </summary>
public sealed record AppxPackage(
    string FullName,
    string Name,
    string Publisher,
    string Version,
    string InstallLocation,
    bool IsProvisioned);
