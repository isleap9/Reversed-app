using AkariTools.App.ViewModels.General;
using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using Moq;
using Xunit;

namespace AkariTools.Tests.ViewModels.General;

// Context Menu page: seven KeyValues rows over shell extension registry keys
// (task-7 brief, binary evidence 2026-10-11). Each row probes one string
// value for state and writes a value set on enable / deletes key trees on
// disable. Key paths pinned here to catch wrong-path bugs.
public sealed class ContextMenuViewModelTests
{
    private const string ModernMenuProbePath =
        @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32";
    private const string TakeOwnershipPath = @"Directory\shell\runas";
    private const string TakeOwnershipCommandPath = @"Directory\shell\runas\command";
    private const string SendToPath = @"AllFilesystemObjects\shellex\ContextMenuHandlers\SendTo";
    private const string UserLibrarySendToPath = @"UserLibraryFolder\shellex\ContextMenuHandlers\SendTo";
    private const string Ps1ShellNewPath = @".ps1\ShellNew";
    private const string BatShellNewPath = @".bat\ShellNew";
    private const string RegShellNewPath = @".reg\ShellNew";
    private const string ViewsKey1 = @"Software\Classes\CLSID\{2aa9162e-c906-4dd9-ad0b-3d24a8eef5a0}";
    private const string ViewsKey2 = @"Software\Classes\CLSID\{6480100b-5a83-4d1e-9f69-8ae5a88e9a33}";

    private const string TakeOwnershipCommand =
        "cmd.exe /c takeown /f \"%1\" /r /d y && icacls \"%1\" /grant *S-1-5-32-544:F /t";

