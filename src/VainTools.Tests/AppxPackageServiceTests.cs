using Microsoft.Extensions.Logging.Abstractions;
using VainTools.App.Services;
using Xunit;

namespace VainTools.Tests;

/// <summary>
/// Service tests for <see cref="AppxPackageService"/> run against the real WinRT
/// <c>PackageManager</c> (read-only enumeration). The remove path is only exercised
/// with invalid input, which must surface — never swallow — the WinRT failure.
/// </summary>
public sealed class AppxPackageServiceTests
{
    private readonly AppxPackageService _service = new(NullLogger<AppxPackageService>.Instance);

    [Fact]
    public void GetInstalledPackages_ReturnsList()
    {
        var packages = _service.GetInstalledPackages();

        Assert.NotNull(packages);
    }

    [Fact]
    public void GetInstalledPackages_AllHaveFullNameAndName()
    {
        var packages = _service.GetInstalledPackages();

        Assert.All(packages, p =>
        {
            Assert.False(string.IsNullOrWhiteSpace(p.FullName));
            Assert.False(string.IsNullOrWhiteSpace(p.Name));
        });
    }

    [Fact]
    public void GetInstalledPackages_MarksNoneAsProvisioned()
    {
        var packages = _service.GetInstalledPackages();

        Assert.All(packages, p => Assert.False(p.IsProvisioned));
    }

    [Fact]
    public void GetInstalledPackages_VersionHasFourParts()
    {
        var packages = _service.GetInstalledPackages();

        Assert.All(packages, p => Assert.Equal(4, p.Version.Split('.').Length));
    }

    [Fact]
    public void GetProvisionedPackages_ReturnsList()
    {
        var packages = _service.GetProvisionedPackages();

        Assert.NotNull(packages);
    }

    [Fact]
    public void GetProvisionedPackages_MarksAllAsProvisioned()
    {
        var packages = _service.GetProvisionedPackages();

        Assert.All(packages, p => Assert.True(p.IsProvisioned));
    }

    [Fact]
    public void GetProvisionedPackages_AllHaveFullNameAndName()
    {
        var packages = _service.GetProvisionedPackages();

        Assert.All(packages, p =>
        {
            Assert.False(string.IsNullOrWhiteSpace(p.FullName));
            Assert.False(string.IsNullOrWhiteSpace(p.Name));
        });
    }

    [Fact]
    public async Task RemovePackageAsync_WithBogusName_ThrowsInsteadOfSwallowing()
    {
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _service.RemovePackageAsync("Bogus.Package_ThatDoesNotExist_1.0.0.0_neutral__000000000000"));
    }

    [Fact]
    public async Task RemovePackageAsync_WithEmptyName_ThrowsArgument()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.RemovePackageAsync(string.Empty));
    }
}
