using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32;
using VainTools.App.Models;
using VainTools.App.Services;
using Xunit;

namespace VainTools.Tests;

/// <summary>
/// Tests for the registry tweak engine.
///
/// Every test operates on a throwaway key under
/// <c>HKCU\Software\VainTools\Test\...</c> and cleans up after itself. No test ever
/// touches a real Windows settings key — a mistake here would change the developer's
/// machine, so the sandbox is not negotiable.
/// </summary>
public sealed class RegistryTweakServiceTests : IDisposable
{
    private const string SandboxRoot = @"Software\VainTools\Test";

    private readonly RegistryTweakService _service =
        new(NullLogger<RegistryTweakService>.Instance);

    private readonly string _keyPath =
        $@"{SandboxRoot}\{Guid.NewGuid():N}";

    private static RegistryTweak Tweak(string keyPath, string valueName = "TestValue") => new()
    {
        Id = "test.tweak",
        Name = "Test tweak",
        Hive = RegistryHive.CurrentUser,
        KeyPath = keyPath,
        ValueName = valueName,
        EnabledValue = 1,
        DisabledValue = 0,
        DefaultWhenUnset = TweakState.Disabled,
    };

    public void Dispose()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(_keyPath, throwOnMissingSubKey: false);
        }
        catch
        {
            // Cleanup is best-effort; a leftover sandbox key is harmless.
        }
    }

    [Fact]
    public void Read_WhenValueAbsent_ReturnsUnset()
    {
        var state = _service.Read(Tweak(_keyPath));

        Assert.Equal(TweakState.Unset, state);
    }

    [Fact]
    public async Task Apply_ThenRead_ReportsEnabled()
    {
        var tweak = Tweak(_keyPath);

        await _service.ApplyAsync(tweak);

        Assert.Equal(TweakState.Enabled, _service.Read(tweak));
    }

    [Fact]
    public async Task Revert_ThenRead_ReportsDisabled()
    {
        var tweak = Tweak(_keyPath);

        await _service.ApplyAsync(tweak);
        await _service.RevertAsync(tweak);

        Assert.Equal(TweakState.Disabled, _service.Read(tweak));
    }

    [Fact]
    public async Task Apply_WritesTheExpectedRawValue()
    {
        var tweak = Tweak(_keyPath);

        await _service.ApplyAsync(tweak);

        using var key = Registry.CurrentUser.OpenSubKey(_keyPath);
        Assert.Equal(1, key!.GetValue("TestValue"));
    }

    [Fact]
    public async Task Apply_CreatesTheKeyWhenMissing()
    {
        var tweak = Tweak(_keyPath);

        await _service.ApplyAsync(tweak);

        using var key = Registry.CurrentUser.OpenSubKey(_keyPath);
        Assert.NotNull(key);
    }

    [Fact]
    public async Task SetAsync_WithUnsetState_Throws()
    {
        var tweak = Tweak(_keyPath);

        // "Unset" is a readable observation, not a writable target.
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.SetAsync(tweak, TweakState.Unset));
    }

    [Fact]
    public async Task SetAsync_WithUnknownState_Throws()
    {
        var tweak = Tweak(_keyPath);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.SetAsync(tweak, TweakState.Unknown));
    }

    [Fact]
    public async Task Apply_WithoutAdmin_ForAdminTweak_ThrowsUnauthorized()
    {
        // The test host is not elevated, so an admin-required tweak must refuse rather
        // than silently fail. If this host IS elevated the assertion is inverted below.
        var tweak = Tweak(_keyPath) with { RequiresAdmin = true };

        if (_service.IsElevated)
        {
            await _service.ApplyAsync(tweak);
            Assert.Equal(TweakState.Enabled, _service.Read(tweak));
        }
        else
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.ApplyAsync(tweak));
        }
    }

    [Fact]
    public async Task StringValueKind_IsStoredAsString()
    {
        var tweak = Tweak(_keyPath) with
        {
            ValueKind = RegistryValueKind.String,
            EnabledValue = "1",
            DisabledValue = "0",
        };

        await _service.ApplyAsync(tweak);

        using var key = Registry.CurrentUser.OpenSubKey(_keyPath);
        Assert.Equal("1", key!.GetValue("TestValue"));

        // And the reader must recognise a string "1" as enabled.
        Assert.Equal(TweakState.Enabled, _service.Read(tweak));
    }

    [Fact]
    public async Task RemoveKeyWhenDisabled_DeletesTheKey()
    {
        // Switches whose state is the key's existence (e.g. the classic context menu)
        // must be removed entirely on revert, not written an empty value.
        var tweak = Tweak(_keyPath) with { RemoveKeyWhenDisabled = true };

        await _service.ApplyAsync(tweak);
        using (var created = Registry.CurrentUser.OpenSubKey(_keyPath))
        {
            Assert.NotNull(created);
        }

        await _service.RevertAsync(tweak);

        using var after = Registry.CurrentUser.OpenSubKey(_keyPath);
        Assert.Null(after);
        Assert.Equal(TweakState.Unset, _service.Read(tweak));
    }

