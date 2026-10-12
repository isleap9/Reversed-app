using AkariTools.App.ViewModels.General;
using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using Moq;
using System.ServiceProcess;
using Xunit;

namespace AkariTools.Tests.ViewModels.General;

public sealed class GeneralViewModelTests
{
    private readonly Mock<IServiceControl> _serviceControl = new();
    private readonly Mock<IBcdService> _bcd = new();
    private readonly Mock<IRegistrySettingService> _registry = new();

    private const string RdpFlagPath = @"SYSTEM\CurrentControlSet\Control\Terminal Server";
    private const string RdpFlagName = "fDenyTSConnections";

    private GeneralViewModel CreateViewModel() => new(
        _serviceControl.Object, _bcd.Object, _registry.Object);

    private RegistryValue RdpFlag() => new(RegistryHive.LocalMachine, RdpFlagPath, RdpFlagName);

    [Fact]
    public async Task Load_MapsServiceStates()
    {
        _bcd.Setup(b => b.ReadSetting(It.IsAny<string>())).Returns("Auto");
        _serviceControl.Setup(s => s.GetStartMode("Spooler")).Returns(ServiceStartMode.Automatic);
        _registry.Setup(r => r.ReadDword(It.Is<RegistryValue>(v => v.Name == RdpFlagName))).Returns(0);

        var vm = CreateViewModel();
        await vm.LoadAsync();

        Assert.True(vm.HyperVOn);
        Assert.True(vm.SpoolerOn);
        Assert.Null(vm.SpoolerNote);
        Assert.True(vm.RdpOn);
        Assert.Null(vm.LastError);
    }

    [Fact]
    public async Task HyperV_AbsentLineMeansOff()
    {
        _bcd.Setup(b => b.ReadSetting(It.IsAny<string>())).Returns((string?)null);
        _serviceControl.Setup(s => s.GetStartMode(It.IsAny<string>())).Returns((ServiceStartMode?)null);
        _registry.Setup(r => r.ReadDword(It.IsAny<RegistryValue>())).Returns((int?)null);

        var vm = CreateViewModel();
        await vm.LoadAsync();

        Assert.False(vm.HyperVOn);
        Assert.False(vm.SpoolerOn);
        Assert.False(vm.RdpOn);
    }

    [Fact]
    public async Task Load_HyperVValueComparedIgnoringCase()
    {
        _bcd.Setup(b => b.ReadSetting(It.IsAny<string>())).Returns("auto");

        var vm = CreateViewModel();
        await vm.LoadAsync();

        Assert.True(vm.HyperVOn);
    }

    [Fact]
    public async Task Load_ManualSpoolerDisplaysOnWithNote()
    {
        _bcd.Setup(b => b.ReadSetting(It.IsAny<string>())).Returns((string?)null);
        _serviceControl.Setup(s => s.GetStartMode("Spooler")).Returns(ServiceStartMode.Manual);

        var vm = CreateViewModel();
        await vm.LoadAsync();

        Assert.True(vm.SpoolerOn);
        Assert.Equal("Manual", vm.SpoolerNote);
    }

    [Fact]
    public async Task Load_RdpFlagOneMeansOff()
    {
        _bcd.Setup(b => b.ReadSetting(It.IsAny<string>())).Returns((string?)null);
        _registry.Setup(r => r.ReadDword(It.Is<RegistryValue>(v => v.Name == RdpFlagName))).Returns(1);

        var vm = CreateViewModel();
        await vm.LoadAsync();

        Assert.False(vm.RdpOn);
    }

