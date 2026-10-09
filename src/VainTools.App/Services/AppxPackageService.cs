using Microsoft.Extensions.Logging;
using Wmd = Windows.Management.Deployment;

namespace VainTools.App.Services;

/// <summary>
/// Implements <see cref="IAppxPackageService"/> over the WinRT
/// <c>Windows.Management.Deployment.PackageManager</c>.
///
/// Exceptions from WinRT are deliberately NOT caught here — they propagate to the
/// view model, which reports them through the error InfoBar (T-06-07: user-friendly
/// message on screen, full details in the log).
/// </summary>
public sealed class AppxPackageService : IAppxPackageService
{
    private readonly ILogger<AppxPackageService> _logger;

    public AppxPackageService(ILogger<AppxPackageService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<AppxPackage> GetInstalledPackages()
    {
        var manager = new Wmd.PackageManager();
        var packages = manager.FindPackages()
            .Select(p => Map(p, isProvisioned: false))
            .ToList();

        _logger.LogInformation("Enumerated {Count} installed Appx packages", packages.Count);
        return packages;
    }

    /// <inheritdoc />
    public IReadOnlyList<AppxPackage> GetProvisionedPackages()
    {
        var manager = new Wmd.PackageManager();
        var packages = manager.FindProvisionedPackages()
            .Select(p => Map(p, isProvisioned: true))
            .ToList();

        _logger.LogInformation("Enumerated {Count} provisioned Appx packages", packages.Count);
        return packages;
    }

    /// <inheritdoc />
    public async Task<DeploymentResult> RemovePackageAsync(string packageFullName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageFullName);

        _logger.LogInformation("Removing Appx package {FullName}", packageFullName);

        var manager = new Wmd.PackageManager();
        var operation = manager.RemovePackageAsync(packageFullName);
        var raw = await operation.AsTask().ConfigureAwait(false);

        var success = raw.ExtendedErrorCode is null || raw.ExtendedErrorCode.HResult == 0;
        if (success)
        {
            _logger.LogInformation("Removed Appx package {FullName}", packageFullName);
        }
        else
        {
            _logger.LogWarning("Failed to remove Appx package {FullName}: {Error}",
                packageFullName, raw.ErrorText);
        }

        return new DeploymentResult(success, success ? null : raw.ErrorText);
    }

    private static AppxPackage Map(Windows.ApplicationModel.Package package, bool isProvisioned)
    {
        var id = package.Id;
        var version = id.Version;
        return new AppxPackage(
            id.FullName,
            id.Name,
            id.Publisher,
            $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}",
            package.InstalledLocation.Path,
            isProvisioned);
    }
}