    private static Mock<IRegistrySettingService> QuietRegistry()
    {
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadString(It.IsAny<RegistryValue>())).Returns((string?)null);
        return registry;
    }

    [Fact]
    public async Task Load_BuildsSevenRows()
    {
        var vm = new ContextMenuViewModel(QuietRegistry().Object);
        await vm.LoadAsync();

        Assert.Equal(7, vm.Rows.Count);
        Assert.Equal(
            new[] { "ModernMenu", "TakeOwnership", "SendTo", "NewPs1", "NewBat", "NewReg", "ModernViews" },
            vm.Rows.Select(r => r.Setting.Id));

        // All rows are KeyValues (task-7 brief).
        foreach (var row in vm.Rows)
            Assert.Equal(ToggleKind.KeyValues, row.Setting.Kind);
    }

    [Fact]
    public async Task Load_MapsProbeState()
    {
        var registry = QuietRegistry();
        // Probes returning non-null (even empty string) mean On; null means Off.
        registry.Setup(r => r.ReadString(It.Is<RegistryValue>(
                v => v.Hive == RegistryHive.ClassesRoot && v.Path == TakeOwnershipPath && v.Name == "")))
            .Returns("");
        registry.Setup(r => r.ReadString(It.Is<RegistryValue>(
                v => v.Hive == RegistryHive.CurrentUser && v.Path == ModernMenuProbePath && v.Name == "")))
            .Returns("present");

        var vm = new ContextMenuViewModel(registry.Object);
        await vm.LoadAsync();

        Assert.True(vm.Rows[0].IsOn);   // ModernMenu: probe non-null
        Assert.True(vm.Rows[1].IsOn);   // TakeOwnership: probe empty-string (non-null)
        Assert.False(vm.Rows[2].IsOn);  // SendTo: probe null
    }

    [Fact]
    public async Task Apply_RereadsState()
    {
        // Review Focus #5: after writing, the VM re-reads to confirm.
        var registry = QuietRegistry();
        var probeValue = (string?)null;
        registry.Setup(r => r.ReadString(It.Is<RegistryValue>(
                v => v.Hive == RegistryHive.ClassesRoot && v.Path == TakeOwnershipPath && v.Name == "")))
            .Returns(() => probeValue);

        var vm = new ContextMenuViewModel(registry.Object);
        await vm.LoadAsync();
        Assert.False(vm.Rows[1].IsOn); // TakeOwnership

        await vm.Rows[1].SetAsync(true);

        // All 5 TakeOwnership writes, exact paths/names/data.
        registry.Verify(r => r.WriteString(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.ClassesRoot && v.Path == TakeOwnershipPath && v.Name == ""),
            "Take Ownership"), Times.Once);
        registry.Verify(r => r.WriteString(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.ClassesRoot && v.Path == TakeOwnershipPath && v.Name == "HasLUAShield"),
            ""), Times.Once);
        registry.Verify(r => r.WriteString(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.ClassesRoot && v.Path == TakeOwnershipPath && v.Name == "NoWorkingDirectory"),
            ""), Times.Once);
        registry.Verify(r => r.WriteString(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.ClassesRoot && v.Path == TakeOwnershipCommandPath && v.Name == ""),
            TakeOwnershipCommand), Times.Once);
        registry.Verify(r => r.WriteString(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.ClassesRoot && v.Path == TakeOwnershipCommandPath && v.Name == "IsolatedCommand"),
            TakeOwnershipCommand), Times.Once);

        // Write "took effect": probe now non-null → re-read shows On.
        probeValue = "Take Ownership";
        await vm.LoadAsync();
        Assert.True(vm.Rows[1].IsOn);

        // Disable → the runas tree is deleted, probe null → Off.
        await vm.Rows[1].SetAsync(false);
        registry.Verify(r => r.DeleteKey(RegistryHive.ClassesRoot, TakeOwnershipPath), Times.Once);
        probeValue = null;
        await vm.LoadAsync();
        Assert.False(vm.Rows[1].IsOn);
    }

    [Fact]
    public async Task ModernViews_WritesBothClsids()
    {
        var registry = QuietRegistry();
        var probeValue = (string?)null;
        registry.Setup(r => r.ReadString(It.Is<RegistryValue>(
                v => v.Hive == RegistryHive.CurrentUser && v.Path == ViewsKey1 && v.Name == "")))
            .Returns(() => probeValue);

        var vm = new ContextMenuViewModel(registry.Object);
        await vm.LoadAsync();
        Assert.False(vm.Rows[6].IsOn); // ModernViews

        await vm.Rows[6].SetAsync(true); // ModernViews

        // 6 writes: default + InprocServer32 default + ThreadingModel per CLSID,
        // covering BOTH the {2aa9162e} and the corrected {6480100b} GUIDs.
        foreach (var key in new[] { ViewsKey1, ViewsKey2 })
        {
            registry.Verify(r => r.WriteString(
                It.Is<RegistryValue>(v => v.Hive == RegistryHive.CurrentUser && v.Path == key && v.Name == ""),
                ""), Times.Once);
            registry.Verify(r => r.WriteString(
                It.Is<RegistryValue>(v => v.Hive == RegistryHive.CurrentUser && v.Path == key + @"\InprocServer32" && v.Name == ""),
                ""), Times.Once);
            registry.Verify(r => r.WriteString(
                It.Is<RegistryValue>(v => v.Hive == RegistryHive.CurrentUser && v.Path == key + @"\InprocServer32" && v.Name == "ThreadingModel"),
                "Apartment"), Times.Once);
        }

        // Flip the probe so the re-read shows On; otherwise the same-value
        // guard would swallow the disable below.
        probeValue = "";
        await vm.LoadAsync();
        Assert.True(vm.Rows[6].IsOn);

        await vm.Rows[6].SetAsync(false);
        registry.Verify(r => r.DeleteKey(RegistryHive.CurrentUser, ViewsKey1), Times.Once);
        registry.Verify(r => r.DeleteKey(RegistryHive.CurrentUser, ViewsKey2), Times.Once);
    }

    [Fact]
    public async Task NewBat_UsesExpandString()
    {
        var registry = QuietRegistry();
        var vm = new ContextMenuViewModel(registry.Object);
        await vm.LoadAsync();

        await vm.Rows[4].SetAsync(true); // NewBat

        // NullFile is a plain REG_SZ; ItemName must be REG_EXPAND_SZ.
        registry.Verify(r => r.WriteString(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.ClassesRoot && v.Path == BatShellNewPath && v.Name == "NullFile"),
            ""), Times.Once);
        registry.Verify(r => r.WriteExpandString(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.ClassesRoot && v.Path == BatShellNewPath && v.Name == "ItemName"),
            @"%systemroot%\System32\acppage.dll,-6002"), Times.Once);
        registry.Verify(r => r.WriteString(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.ClassesRoot && v.Path == BatShellNewPath && v.Name == "ItemName"),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendTo_WritesBothHandlers()
    {
        var registry = QuietRegistry();
        var probeValue = (string?)null;
        registry.Setup(r => r.ReadString(It.Is<RegistryValue>(
                v => v.Hive == RegistryHive.ClassesRoot && v.Path == SendToPath && v.Name == "")))
            .Returns(() => probeValue);

        var vm = new ContextMenuViewModel(registry.Object);
        await vm.LoadAsync();
        Assert.False(vm.Rows[2].IsOn); // SendTo

        await vm.Rows[2].SetAsync(true); // SendTo

        const string sendToGuid = "{7BA4C740-9E81-11CF-99D3-00AA004AE837}";
        registry.Verify(r => r.WriteString(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.ClassesRoot && v.Path == SendToPath && v.Name == ""),
            sendToGuid), Times.Once);
        registry.Verify(r => r.WriteString(
            It.Is<RegistryValue>(v => v.Hive == RegistryHive.ClassesRoot && v.Path == UserLibrarySendToPath && v.Name == ""),
            sendToGuid), Times.Once);

        // Flip the probe so the re-read shows On; otherwise the same-value
        // guard would swallow the disable below.
        probeValue = sendToGuid;
        await vm.LoadAsync();
        Assert.True(vm.Rows[2].IsOn);

        await vm.Rows[2].SetAsync(false);
        registry.Verify(r => r.DeleteKey(RegistryHive.ClassesRoot, SendToPath), Times.Once);
        registry.Verify(r => r.DeleteKey(RegistryHive.ClassesRoot, UserLibrarySendToPath), Times.Once);
    }
}
