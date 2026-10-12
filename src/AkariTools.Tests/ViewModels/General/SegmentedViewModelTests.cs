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
    public async Task ManualUpdates_Load_Failure_SetsError()
    {
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadDword(It.IsAny<RegistryValue>())).Throws<InvalidOperationException>();

        var vm = new ManualUpdatesViewModel(registry.Object);
        await vm.LoadAsync();

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
    public async Task PrintService_Load_Failure_SetsError()
    {
        var serviceControl = new Mock<IServiceControl>();
        serviceControl.Setup(s => s.GetStartMode(It.IsAny<string>())).Throws<InvalidOperationException>();

        var vm = new PrintServiceViewModel(serviceControl.Object);
        await vm.LoadAsync();

        Assert.NotNull(vm.LastError);
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

    private const string GameBarPath = @"SOFTWARE\Microsoft\GameBar";
    private const string GameDvrPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR";
    private const string GameDvrPolicyPath = @"SOFTWARE\Policies\Microsoft\Windows\GameDVR";
    private const string GameDvrPolicyManagerPath = @"SOFTWARE\Microsoft\PolicyManager\default\ApplicationManagement\AllowGameDVR";
    private const string PresenceWriterPath = @"SOFTWARE\Microsoft\WindowsRuntime\ActivatableClassId\Windows.Gaming.GameBar.PresenceServer.Internal.PresenceWriter";
    private const string MsGamebarPath = "ms-gamebar";

    [Fact]
    public async Task GameBar_Disable_WritesAllEightValues()
    {
        // Disable writes the §5.1 set (ev_c44a2bd): 7 DWORDs + 3 ms-gamebar
        // strings; enable deletes each value.
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadDword(It.Is<RegistryValue>(v =>
            v.Hive == RegistryHive.LocalMachine
            && v.Path == GameDvrPolicyPath
            && v.Name == "AllowGameDVR"))).Returns(2);
        registry.Setup(r => r.WriteDword(It.IsAny<RegistryValue>(), It.IsAny<int>())).Returns(true);
        registry.Setup(r => r.WriteString(It.IsAny<RegistryValue>(), It.IsAny<string>())).Returns(true);
        registry.Setup(r => r.DeleteValue(It.IsAny<RegistryValue>())).Returns(true);

        var vm = new GameBarViewModel(registry.Object);
        await vm.LoadAsync();
        Assert.Equal(1, vm.SelectedIndex);

        // Re-read after the writes reports the policy DWORD 0 = Disabled.
        registry.Setup(r => r.ReadDword(It.Is<RegistryValue>(v =>
            v.Hive == RegistryHive.LocalMachine
            && v.Path == GameDvrPolicyPath
            && v.Name == "AllowGameDVR"))).Returns(0);
        await vm.ApplyAsync(0);

        registry.Verify(r => r.WriteDword(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.CurrentUser && v.Path == GameBarPath && v.Name == "GamePanelStartupTipIndex"), 3), Times.Once);
        registry.Verify(r => r.WriteDword(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.CurrentUser && v.Path == GameBarPath && v.Name == "ShowStartupPanel"), 0), Times.Once);
        registry.Verify(r => r.WriteDword(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.CurrentUser && v.Path == GameBarPath && v.Name == "UseNexusForGameBarEnabled"), 0), Times.Once);
        registry.Verify(r => r.WriteDword(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.CurrentUser && v.Path == GameDvrPath && v.Name == "AppCaptureEnabled"), 0), Times.Once);
        registry.Verify(r => r.WriteDword(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.LocalMachine && v.Path == GameDvrPolicyPath && v.Name == "AllowGameDVR"), 0), Times.Once);
        registry.Verify(r => r.WriteDword(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.LocalMachine && v.Path == GameDvrPolicyManagerPath && v.Name == "value"), 0), Times.Once);
        registry.Verify(r => r.WriteDword(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.LocalMachine && v.Path == PresenceWriterPath && v.Name == "ActivationType"), 0), Times.Once);
        registry.Verify(r => r.WriteString(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.ClassesRoot && v.Path == MsGamebarPath && v.Name == string.Empty), "URL:ms-gamebar"), Times.Once);
        registry.Verify(r => r.WriteString(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.ClassesRoot && v.Path == MsGamebarPath && v.Name == "URL Protocol"), string.Empty), Times.Once);
        registry.Verify(r => r.WriteString(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.ClassesRoot && v.Path == MsGamebarPath && v.Name == "NoOpenWith"), string.Empty), Times.Once);
        Assert.Equal(0, vm.SelectedIndex);
        Assert.Null(vm.LastError);

        // Enable deletes each value in the same order.
        registry.Setup(r => r.ReadDword(It.Is<RegistryValue>(v =>
            v.Hive == RegistryHive.LocalMachine
            && v.Path == GameDvrPolicyPath
            && v.Name == "AllowGameDVR"))).Returns(2);
        await vm.ApplyAsync(1);

        registry.Verify(r => r.DeleteValue(It.Is<RegistryValue>(v =>
            v.Hive == RegistryHive.CurrentUser && v.Path == GameBarPath && v.Name == "GamePanelStartupTipIndex")), Times.Once);
        registry.Verify(r => r.DeleteValue(It.Is<RegistryValue>(v =>
            v.Hive == RegistryHive.CurrentUser && v.Path == GameBarPath && v.Name == "ShowStartupPanel")), Times.Once);
        registry.Verify(r => r.DeleteValue(It.Is<RegistryValue>(v =>
            v.Hive == RegistryHive.CurrentUser && v.Path == GameBarPath && v.Name == "UseNexusForGameBarEnabled")), Times.Once);
        registry.Verify(r => r.DeleteValue(It.Is<RegistryValue>(v =>
            v.Hive == RegistryHive.CurrentUser && v.Path == GameDvrPath && v.Name == "AppCaptureEnabled")), Times.Once);
        registry.Verify(r => r.DeleteValue(It.Is<RegistryValue>(v =>
            v.Hive == RegistryHive.LocalMachine && v.Path == GameDvrPolicyPath && v.Name == "AllowGameDVR")), Times.Once);
        registry.Verify(r => r.DeleteValue(It.Is<RegistryValue>(v =>
            v.Hive == RegistryHive.LocalMachine && v.Path == GameDvrPolicyManagerPath && v.Name == "value")), Times.Once);
        registry.Verify(r => r.DeleteValue(It.Is<RegistryValue>(v =>
            v.Hive == RegistryHive.LocalMachine && v.Path == PresenceWriterPath && v.Name == "ActivationType")), Times.Once);
        registry.Verify(r => r.DeleteValue(It.Is<RegistryValue>(v =>
            v.Hive == RegistryHive.ClassesRoot && v.Path == MsGamebarPath && v.Name == string.Empty)), Times.Once);
        registry.Verify(r => r.DeleteValue(It.Is<RegistryValue>(v =>
            v.Hive == RegistryHive.ClassesRoot && v.Path == MsGamebarPath && v.Name == "URL Protocol")), Times.Once);
        registry.Verify(r => r.DeleteValue(It.Is<RegistryValue>(v =>
            v.Hive == RegistryHive.ClassesRoot && v.Path == MsGamebarPath && v.Name == "NoOpenWith")), Times.Once);
        Assert.Equal(1, vm.SelectedIndex);
        Assert.Null(vm.LastError);
    }

    [Fact]
    public async Task GameBar_Load_MapsAllowGameDVR()
    {
        // Policy DWORD 0 = Disabled (index 0); anything else or missing = Enabled (index 1).
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadDword(It.IsAny<RegistryValue>())).Returns(0);

        var vm = new GameBarViewModel(registry.Object);
        await vm.LoadAsync();
        Assert.Equal(0, vm.SelectedIndex);

        registry.Setup(r => r.ReadDword(It.IsAny<RegistryValue>())).Returns(1);
        await vm.LoadAsync();
        Assert.Equal(1, vm.SelectedIndex);

        registry.Setup(r => r.ReadDword(It.IsAny<RegistryValue>())).Returns((int?)null);
        await vm.LoadAsync();
        Assert.Equal(1, vm.SelectedIndex);
    }

    [Fact]
    public async Task Apply_PartialFailure_RevertsIndex()
    {
        // The 5th sequential write (AllowGameDVR) fails: the index reverts,
        // an error surfaces, and no further writes run (no ms-gamebar strings).
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadDword(It.IsAny<RegistryValue>())).Returns(2);
        var writes = 0;
        registry.Setup(r => r.WriteDword(It.IsAny<RegistryValue>(), It.IsAny<int>()))
            .Returns(() => ++writes < 5);

        var vm = new GameBarViewModel(registry.Object);
        await vm.LoadAsync();
        Assert.Equal(1, vm.SelectedIndex);

        await vm.ApplyAsync(0);

        Assert.Equal(5, writes);
        Assert.Equal(1, vm.SelectedIndex);
        Assert.NotNull(vm.LastError);
        registry.Verify(r => r.WriteString(It.IsAny<RegistryValue>(), It.IsAny<string>()), Times.Never);
    }

    private const string WpnServicePath = @"SYSTEM\CurrentControlSet\Services\WpnService";
    private const string ConsentStorePath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userNotificationListener";
    private const string PushNotificationsPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\PushNotifications";
    private const string PushPolicyPath = @"SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\PushNotifications";
    private const string ExplorerAdvancedPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string NotificationSettingsPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings";

    [Fact]
    public async Task Push_Disable_WritesSetAndStopsService()
    {
        // Disable writes the §5.1 set (ev_bb40955) incl. WpnService Start=4
        // (service Start query nonzero) and stops WpnService.
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadDword(It.Is<RegistryValue>(v =>
            v.Hive == RegistryHive.LocalMachine && v.Path == WpnServicePath && v.Name == "Start"))).Returns(2);
        registry.Setup(r => r.ReadDword(It.Is<RegistryValue>(v =>
            v.Hive == RegistryHive.LocalMachine && v.Path == PushNotificationsPath && v.Name == "ToastEnabled"))).Returns(1);
        registry.Setup(r => r.WriteDword(It.IsAny<RegistryValue>(), It.IsAny<int>())).Returns(true);
        registry.Setup(r => r.WriteString(It.IsAny<RegistryValue>(), It.IsAny<string>())).Returns(true);

        var serviceControl = new Mock<IServiceControl>();
        serviceControl.Setup(s => s.StopService(It.IsAny<string>())).Returns(true);
        serviceControl.Setup(s => s.StartService(It.IsAny<string>())).Returns(true);

        var vm = new PushNotificationsViewModel(registry.Object, serviceControl.Object);
        await vm.LoadAsync();
        Assert.Equal(1, vm.SelectedIndex);

        // Re-read after the writes reports ToastEnabled 0 = Disabled.
        registry.Setup(r => r.ReadDword(It.Is<RegistryValue>(v =>
            v.Hive == RegistryHive.LocalMachine && v.Path == PushNotificationsPath && v.Name == "ToastEnabled"))).Returns(0);
        await vm.ApplyAsync(0);

        registry.Verify(r => r.WriteDword(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.LocalMachine && v.Path == WpnServicePath && v.Name == "Start"), 4), Times.Once);
        registry.Verify(r => r.WriteString(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.CurrentUser && v.Path == ConsentStorePath && v.Name == "Value"), "Deny"), Times.Once);
        registry.Verify(r => r.WriteDword(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.LocalMachine && v.Path == PushNotificationsPath && v.Name == "ToastEnabled"), 0), Times.Once);
        registry.Verify(r => r.WriteDword(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.LocalMachine && v.Path == PushPolicyPath && v.Name == "NoCloudApplicationNotification"), 1), Times.Once);
        registry.Verify(r => r.WriteDword(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.CurrentUser && v.Path == ExplorerAdvancedPath && v.Name == "ShowSyncProviderNotifications"), 0), Times.Once);
        registry.Verify(r => r.WriteDword(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.CurrentUser && v.Path == NotificationSettingsPath && v.Name == "NOC_GLOBAL_SETTING_ALLOW_NOTIFICATION_SOUND"), 0), Times.Once);
        registry.Verify(r => r.WriteDword(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.CurrentUser && v.Path == NotificationSettingsPath && v.Name == "NOC_GLOBAL_SETTING_ALLOW_CRITICAL_TOASTS_ABOVE_LOCK"), 0), Times.Once);
        registry.Verify(r => r.WriteDword(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.CurrentUser && v.Path == NotificationSettingsPath && v.Name == "NOC_GLOBAL_SETTING_ALLOW_TOASTS_ABOVE_LOCK"), 0), Times.Once);
        serviceControl.Verify(s => s.StopService("WpnService"), Times.Once);
        Assert.Equal(0, vm.SelectedIndex);
        Assert.Null(vm.LastError);
    }

    [Fact]
    public async Task Push_Enable_DeletesNocValues()
    {
        // Enable writes actives and deletes the three NOC_* values.
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadDword(It.Is<RegistryValue>(v =>
            v.Hive == RegistryHive.LocalMachine && v.Path == PushNotificationsPath && v.Name == "ToastEnabled"))).Returns(0);
        registry.Setup(r => r.WriteDword(It.IsAny<RegistryValue>(), It.IsAny<int>())).Returns(true);
        registry.Setup(r => r.WriteString(It.IsAny<RegistryValue>(), It.IsAny<string>())).Returns(true);
        registry.Setup(r => r.DeleteValue(It.IsAny<RegistryValue>())).Returns(true);

        var serviceControl = new Mock<IServiceControl>();
        serviceControl.Setup(s => s.StopService(It.IsAny<string>())).Returns(true);
        serviceControl.Setup(s => s.StartService(It.IsAny<string>())).Returns(true);

        var vm = new PushNotificationsViewModel(registry.Object, serviceControl.Object);
        await vm.LoadAsync();
        Assert.Equal(0, vm.SelectedIndex);

        // Re-read after the writes reports ToastEnabled 1 = Enabled.
        registry.Setup(r => r.ReadDword(It.Is<RegistryValue>(v =>
            v.Hive == RegistryHive.LocalMachine && v.Path == PushNotificationsPath && v.Name == "ToastEnabled"))).Returns(1);
        await vm.ApplyAsync(1);

        registry.Verify(r => r.WriteDword(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.LocalMachine && v.Path == WpnServicePath && v.Name == "Start"), 2), Times.Once);
        registry.Verify(r => r.WriteString(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.CurrentUser && v.Path == ConsentStorePath && v.Name == "Value"), "Allow"), Times.Once);
        registry.Verify(r => r.WriteDword(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.LocalMachine && v.Path == PushNotificationsPath && v.Name == "ToastEnabled"), 1), Times.Once);
        registry.Verify(r => r.WriteDword(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.LocalMachine && v.Path == PushPolicyPath && v.Name == "NoCloudApplicationNotification"), 0), Times.Once);
        registry.Verify(r => r.WriteDword(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.CurrentUser && v.Path == ExplorerAdvancedPath && v.Name == "ShowSyncProviderNotifications"), 1), Times.Once);
        registry.Verify(r => r.DeleteValue(It.Is<RegistryValue>(v =>
            v.Hive == RegistryHive.CurrentUser && v.Path == NotificationSettingsPath && v.Name == "NOC_GLOBAL_SETTING_ALLOW_NOTIFICATION_SOUND")), Times.Once);
        registry.Verify(r => r.DeleteValue(It.Is<RegistryValue>(v =>
            v.Hive == RegistryHive.CurrentUser && v.Path == NotificationSettingsPath && v.Name == "NOC_GLOBAL_SETTING_ALLOW_CRITICAL_TOASTS_ABOVE_LOCK")), Times.Once);
        registry.Verify(r => r.DeleteValue(It.Is<RegistryValue>(v =>
            v.Hive == RegistryHive.CurrentUser && v.Path == NotificationSettingsPath && v.Name == "NOC_GLOBAL_SETTING_ALLOW_TOASTS_ABOVE_LOCK")), Times.Once);
        serviceControl.Verify(s => s.StartService("WpnService"), Times.Once);
        Assert.Equal(1, vm.SelectedIndex);
        Assert.Null(vm.LastError);
    }

    [Fact]
    public async Task Push_Load_MapsToastEnabled()
    {
        // ToastEnabled 0 = Disabled (index 0); anything else or missing = Enabled (index 1).
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadDword(It.IsAny<RegistryValue>())).Returns(0);
        var serviceControl = new Mock<IServiceControl>();

        var vm = new PushNotificationsViewModel(registry.Object, serviceControl.Object);
        await vm.LoadAsync();
        Assert.Equal(0, vm.SelectedIndex);

        registry.Setup(r => r.ReadDword(It.IsAny<RegistryValue>())).Returns(1);
        await vm.LoadAsync();
        Assert.Equal(1, vm.SelectedIndex);

        registry.Setup(r => r.ReadDword(It.IsAny<RegistryValue>())).Returns((int?)null);
        await vm.LoadAsync();
        Assert.Equal(1, vm.SelectedIndex);
    }

    [Fact]
    public async Task Push_Disable_SkipsStartWriteWhenServiceAbsent()
    {
        // A zero/missing WpnService Start query means the service is absent:
        // Start=4 is skipped but the rest of the set still writes.
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadDword(It.Is<RegistryValue>(v =>
            v.Hive == RegistryHive.LocalMachine && v.Path == WpnServicePath && v.Name == "Start"))).Returns((int?)null);
        registry.Setup(r => r.ReadDword(It.Is<RegistryValue>(v =>
            v.Hive == RegistryHive.LocalMachine && v.Path == PushNotificationsPath && v.Name == "ToastEnabled"))).Returns(1);
        registry.Setup(r => r.WriteDword(It.IsAny<RegistryValue>(), It.IsAny<int>())).Returns(true);
        registry.Setup(r => r.WriteString(It.IsAny<RegistryValue>(), It.IsAny<string>())).Returns(true);

        var serviceControl = new Mock<IServiceControl>();
        serviceControl.Setup(s => s.StopService(It.IsAny<string>())).Returns(true);

        var vm = new PushNotificationsViewModel(registry.Object, serviceControl.Object);
        await vm.LoadAsync();
        Assert.Equal(1, vm.SelectedIndex);

        // Re-read after the writes reports ToastEnabled 0 = Disabled.
        registry.Setup(r => r.ReadDword(It.Is<RegistryValue>(v =>
            v.Hive == RegistryHive.LocalMachine && v.Path == PushNotificationsPath && v.Name == "ToastEnabled"))).Returns(0);
        await vm.ApplyAsync(0);

        registry.Verify(r => r.WriteDword(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.LocalMachine && v.Path == WpnServicePath && v.Name == "Start"),
            It.IsAny<int>()), Times.Never);
        registry.Verify(r => r.WriteDword(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.LocalMachine && v.Path == PushNotificationsPath && v.Name == "ToastEnabled"), 0), Times.Once);
        Assert.Equal(0, vm.SelectedIndex);
        Assert.Null(vm.LastError);
    }
}
