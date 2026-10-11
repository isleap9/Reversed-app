using AkariTools.App.ViewModels.General;
using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using Moq;
using Xunit;

namespace AkariTools.Tests.ViewModels.General;

public sealed class SettingRowViewModelTests
{
    // Throwaway-style fake path; mocks only — the real registry is never touched.
    private const string TestPath = @"Software\AkariToolbox.Tests\SettingRow";
    private const string TestName = "TestDword";

    private static ToggleSetting DwordSetting() => new()
    {
        Id = "TestDword",
        Label = "Test toggle",
        Description = "Test description.",
        Kind = ToggleKind.Dword,
        ValueKey = new RegistryValue(RegistryHive.CurrentUser, TestPath, TestName),
        OnValue = 1,
        OffValue = 0,
    };

    [Fact]
    public async Task SetAsync_WritesThenRereads()
    {
        // Review Focus #5: the write is followed by a re-read, so the row
        // reflects the real machine state, not the requested one.
        var current = 0;
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadDword(It.IsAny<RegistryValue>())).Returns(() => current);
        registry.Setup(r => r.WriteDword(It.IsAny<RegistryValue>(), It.IsAny<int>()))
            .Callback<RegistryValue, int>((_, data) => current = data)
            .Returns(true);

        var vm = new SettingRowViewModel(DwordSetting(), registry.Object);
        await vm.LoadAsync();
        Assert.False(vm.IsOn);

        await vm.SetAsync(true);

        registry.Verify(r => r.WriteDword(
            It.Is<RegistryValue>(v => v.Path == TestPath && v.Name == TestName), 1), Times.Once);
        Assert.True(vm.IsOn);
    }

    [Fact]
    public async Task SetAsync_SameValue_SkipsWrite()
    {
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadDword(It.IsAny<RegistryValue>())).Returns(0);

        var vm = new SettingRowViewModel(DwordSetting(), registry.Object);
        await vm.LoadAsync();
        Assert.False(vm.IsOn);

        // IsOn is already false: the same-value guard must skip the write.
        await vm.SetAsync(false);

        registry.Verify(r => r.WriteDword(It.IsAny<RegistryValue>(), It.IsAny<int>()), Times.Never);
        Assert.False(vm.IsApplying);
    }

    [Fact]
    public async Task SetAsync_WriteFailure_KeepsLastState()
    {
        // The write "succeeds" but the stored value never changes: the re-read
        // still returns 0, so IsOn stays false and nothing throws.
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadDword(It.IsAny<RegistryValue>())).Returns(0);
        registry.Setup(r => r.WriteDword(It.IsAny<RegistryValue>(), It.IsAny<int>())).Returns(true);

        var vm = new SettingRowViewModel(DwordSetting(), registry.Object);
        await vm.LoadAsync();

        await vm.SetAsync(true);

        registry.Verify(r => r.WriteDword(It.IsAny<RegistryValue>(), 1), Times.Once);
        Assert.False(vm.IsOn);
        Assert.False(vm.IsApplying);
    }
}