    [Fact]
    public async Task ToggleService_ReportsFailure()
    {
        _serviceControl.Setup(s => s.SetStartMode(It.IsAny<string>(), It.IsAny<ServiceStartMode>())).Returns(false);

        var vm = CreateViewModel();
        await vm.SetSpoolerAsync(true);

        Assert.False(vm.SpoolerOn);
        Assert.NotNull(vm.LastError);
        _serviceControl.Verify(s => s.StartService(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SetHyperV_OnWritesAuto_OffWritesOff()
    {
        _bcd.Setup(b => b.Set(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var vm = CreateViewModel();
        await vm.SetHyperVAsync(true);

        _bcd.Verify(b => b.Set("hypervisorlaunchtype", "auto"), Times.Once);
        Assert.True(vm.HyperVOn);
        Assert.Null(vm.LastError);

        await vm.SetHyperVAsync(false);
        _bcd.Verify(b => b.Set("hypervisorlaunchtype", "off"), Times.Once);
        Assert.False(vm.HyperVOn);
    }

    [Fact]
    public async Task SetHyperV_WriteFailure_KeepsOffAndReports()
    {
        _bcd.Setup(b => b.Set(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var vm = CreateViewModel();
        await vm.SetHyperVAsync(true);

        Assert.False(vm.HyperVOn);
        Assert.NotNull(vm.LastError);
    }

    [Fact]
    public async Task SetSpooler_OnWritesAutomaticAndStarts_OffWritesDisabledAndStops()
    {
        _serviceControl.Setup(s => s.SetStartMode(It.IsAny<string>(), It.IsAny<ServiceStartMode>())).Returns(true);
        _serviceControl.Setup(s => s.StartService(It.IsAny<string>())).Returns(true);
        _serviceControl.Setup(s => s.StopService(It.IsAny<string>())).Returns(true);

        var vm = CreateViewModel();
        await vm.SetSpoolerAsync(true);

        _serviceControl.Verify(s => s.SetStartMode("Spooler", ServiceStartMode.Automatic), Times.Once);
        _serviceControl.Verify(s => s.StartService("Spooler"), Times.Once);
        Assert.True(vm.SpoolerOn);
        Assert.Null(vm.SpoolerNote);
        Assert.Null(vm.LastError);

        await vm.SetSpoolerAsync(false);
        _serviceControl.Verify(s => s.SetStartMode("Spooler", ServiceStartMode.Disabled), Times.Once);
        _serviceControl.Verify(s => s.StopService("Spooler"), Times.Once);
        Assert.False(vm.SpoolerOn);
    }

    [Fact]
    public async Task SetRdp_OnWritesZeroFlagAndAutomatic_OffWritesOneFlagAndDisabled()
    {
        _serviceControl.Setup(s => s.SetStartMode(It.IsAny<string>(), It.IsAny<ServiceStartMode>())).Returns(true);
        _serviceControl.Setup(s => s.StartService(It.IsAny<string>())).Returns(true);
        _serviceControl.Setup(s => s.StopService(It.IsAny<string>())).Returns(true);
        _registry.Setup(r => r.WriteDword(It.IsAny<RegistryValue>(), It.IsAny<int>())).Returns(true);

        var vm = CreateViewModel();
        await vm.SetRdpAsync(true);

        _registry.Verify(r => r.WriteDword(RdpFlag(), 0), Times.Once);
        _serviceControl.Verify(s => s.SetStartMode("TermService", ServiceStartMode.Automatic), Times.Once);
        _serviceControl.Verify(s => s.StartService("TermService"), Times.Once);
        Assert.True(vm.RdpOn);
        Assert.Null(vm.LastError);

        await vm.SetRdpAsync(false);
        _registry.Verify(r => r.WriteDword(RdpFlag(), 1), Times.Once);
        _serviceControl.Verify(s => s.SetStartMode("TermService", ServiceStartMode.Disabled), Times.Once);
        _serviceControl.Verify(s => s.StopService("TermService"), Times.Once);
        Assert.False(vm.RdpOn);
    }

    [Fact]
    public async Task SetRdp_WriteFailure_KeepsOffAndReports()
    {
        _registry.Setup(r => r.WriteDword(It.IsAny<RegistryValue>(), It.IsAny<int>())).Returns(false);
        _serviceControl.Setup(s => s.SetStartMode(It.IsAny<string>(), It.IsAny<ServiceStartMode>())).Returns(true);

        var vm = CreateViewModel();
        await vm.SetRdpAsync(true);

        Assert.False(vm.RdpOn);
        Assert.NotNull(vm.LastError);
        _serviceControl.Verify(s => s.SetStartMode(It.IsAny<string>(), It.IsAny<ServiceStartMode>()), Times.Never);
    }

    [Fact]
    public async Task Set_SameValue_SkipsWrite()
    {
        // Revert echoes (the switch snapping back after a failed apply) carry
        // the value already shown and must not write. The view binds OneWay,
        // so a user toggle always reaches here with a differing value.
        _bcd.Setup(b => b.Set(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var vm = CreateViewModel();
        await vm.SetHyperVAsync(false);

        _bcd.Verify(b => b.Set(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SetHyperV_Failure_ForcesRevertNotification()
    {
        // OneWay binding: the switch keeps the user's flipped state until the
        // VM pushes back. On failure the VM must raise a notification even
        // though the value is unchanged, or the switch lies.
        _bcd.Setup(b => b.Set(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var vm = CreateViewModel();
        var notified = new List<string?>();
        vm.PropertyChanged += (_, e) => notified.Add(e.PropertyName);

        await vm.SetHyperVAsync(true);

        Assert.False(vm.HyperVOn);
        Assert.Contains(nameof(GeneralViewModel.HyperVOn), notified);
    }

    [Fact]
    public async Task SetRdp_ModeFailure_RollsBackFlag()
    {
        // The flag is written first; if the service mode then fails, the flag
        // must be restored so the machine never disagrees with the display.
        _registry.Setup(r => r.WriteDword(It.IsAny<RegistryValue>(), It.IsAny<int>())).Returns(true);
        _serviceControl.Setup(s => s.SetStartMode(It.IsAny<string>(), It.IsAny<ServiceStartMode>())).Returns(false);

        var vm = CreateViewModel();
        await vm.SetRdpAsync(true);

        _registry.Verify(r => r.WriteDword(RdpFlag(), 0), Times.Once);
        _registry.Verify(r => r.WriteDword(RdpFlag(), 1), Times.Once);
        Assert.False(vm.RdpOn);
        Assert.NotNull(vm.LastError);
        _serviceControl.Verify(s => s.StartService(It.IsAny<string>()), Times.Never);
    }
}
