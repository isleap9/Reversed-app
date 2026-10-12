using AkariTools.App.ViewModels.General;
using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using Moq;
using Xunit;

namespace AkariTools.Tests.ViewModels.General;

public sealed class SettingsVisibilityViewModelTests
{
    private const string PoliciesExplorerPath =
        @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer";
    private const string SettingsPageVisibilityName = "SettingsPageVisibility";

    private static IRegistrySettingService MockRegistry(string? visibilityValue)
    {
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadString(
            It.Is<RegistryValue>(v =>
                v.Hive == RegistryHive.CurrentUser &&
                v.Path == PoliciesExplorerPath &&
                v.Name == SettingsPageVisibilityName)))
            .Returns(visibilityValue);
        return registry.Object;
    }

    [Fact]
    public async Task Load_BuildsExpectedRows()
    {
        var registry = MockRegistry(null);
        var vm = new SettingsVisibilityViewModel(registry);

        await vm.LoadAsync();

        Assert.Equal(20, vm.Pages.Count);
        Assert.Contains(vm.Pages, p => p.Name == "System" && p.Uri == "ms-settings:system");
        Assert.Contains(vm.Pages, p => p.Name == "Display" && p.Uri == "ms-settings:display");
        Assert.Contains(vm.Pages, p => p.Name == "Taskbar" && p.Uri == "ms-settings:taskbar");
    }

    [Fact]
    public async Task Load_ParsesHiddenSet()
    {
        var registry = MockRegistry("hide:ms-settings:bluetooth;ms-settings:gaming");
        var vm = new SettingsVisibilityViewModel(registry);

        await vm.LoadAsync();

        Assert.Contains(vm.Pages, p => p.Name == "Bluetooth & devices" && p.IsHidden);
        Assert.Contains(vm.Pages, p => p.Name == "Gaming" && p.IsHidden);
        Assert.DoesNotContain(vm.Pages, p => p.Name == "System" && p.IsHidden);
    }

    [Fact]
    public async Task SetHidden_ShowPage_WritesHideUri()
    {
        string? writtenValue = null;
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadString(It.IsAny<RegistryValue>())).Returns("");
        registry.Setup(r => r.WriteString(
                It.Is<RegistryValue>(v =>
                    v.Hive == RegistryHive.CurrentUser &&
                    v.Path == PoliciesExplorerPath &&
                    v.Name == SettingsPageVisibilityName),
                It.IsAny<string>()))
            .Callback<RegistryValue, string>((_, val) => writtenValue = val)
            .Returns(true);

        var vm = new SettingsVisibilityViewModel(registry.Object);
        await vm.LoadAsync();

        var display = vm.Pages.First(p => p.Name == "Display");
        Assert.False(display.IsHidden);

        // The view passes the switch's requested value (OneWay binding never
        // pushes, so the VM must SET, not flip).
        await vm.SetHiddenAsync(display, true);

        Assert.True(display.IsHidden);
        Assert.NotNull(writtenValue);
        Assert.Contains("ms-settings:display", writtenValue);
    }

    [Fact]
    public async Task SetHidden_UnhidePage_RemovesUri()
    {
        string? writtenValue = null;
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadString(It.IsAny<RegistryValue>()))
            .Returns("hide:ms-settings:bluetooth;ms-settings:gaming");
        registry.Setup(r => r.WriteString(
                It.Is<RegistryValue>(v =>
                    v.Hive == RegistryHive.CurrentUser &&
                    v.Path == PoliciesExplorerPath &&
                    v.Name == SettingsPageVisibilityName),
                It.IsAny<string>()))
            .Callback<RegistryValue, string>((_, val) => writtenValue = val)
            .Returns(true);

        var vm = new SettingsVisibilityViewModel(registry.Object);
        await vm.LoadAsync();

        var bluetooth = vm.Pages.First(p => p.Name == "Bluetooth & devices");
        Assert.True(bluetooth.IsHidden);

        await vm.SetHiddenAsync(bluetooth, false);

        Assert.False(bluetooth.IsHidden);
        Assert.NotNull(writtenValue);
        Assert.DoesNotContain("ms-settings:bluetooth", writtenValue);
        Assert.Contains("ms-settings:gaming", writtenValue);
    }

    [Fact]
    public async Task SetHidden_SameValue_SkipsWrite()
    {
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadString(It.IsAny<RegistryValue>())).Returns("");

        var vm = new SettingsVisibilityViewModel(registry.Object);
        await vm.LoadAsync();

        var system = vm.Pages.First(p => p.Name == "System");
        Assert.False(system.IsHidden);

        // Revert echoes (the switch snapping back after a failed apply) carry
        // the value already shown and must not write.
        await vm.SetHiddenAsync(system, false);

        registry.Verify(r => r.WriteString(It.IsAny<RegistryValue>(), It.IsAny<string>()), Times.Never);
        Assert.False(vm.IsApplying);
    }

    [Fact]
    public async Task SetHidden_Failure_RevertsState()
    {
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadString(It.IsAny<RegistryValue>())).Returns("");
        // The real service returns false instead of throwing on denied writes.
        registry.Setup(r => r.WriteString(It.IsAny<RegistryValue>(), It.IsAny<string>()))
            .Returns(false);

        var vm = new SettingsVisibilityViewModel(registry.Object);
        await vm.LoadAsync();

        var system = vm.Pages.First(p => p.Name == "System");
        Assert.False(system.IsHidden);

        await vm.SetHiddenAsync(system, true);

        // Reverted to pre-toggle state after the failure.
        Assert.False(system.IsHidden);
        Assert.False(vm.IsApplying);
    }
}
