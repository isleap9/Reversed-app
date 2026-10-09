namespace VainTools.App.Services;

/// <summary>
/// Enumerates and removes Appx/MSIX packages via the WinRT <c>PackageManager</c>.
/// </summary>
public interface IAppxPackageService
{
    /// <summary>Lists packages installed for the current user.</summary>
    IReadOnlyList<AppxPackage> GetInstalledPackages();

    /// <summary>Lists packages provisioned into the OS image.</summary>
    IReadOnlyList<AppxPackage> GetProvisionedPackages();

    /// <summary>
    /// Removes a package by full name. Requires elevation. Irreversible: the caller must confirm first.
    /// </summary>
    Task<DeploymentResult> RemovePackageAsync(string packageFullName);
}

/// <summary>
/// Outcome of an Appx deployment operation (remove). Kept as a plain record — rather than
/// surfacing the WinRT <c>DeploymentResult</c> type — so callers and unit tests never need to
/// construct WinRT objects.
/// </summary>
public sealed record DeploymentResult(bool Success, string? ErrorText);
