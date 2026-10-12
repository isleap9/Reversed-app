using AkariTools.App.ViewModels.General;
using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using Moq;
using Xunit;

namespace AkariTools.Tests.ViewModels.General;

public sealed class ExplorerViewModelTests
{
    // Every Explorer row lives under this key (spec §7.2).
    private const string AdvancedPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";

    [Fact]
    public async Task Load_BuildsExpectedRows()
    {
        // Definitions pinning: a wrong key path ships a broken feature that
        // nothing else catches, so the paths are asserted here.
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadDword(It.IsAny<RegistryValue>())).Returns(0);

        var vm = new ExplorerViewModel(registry.Object);
        await vm.LoadAsync();

        Assert.Equal(4, vm.Rows.Count);
        Assert.Equal(
            new[] { "HideFileExt", "Hidden", "ShowSuperHidden", "TaskbarDa" },
            vm.Rows.Select(r => r.Setting.Id));
        foreach (var row in vm.Rows)
        {
            Assert.Equal(RegistryHive.CurrentUser, row.Setting.ValueKey.Hive);
            Assert.Equal(AdvancedPath, row.Setting.ValueKey.Path);
            Assert.Equal(ToggleKind.Dword, row.Setting.Kind);
            Assert.Equal(1, row.Setting.OnValue);
            Assert.Equal(0, row.Setting.OffValue);
        }
    }

    [Fact]
    public async Task TaskbarDaRow_LabelsWidgetsBoard()
    {
        // TaskbarDa is the Widgets board toggle: the label must name the
        // behavior the key actually controls, not multi-monitor taskbar.
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadDword(It.IsAny<RegistryValue>())).Returns(0);

        var vm = new ExplorerViewModel(registry.Object);
        await vm.LoadAsync();

        var row = vm.Rows.First(r => r.Setting.Id == "TaskbarDa");
        Assert.Equal("Widgets board", row.Label);
    }

    [Fact]
    public async Task Load_MapsDwordState()
    {
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadDword(It.Is<RegistryValue>(v => v.Name == "HideFileExt"))).Returns(1);
        registry.Setup(r => r.ReadDword(It.Is<RegistryValue>(v => v.Name != "HideFileExt"))).Returns(0);

        var vm = new ExplorerViewModel(registry.Object);
        await vm.LoadAsync();

        Assert.True(vm.Rows[0].IsOn);
        Assert.False(vm.Rows[1].IsOn);
        Assert.False(vm.Rows[2].IsOn);
        Assert.False(vm.Rows[3].IsOn);
    }
}
