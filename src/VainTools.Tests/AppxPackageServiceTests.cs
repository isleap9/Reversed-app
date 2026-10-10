using Microsoft.Extensions.Logging.Abstractions;
using VainTools.App.Services;
using Xunit;

namespace VainTools.Tests;

/// <summary>
/// Service tests for <see cref="AppxPackageService"/> run against the real WinRT
/// <c>PackageManager</c> (read-only enumeration). The remove path is only exercised
/// with invalid input, which must surface — never be swallowed.
/// </summary>
public sealed class AppxPackageServiceTests
{
    private readonly AppxPackageService _service = new(NullLogger<AppxPackageService>.Instance);

    [Fact]
    public void GetInstalledPackages_ReturnsList()
    {
        var packages = _service.GetInstalledPackages();

        Assert.NotNull(packages);
        Assert.NotEmpty(packages);
    }

    [Fact]
    public void GetInstalledPackages_AllHaveFullNameAndName()
    {
        Assert.All(_service.GetInstalledPackages(), p =>
        {
            Assert.False(string.IsNullOrWhiteSpace(p.FullName));
            Assert.False(string.IsNullOrWhiteSpace(p.Name));
        });
    }

    [Fact]
    public void GetInstalledPackages_MarksNoneAsProvisioned()
    {
        Assert.All(_service.GetInstalledPackages(), p => Assert.False(p.IsProvisioned));
    }

    [Fact]
    public void GetInstalledPackages_VersionHasFourParts()
    {
        Assert.All(_service.GetInstalledPackages(), p => Assert.Equal(4, p.Version.Split('.').Length));
    }

    [Fact]
    public void GetProvisionedPackages_ReturnsList()
    {
        var packages = _service.GetProvisionedPackages();

        Assert.NotNull(packages);
        Assert.NotEmpty(packages);
    }

    [Fact]
    public void GetProvisionedPackages_MarksAllAsProvisioned()
    {
        Assert.All(_service.GetProvisionedPackages(), p => Assert.True(p.IsProvisioned));
    }

    [Fact]
    public void GetProvisionedPackages_AllHaveFullNameAndName()
    {
        Assert.All(_service.GetProvisionedPackages(), p =>
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

    [Fact]
    public void GetInstalledPackages_AllCarryFamilyName()
    {
        Assert.All(_service.GetInstalledPackages(), p =>
        {
            Assert.False(string.IsNullOrWhiteSpace(p.PackageFamilyName));
            Assert.StartsWith(p.Name + "_", p.PackageFamilyName, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void GetProvisionedPackages_AllCarryFamilyName()
    {
        Assert.All(_service.GetProvisionedPackages(), p =>
        {
            Assert.False(string.IsNullOrWhiteSpace(p.PackageFamilyName));
            Assert.StartsWith(p.Name + "_", p.PackageFamilyName, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task DeprovisionPackageAsync_WithEmptyName_ThrowsArgument()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.DeprovisionPackageAsync(string.Empty));
    }

    [Fact]
    public async Task DeprovisionPackageAsync_WithBogusFamily_NeverReportsSuccess()
    {
        DeploymentResult? result = null;
        Exception? thrown = null;
        try
        {
            result = await _service.DeprovisionPackageAsync("VainTools.Bogus.NotProvisioned_0000000000000");
        }
        catch (Exception ex)
        {
            thrown = ex;
        }

        Assert.True(thrown is not null || result is { Success: false }, "Bogus deprovision must throw or report failure.");
        if (result is not null)
        {
            Assert.False(result.Success);
        }
    }

    [Fact]
    public void SafeInstalledPath_WhenReadThrows_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, AppxPackageService.SafeInstalledPath(() => throw new InvalidOperationException("stale")));
    }

    [Fact]
    public void SafeInstalledPath_ReturnsValue()
    {
        Assert.Equal("C:\\Packages\\App", AppxPackageService.SafeInstalledPath(() => "C:\\Packages\\App"));
    }
}
