using AkariTools.App.ViewModels.General;
using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using Moq;
using Xunit;

namespace AkariTools.Tests.ViewModels.General;

public sealed class VisualViewModelTests
{
    // Definitions pinning: transparency lives under Themes\Personalize, the
    // other two keys under Explorer\Advanced (spec §7.4).
    private const string PersonalizePath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AdvancedPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string LightThemeName = "AppsUseLightTheme";

    [Fact]
    public async Task Load_BuildsExpectedRows()
    {
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadDword(It.IsAny<RegistryValue>())).Returns(0);

        var vm = new VisualViewModel(registry.Object);
        await vm.LoadAsync();

        Assert.Equal(3, vm.Rows.Count);
        Assert.Equal(
            new[] { "EnableTransparency", "TaskbarAnimations", "ListviewAlphaSelect" },
            vm.Rows.Select(r => r.Setting.Id));

        Assert.Equal(PersonalizePath, vm.Rows[0].Setting.ValueKey.Path);
        Assert.Equal(AdvancedPath, vm.Rows[1].Setting.ValueKey.Path);
        Assert.Equal(AdvancedPath, vm.Rows[2].Setting.ValueKey.Path);
        foreach (var row in vm.Rows)
        {
            Assert.Equal(RegistryHive.CurrentUser, row.Setting.ValueKey.Hive);
            Assert.Equal(ToggleKind.Dword, row.Setting.Kind);
            Assert.Equal(1, row.Setting.OnValue);
            Assert.Equal(0, row.Setting.OffValue);
        }
    }

    [Fact]
    public async Task Load_ReadsLightThemeDisplay()
    {
        // AppsUseLightTheme (DWORD, 1 = light) is display-only (spec §7.4).
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadDword(It.Is<RegistryValue>(
            v => v.Path == PersonalizePath && v.Name == LightThemeName))).Returns(1);
        registry.Setup(r => r.ReadDword(It.Is<RegistryValue>(
            v => !(v.Path == PersonalizePath && v.Name == LightThemeName)))).Returns(0);

        var vm = new VisualViewModel(registry.Object);
        await vm.LoadAsync();

        Assert.Equal("On", vm.AppsUseLightThemeText);
    }
}
