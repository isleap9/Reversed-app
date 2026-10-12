using AkariTools.App.ViewModels.General;
using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using Moq;
using System.ServiceProcess;
using Xunit;

namespace AkariTools.Tests.ViewModels.General;

public sealed class SegmentedViewModelTests
{
    private const string UpdatesPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate";
    private const string UpdatesName = "SetDisableUXWUAccess";

    [Fact]
    public async Task ManualUpdates_Load_MapsInvertedValue()
    {
        // DWORD 0 = UX access allowed = Enabled (index 1); 1 or missing = Disabled (index 0).
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadDword(It.IsAny<RegistryValue>())).Returns(0);

        var vm = new ManualUpdatesViewModel(registry.Object);
        await vm.LoadAsync();
        Assert.Equal(1, vm.SelectedIndex);

        registry.Setup(r => r.ReadDword(It.IsAny<RegistryValue>())).Returns(1);
        await vm.LoadAsync();
        Assert.Equal(0, vm.SelectedIndex);

        registry.Setup(r => r.ReadDword(It.IsAny<RegistryValue>())).Returns((int?)null);
        await vm.LoadAsync();
        Assert.Equal(0, vm.SelectedIndex);
    }

    [Fact]
    public async Task ManualUpdates_Apply_WritesInvertedDword()
    {
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadDword(It.IsAny<RegistryValue>())).Returns(1);
        registry.Setup(r => r.WriteDword(It.IsAny<RegistryValue>(), It.IsAny<int>())).Returns(true);

        var vm = new ManualUpdatesViewModel(registry.Object);
        await vm.LoadAsync();
        Assert.Equal(0, vm.SelectedIndex);

        // Re-read after the write reports Enabled.
        registry.Setup(r => r.ReadDword(It.IsAny<RegistryValue>())).Returns(0);
        await vm.ApplyAsync(1);

        registry.Verify(r => r.WriteDword(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.LocalMachine
                && v.Path == UpdatesPath
                && v.Name == UpdatesName),
            0), Times.Once);
        Assert.Equal(1, vm.SelectedIndex);
        Assert.Null(vm.LastError);
    }

    [Fact]
    public async Task ManualUpdates_Apply_Failure_RevertsAndReports()
    {
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadDword(It.IsAny<RegistryValue>())).Returns(1);
        registry.Setup(r => r.WriteDword(It.IsAny<RegistryValue>(), It.IsAny<int>())).Returns(false);

        var vm = new ManualUpdatesViewModel(registry.Object);
        await vm.LoadAsync();
        await vm.ApplyAsync(1);

        Assert.Equal(0, vm.SelectedIndex);
        Assert.NotNull(vm.LastError);
    }

    [Fact]
    public async Task Segmented_SameIndex_SkipsWrite()
    {
        // A re-fired pick carrying the value already shown (e.g. the
        // programmatic revert echo) must not write.
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadDword(It.IsAny<RegistryValue>())).Returns(0);

        var vm = new ManualUpdatesViewModel(registry.Object);
        await vm.LoadAsync();
        Assert.Equal(1, vm.SelectedIndex);

        await vm.SelectAsync(1);

        registry.Verify(r => r.WriteDword(It.IsAny<RegistryValue>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task PrintService_ManualMode_DisplaysOnWithNote()
    {
        // Pre-existing Manual start mode displays as On with a "Manual" note.
        var serviceControl = new Mock<IServiceControl>();
        serviceControl.Setup(s => s.GetStartMode("Spooler")).Returns(ServiceStartMode.Manual);

        var vm = new PrintServiceViewModel(serviceControl.Object);
        await vm.LoadAsync();

        Assert.Equal(1, vm.SelectedIndex);
        Assert.Equal("Manual", vm.Note);
    }

    [Fact]
    public async Task PrintService_Apply_WritesAutomaticAndStarts()
    {
        var serviceControl = new Mock<IServiceControl>();
        serviceControl.Setup(s => s.GetStartMode("Spooler")).Returns(ServiceStartMode.Disabled);
        serviceControl.Setup(s => s.SetStartMode(It.IsAny<string>(), It.IsAny<ServiceStartMode>())).Returns(true);
        serviceControl.Setup(s => s.StartService(It.IsAny<string>())).Returns(true);
        serviceControl.Setup(s => s.StopService(It.IsAny<string>())).Returns(true);

        var vm = new PrintServiceViewModel(serviceControl.Object);
        await vm.LoadAsync();
        Assert.Equal(0, vm.SelectedIndex);

        // Re-read after the write reports Automatic.
        serviceControl.Setup(s => s.GetStartMode("Spooler")).Returns(ServiceStartMode.Automatic);
        await vm.ApplyAsync(1);

        serviceControl.Verify(s => s.SetStartMode("Spooler", ServiceStartMode.Automatic), Times.Once);
        serviceControl.Verify(s => s.StartService("Spooler"), Times.Once);
        Assert.Equal(1, vm.SelectedIndex);
        Assert.Null(vm.Note);
        Assert.Null(vm.LastError);
    }

    [Fact]
    public async Task PrintService_Apply_Failure_RevertsAndReports()
    {
        var serviceControl = new Mock<IServiceControl>();
        serviceControl.Setup(s => s.GetStartMode("Spooler")).Returns(ServiceStartMode.Disabled);
        serviceControl.Setup(s => s.SetStartMode(It.IsAny<string>(), It.IsAny<ServiceStartMode>())).Returns(false);

        var vm = new PrintServiceViewModel(serviceControl.Object);
        await vm.LoadAsync();
        await vm.ApplyAsync(1);

        Assert.Equal(0, vm.SelectedIndex);
        Assert.NotNull(vm.LastError);
        serviceControl.Verify(s => s.StartService(It.IsAny<string>()), Times.Never);
    }
}
