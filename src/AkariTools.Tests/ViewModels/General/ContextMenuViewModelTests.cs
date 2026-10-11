using AkariTools.App.ViewModels.General;
using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using Moq;
using Xunit;

namespace AkariTools.Tests.ViewModels.General;

public sealed class ContextMenuViewModelTests
{
    // Key paths from spec §7.3 — pinned here to catch wrong-path bugs.
    private const string TakeOwnershipPath = @"Directory\shell\runas";
    private const string SendToPath = @"AllFilesystemObjects\shellex\ContextMenuHandlers\SendTo";
    private const string ShellNewPs1Path = @".ps1\ShellNew";
    private const string ShellNewBatPath = @".bat\ShellNew";
    private const string ShellNewRegPath = @".reg\ShellNew";
    private const string ClassicMenuKey1Path =
        @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32";
    private const string ClassicMenuKey2Path =
        @"Software\Classes\CLSID\{6480400b-5a83-4d1e-9f69-8ae5a8eef5a0}\InprocServer32";

    [Fact]
    public async Task Load_BuildsExpectedRows()
    {
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.KeyExists(It.IsAny<RegistryHive>(), It.IsAny<string>())).Returns(false);

        var vm = new ContextMenuViewModel(registry.Object);
        await vm.LoadAsync();

        Assert.Equal(6, vm.Rows.Count);
        Assert.Equal(
            new[] { "TakeOwnership", "SendTo", "ShellNewPs1", "ShellNewBat", "ShellNewReg", "ClassicMenu" },
            vm.Rows.Select(r => r.Setting.Id));

        // All rows are KeyPresence (spec §7.3).
        foreach (var row in vm.Rows)
            Assert.Equal(ToggleKind.KeyPresence, row.Setting.Kind);
    }

    [Fact]
    public async Task Load_MapsKeyPresence()
    {
        var registry = new Mock<IRegistrySettingService>();
        // Take Ownership and Send To keys exist; others do not.
        registry.Setup(r => r.KeyExists(RegistryHive.ClassesRoot, TakeOwnershipPath)).Returns(true);
        registry.Setup(r => r.KeyExists(RegistryHive.ClassesRoot, SendToPath)).Returns(true);

        var vm = new ContextMenuViewModel(registry.Object);
        await vm.LoadAsync();

        Assert.True(vm.Rows[0].IsOn);   // TakeOwnership
        Assert.True(vm.Rows[1].IsOn);   // SendTo
        Assert.False(vm.Rows[2].IsOn);  // ShellNewPs1
        Assert.False(vm.Rows[3].IsOn);  // ShellNewBat
        Assert.False(vm.Rows[4].IsOn);  // ShellNewReg
        Assert.False(vm.Rows[5].IsOn);  // ClassicMenu
    }

    [Fact]
    public async Task ClassicMenu_SecondaryKeyRequired()
    {
        // ClassicMenu row: both keys must exist for IsOn=true (spec §12.5).
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.KeyExists(RegistryHive.CurrentUser, ClassicMenuKey1Path)).Returns(true);
        // SecondaryKey (key2) does not exist — row must be Off.

        var vm = new ContextMenuViewModel(registry.Object);
        await vm.LoadAsync();

        Assert.False(vm.Rows[5].IsOn);  // ClassicMenu: only one key exists
    }

    [Fact]
    public async Task ClassicMenu_BothKeysExist_IsOn()
    {
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.KeyExists(RegistryHive.CurrentUser, ClassicMenuKey1Path)).Returns(true);
        registry.Setup(r => r.KeyExists(RegistryHive.CurrentUser, ClassicMenuKey2Path)).Returns(true);

        var vm = new ContextMenuViewModel(registry.Object);
        await vm.LoadAsync();

        Assert.True(vm.Rows[5].IsOn);  // ClassicMenu: both keys exist
    }

    [Fact]
    public async Task Apply_RereadsState()
    {
        // Review Focus #5: after writing, the VM re-reads to confirm.
        // TakeOwnership: toggle On → CreateKey called → KeyExists returns true.
        var registry = new Mock<IRegistrySettingService>();
        var keyExisted = false;
        registry.Setup(r => r.KeyExists(RegistryHive.ClassesRoot, TakeOwnershipPath))
                .Returns(() => keyExisted);
        registry.Setup(r => r.CreateKey(RegistryHive.ClassesRoot, TakeOwnershipPath))
                .Callback(() => keyExisted = true);

        var vm = new ContextMenuViewModel(registry.Object);
        await vm.LoadAsync();

        Assert.False(vm.Rows[0].IsOn);
        await vm.Rows[0].SetAsync(true);

        Assert.True(vm.Rows[0].IsOn);  // Re-read confirmed the write
    }

    [Fact]
    public async Task Apply_ClassicMenu_CreatesBothKeys()
    {
        var registry = new Mock<IRegistrySettingService>();
        var created = new List<string>();

        registry.Setup(r => r.KeyExists(It.IsAny<RegistryHive>(), It.IsAny<string>())).Returns(false);
        registry.Setup(r => r.CreateKey(RegistryHive.CurrentUser, ClassicMenuKey1Path))
                .Callback(() => created.Add("key1"));
        registry.Setup(r => r.CreateKey(RegistryHive.CurrentUser, ClassicMenuKey2Path))
                .Callback(() => created.Add("key2"));

        var vm = new ContextMenuViewModel(registry.Object);
        await vm.LoadAsync();

        Assert.False(vm.Rows[5].IsOn);
        await vm.Rows[5].SetAsync(true);

        Assert.Equal(2, created.Count);
        Assert.Contains("key1", created);
        Assert.Contains("key2", created);
    }

    [Fact]
    public async Task Apply_Remove_DeletesBothKeys()
    {
        var registry = new Mock<IRegistrySettingService>();
        var deleted = new List<string>();

        registry.Setup(r => r.KeyExists(It.IsAny<RegistryHive>(), It.IsAny<string>())).Returns(true);
        registry.Setup(r => r.DeleteKey(RegistryHive.CurrentUser, ClassicMenuKey1Path))
                .Callback(() => deleted.Add("key1"));
        registry.Setup(r => r.DeleteKey(RegistryHive.CurrentUser, ClassicMenuKey2Path))
                .Callback(() => deleted.Add("key2"));

        var vm = new ContextMenuViewModel(registry.Object);
        await vm.LoadAsync();

        Assert.True(vm.Rows[5].IsOn);
        await vm.Rows[5].SetAsync(false);

        Assert.Equal(2, deleted.Count);
        Assert.Contains("key1", deleted);
        Assert.Contains("key2", deleted);
    }
}