[Fact]
    public async Task KeyPresenceSwitch_ReadsEnabledWhenKeyExistsAndUnsetWhenRemoved()
    {
        // The classic context-menu tweak is switched by the *existence* of a key whose
        // default (unnamed) value is empty. Reading it must not confuse "key absent"
        // with "value empty", or the toggle would never show the right position.
        var tweak = Tweak(_keyPath, valueName: string.Empty) with
        {
            ValueKind = RegistryValueKind.String,
            EnabledValue = string.Empty,
            DisabledValue = string.Empty,
            RemoveKeyWhenDisabled = true,
        };

        Assert.Equal(TweakState.Unset, _service.Read(tweak));

        await _service.ApplyAsync(tweak);
        Assert.Equal(TweakState.Enabled, _service.Read(tweak));

        await _service.RevertAsync(tweak);
        Assert.Equal(TweakState.Unset, _service.Read(tweak));
    }

    [Fact]
    public void Read_ValuePresentButUnexpected_ReportsDisabled()
    {
        // A value that is neither the enabled nor disabled representation should still
        // produce a definite state, so the UI never shows an ambiguous switch.
        var tweak = Tweak(_keyPath);

        using (var key = Registry.CurrentUser.CreateSubKey(_keyPath, writable: true))
        {
            key!.SetValue("TestValue", 99, RegistryValueKind.DWord);
        }

        Assert.Equal(TweakState.Disabled, _service.Read(tweak));
    }

    [Fact]
    public void Catalog_IdsAreUnique()
    {
        var ids = TweakCatalog.All.Select(t => t.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Catalog_EveryTweakHasAKeyPathAndName()
    {
        foreach (var tweak in TweakCatalog.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(tweak.Id));
            Assert.False(string.IsNullOrWhiteSpace(tweak.Name));
            Assert.False(string.IsNullOrWhiteSpace(tweak.KeyPath));
        }
    }

    [Fact]
    public void Catalog_ClassicContextMenuRemovesKeyWhenDisabled()
    {
        var tweak = TweakCatalog.Find("contextmenu.classic-menu");

        Assert.NotNull(tweak);
        Assert.True(tweak!.RemoveKeyWhenDisabled);
    }

    [Fact]
    public void Catalog_OnlyTweaksThatNeedAdminAreMarkedSo()
    {
        // Guard against a machine-wide key being writable without elevation.
        foreach (var tweak in TweakCatalog.All.Where(t => t.Hive == RegistryHive.LocalMachine))
        {
            Assert.True(tweak.RequiresAdmin, $"{tweak.Id} writes HKLM but is not marked RequiresAdmin");
        }
    }

    [Fact]
    public void Catalog_ReadsEveryTweakWithoutThrowing()
    {
        // Reading the real catalog must be safe: it is what every page does on load.
        foreach (var tweak in TweakCatalog.All)
        {
            var state = _service.Read(tweak);
            Assert.True(Enum.IsDefined(state), $"{tweak.Id} produced an undefined state");
        }
    }
}
