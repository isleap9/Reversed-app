using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using Microsoft.Win32;
using Windows.UI;
using Xunit;

namespace AkariTools.Tests.Services;

// Each test gets its own throwaway registry key so the real
// HKCU\Software\AkariToolbox settings are never touched.
public sealed class ThemeServiceTests : IDisposable
{
    private readonly string _keyPath = $@"Software\AkariToolbox.Tests\{Guid.NewGuid():N}";

    public void Dispose() => Registry.CurrentUser.DeleteSubKeyTree(_keyPath, throwOnMissingSubKey: false);

    [Fact]
    public void New_ThemeService_WithMissingKey_DefaultsToSystem()
    {
        var service = new ThemeService(_keyPath);
        Assert.Equal(AppTheme.System, service.CurrentTheme);
    }

    [Theory]
    [InlineData(AppTheme.Light)]
    [InlineData(AppTheme.Dark)]
    [InlineData(AppTheme.FullDark)]
    [InlineData(AppTheme.System)]
    public void SetTheme_UpdatesCurrentTheme(AppTheme theme)
    {
        var service = new ThemeService(_keyPath);
        service.SetTheme(theme);
        Assert.Equal(theme, service.CurrentTheme);
    }

    [Fact]
    public void SetTheme_RaisesThemeChangedEvent()
    {
        var service = new ThemeService(_keyPath);
        AppTheme? received = null;
        service.ThemeChanged += (_, e) => received = e;

        service.SetTheme(AppTheme.Dark);

        Assert.Equal(AppTheme.Dark, received);
    }

    [Fact]
    public void Theme_PersistsToRegistry_WithSpecValues()
    {
        new ThemeService(_keyPath).SetTheme(AppTheme.FullDark);

        Assert.Equal(AppTheme.FullDark, new ThemeService(_keyPath).CurrentTheme);
        using var key = Registry.CurrentUser.OpenSubKey(_keyPath);
        Assert.Equal(3, key!.GetValue("Theme"));
    }

    [Fact]
    public void InvalidRegistryValue_FallsBackToSystem()
    {
        using (var key = Registry.CurrentUser.CreateSubKey(_keyPath))
            key.SetValue("Theme", 42, RegistryValueKind.DWord);

        Assert.Equal(AppTheme.System, new ThemeService(_keyPath).CurrentTheme);
    }

    [Fact]
    public void GetAccentColor_WithNoOverride_ReturnsNull()
    {
        Assert.Null(new ThemeService(_keyPath).GetAccentColor());
    }

    [Fact]
    public void SetAccentColor_RoundTripsThroughRegistry()
    {
        var red = Color.FromArgb(255, 255, 0, 0);
        new ThemeService(_keyPath).SetAccentColor(red);

        Assert.Equal(red, new ThemeService(_keyPath).GetAccentColor());
    }

    [Fact]
    public void SetAccentColor_RaisesAccentColorChangedEvent()
    {
        var service = new ThemeService(_keyPath);
        Color? received = null;
        service.AccentColorChanged += (_, e) => received = e;

        var green = Color.FromArgb(255, 0, 200, 0);
        service.SetAccentColor(green);

        Assert.Equal(green, received);
    }

    [Fact]
    public void ClearAccentColor_RevertsToSystemAccent()
    {
        var service = new ThemeService(_keyPath);
        service.SetAccentColor(Color.FromArgb(255, 1, 2, 3));
        service.SetAccentColor(null);

        Assert.Null(new ThemeService(_keyPath).GetAccentColor());
    }
}
