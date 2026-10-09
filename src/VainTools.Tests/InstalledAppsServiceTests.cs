using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32;
using VainTools.App.Services;
using Xunit;

namespace VainTools.Tests;

/// <summary>
/// Service tests for <see cref="InstalledAppsService"/> run against throwaway
/// <c>HKCU\Software\VainTools\Test\&lt;guid&gt;</c> keys — never the real uninstall keys.
/// </summary>
public sealed class InstalledAppsServiceTests : IDisposable
{
    private readonly string _root = $@"SOFTWARE\VainTools\Test\{Guid.NewGuid():N}";
    private readonly InstalledAppsService _service;

    public InstalledAppsServiceTests()
    {
        _service = new InstalledAppsService(
            NullLogger<InstalledAppsService>.Instance,
            [(Registry.CurrentUser, _root)]);
    }

    public void Dispose()
    {
        Registry.CurrentUser.DeleteSubKeyTree(_root, throwOnMissingSubKey: false);
    }

    [Fact]
    public void GetInstalledApps_ReadsEntriesFromConfiguredHive()
    {
        WriteEntry("AppOne", displayName: "App One", version: "1.2.3", publisher: "Contoso",
            installLocation: @"C:\AppOne", uninstall: "\"C:\\AppOne\\uninstall.exe\"",
            quiet: "\"C:\\AppOne\\uninstall.exe\" /S");

        var apps = _service.GetInstalledApps();

        var app = Assert.Single(apps);
        Assert.Equal("App One", app.DisplayName);
        Assert.Equal("1.2.3", app.DisplayVersion);
        Assert.Equal("Contoso", app.Publisher);
        Assert.Equal(@"C:\AppOne", app.InstallLocation);
        Assert.Equal("\"C:\\AppOne\\uninstall.exe\"", app.UninstallString);
        Assert.Equal("\"C:\\AppOne\\uninstall.exe\" /S", app.QuietUninstallString);
        Assert.Contains("AppOne", app.RegistryPath);
    }

    [Fact]
    public void GetInstalledApps_SkipsEntriesWithNullDisplayName()
    {
        WriteEntry("NoName", displayName: null, uninstall: "whatever.exe");
        WriteEntry("Named", displayName: "Named App", uninstall: "named.exe");

        var apps = _service.GetInstalledApps();

        var app = Assert.Single(apps);
        Assert.Equal("Named App", app.DisplayName);
    }

    [Fact]
    public void GetInstalledApps_DeduplicatesByDisplayNameCaseInsensitively()
    {
        WriteEntry("AppX64", displayName: "Same App", uninstall: "x64.exe");
        WriteEntry("AppX86", displayName: "SAME APP", uninstall: "x86.exe");

        var apps = _service.GetInstalledApps();

        var app = Assert.Single(apps);
        Assert.Equal("x64.exe", app.UninstallString);
    }

    [Fact]
    public void GetInstalledApps_MapsMissingFieldsToEmpty()
    {
        WriteEntry("Bare", displayName: "Bare App");

        var apps = _service.GetInstalledApps();

        var app = Assert.Single(apps);
        Assert.Equal(string.Empty, app.DisplayVersion);
        Assert.Equal(string.Empty, app.Publisher);
        Assert.Equal(string.Empty, app.InstallLocation);
        Assert.Equal(string.Empty, app.UninstallString);
        Assert.Equal(string.Empty, app.QuietUninstallString);
    }

    [Fact]
    public void GetInstalledApps_ReturnsEmptyWhenKeyMissing()
    {
        var service = new InstalledAppsService(
            NullLogger<InstalledAppsService>.Instance,
            [(Registry.CurrentUser, $@"SOFTWARE\VainTools\Test\{Guid.NewGuid():N}\DoesNotExist")]);

        Assert.Empty(service.GetInstalledApps());
    }

    [Fact]
    public void UninstallKeyLocations_CoversThreeHives()
    {
        Assert.Equal(3, InstalledAppsService.UninstallKeyLocations.Length);
        Assert.Contains(InstalledAppsService.UninstallKeyLocations,
            l => l.Hive == Registry.LocalMachine && l.Path.EndsWith("Uninstall"));
        Assert.Contains(InstalledAppsService.UninstallKeyLocations,
            l => l.Hive == Registry.LocalMachine && l.Path.Contains("WOW6432Node"));
        Assert.Contains(InstalledAppsService.UninstallKeyLocations,
            l => l.Hive == Registry.CurrentUser && l.Path.EndsWith("Uninstall"));
    }

    [Fact]
    public void GetInstalledApps_ReadsFromEveryConfiguredLocation()
    {
        var secondRoot = $@"SOFTWARE\VainTools\Test\{Guid.NewGuid():N}";
        try
        {
            using (var k = Registry.CurrentUser.CreateSubKey(secondRoot))
            {
                using var sub = k.CreateSubKey("SecondApp");
                sub.SetValue("DisplayName", "Second App");
                sub.SetValue("UninstallString", "second.exe");
            }

            WriteEntry("FirstApp", displayName: "First App", uninstall: "first.exe");

            var service = new InstalledAppsService(
                NullLogger<InstalledAppsService>.Instance,
                [(Registry.CurrentUser, _root), (Registry.CurrentUser, secondRoot)]);

            var apps = service.GetInstalledApps();

            Assert.Equal(2, apps.Count);
            Assert.Contains(apps, a => a.DisplayName == "First App");
            Assert.Contains(apps, a => a.DisplayName == "Second App");
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(secondRoot, throwOnMissingSubKey: false);
        }
    }

    private void WriteEntry(
        string subKey,
        string? displayName,
        string? version = null,
        string? publisher = null,
        string? installLocation = null,
        string? uninstall = null,
        string? quiet = null)
    {
        using var key = Registry.CurrentUser.CreateSubKey($@"{_root}\{subKey}");
        if (displayName is not null)
        {
            key.SetValue("DisplayName", displayName);
        }

        if (version is not null)
        {
            key.SetValue("DisplayVersion", version);
        }

        if (publisher is not null)
        {
            key.SetValue("Publisher", publisher);
        }

        if (installLocation is not null)
        {
            key.SetValue("InstallLocation", installLocation);
        }

        if (uninstall is not null)
        {
            key.SetValue("UninstallString", uninstall);
        }

        if (quiet is not null)
        {
            key.SetValue("QuietUninstallString", quiet);
        }
    }
}
