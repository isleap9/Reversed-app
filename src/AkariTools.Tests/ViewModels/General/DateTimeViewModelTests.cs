using AkariTools.App.ViewModels.General;
using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using Moq;
using Xunit;

namespace AkariTools.Tests.ViewModels.General;

public sealed class DateTimeViewModelTests
{
    private const string W32TimeParametersPath =
        @"SYSTEM\CurrentControlSet\Services\W32Time\Parameters";

    private static IRegistrySettingService MockRegistry(string? ntpServerValue)
    {
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadString(
            It.Is<RegistryValue>(v =>
                v.Hive == RegistryHive.LocalMachine &&
                v.Path == W32TimeParametersPath &&
                v.Name == "NtpServer")))
            .Returns(ntpServerValue);
        return registry.Object;
    }

    private static IServiceControl MockServiceControl()
    {
        var sc = new Mock<IServiceControl>();
        sc.Setup(s => s.StopService(It.IsAny<string>())).Returns(true);
        sc.Setup(s => s.StartService(It.IsAny<string>())).Returns(true);
        return sc.Object;
    }

    [Fact]
    public async Task Load_ParsesNtpServerValue()
    {
        var registry = MockRegistry("time.windows.com,0x9 pool.ntp.org,0x2");
        var vm = new DateTimeViewModel(registry, MockServiceControl());

        await vm.LoadAsync();

        Assert.Equal(2, vm.NtpServers.Count);
        Assert.Contains("time.windows.com,0x9", vm.NtpServers);
        Assert.Contains("pool.ntp.org,0x2", vm.NtpServers);
    }

    [Fact]
    public async Task Load_EmptyValue_ClearsServers()
    {
        var registry = MockRegistry(null);
        var vm = new DateTimeViewModel(registry, MockServiceControl());

        await vm.LoadAsync();

        Assert.Empty(vm.NtpServers);
    }

    [Fact]
    public async Task AddServer_RejectsInvalidInput()
    {
        // Review Focus #3: garbage input must be rejected before writing.
        var registry = MockRegistry("");
        var vm = new DateTimeViewModel(registry, MockServiceControl());

        await vm.LoadAsync();

        // Empty string
        vm.NewServer = "   ";
        await vm.AddServerCommand.ExecuteAsync(null);
        Assert.NotNull(vm.ErrorMessage);
        Assert.Empty(vm.NtpServers);

        // Contains spaces (not a valid single entry)
        vm.NewServer = "time windows com";
        await vm.AddServerCommand.ExecuteAsync(null);
        Assert.NotNull(vm.ErrorMessage);
        Assert.Empty(vm.NtpServers);
    }

    [Fact]
    public async Task AddServer_AcceptsFlagFormat()
    {
        var registry = MockRegistry("");
        var vm = new DateTimeViewModel(registry, MockServiceControl());

        await vm.LoadAsync();

        vm.NewServer = "pool.ntp.org,0x9";
        await vm.AddServerCommand.ExecuteAsync(null);

        Assert.Null(vm.ErrorMessage);
        Assert.Single(vm.NtpServers);
        Assert.Equal("pool.ntp.org,0x9", vm.NtpServers[0]);
    }

    [Fact]
    public async Task AddServer_RejectsMalformedFlag()
    {
        var registry = MockRegistry("");
        var vm = new DateTimeViewModel(registry, MockServiceControl());

        await vm.LoadAsync();

        // Flag without 0x prefix
        vm.NewServer = "pool.ntp.org,abc";
        await vm.AddServerCommand.ExecuteAsync(null);
        Assert.NotNull(vm.ErrorMessage);
        Assert.Empty(vm.NtpServers);
    }

    [Fact]
    public async Task AddServer_RejectsEmptyHost()
    {
        // ",0x9" has a valid flag but no host.
        var vm = new DateTimeViewModel(MockRegistry(""), MockServiceControl());
        await vm.LoadAsync();

        vm.NewServer = ",0x9";
        await vm.AddServerCommand.ExecuteAsync(null);

        Assert.NotNull(vm.ErrorMessage);
        Assert.Empty(vm.NtpServers);
    }

    [Fact]
    public async Task AddServer_RejectsTabWhitespace()
    {
        // Only ' ' was rejected; pasted tabs slipped through to HKLM.
        var vm = new DateTimeViewModel(MockRegistry(""), MockServiceControl());
        await vm.LoadAsync();

        vm.NewServer = "time\twindows.com";
        await vm.AddServerCommand.ExecuteAsync(null);

        Assert.NotNull(vm.ErrorMessage);
        Assert.Empty(vm.NtpServers);
    }

    [Fact]
    public async Task AddServer_RejectsDuplicates()
    {
        var vm = new DateTimeViewModel(MockRegistry(""), MockServiceControl());
        await vm.LoadAsync();

        vm.NewServer = "time.windows.com,0x9";
        await vm.AddServerCommand.ExecuteAsync(null);
        Assert.Single(vm.NtpServers);

        // Same entry again (case-insensitive) must not double-write to HKLM.
        vm.NewServer = "TIME.windows.com,0x9";
        await vm.AddServerCommand.ExecuteAsync(null);

        Assert.NotNull(vm.ErrorMessage);
        Assert.Single(vm.NtpServers);
    }

    [Fact]
    public async Task RemoveServer_RemovesEntry()
    {
        var vm = new DateTimeViewModel(MockRegistry(""), MockServiceControl());
        await vm.LoadAsync();

        vm.NewServer = "time.windows.com,0x9";
        await vm.AddServerCommand.ExecuteAsync(null);
        vm.NewServer = "pool.ntp.org,0x2";
        await vm.AddServerCommand.ExecuteAsync(null);
        Assert.Equal(2, vm.NtpServers.Count);

        vm.RemoveServerCommand.Execute("time.windows.com,0x9");

        Assert.Single(vm.NtpServers);
        Assert.Equal("pool.ntp.org,0x2", vm.NtpServers[0]);
    }

    [Fact]
    public async Task Apply_NotifiesCanExecute_WhenListChanges()
    {
        // The Apply button enables only via CanExecuteChanged: WinUI has no
        // CommandManager auto-requery, so the VM must notify on every change.
        var vm = new DateTimeViewModel(MockRegistry(""), MockServiceControl());
        await vm.LoadAsync();
        Assert.False(vm.ApplyCommand.CanExecute(null));

        var fired = false;
        vm.ApplyCommand.CanExecuteChanged += (_, _) => fired = true;

        vm.NewServer = "time.windows.com";
        await vm.AddServerCommand.ExecuteAsync(null);

        Assert.True(fired);
        Assert.True(vm.ApplyCommand.CanExecute(null));
    }

    [Fact]
    public async Task Apply_NotifiesCanExecute_AfterLoadWithServers()
    {
        var vm = new DateTimeViewModel(
            MockRegistry("time.windows.com,0x9"), MockServiceControl());

        var fired = false;
        vm.ApplyCommand.CanExecuteChanged += (_, _) => fired = true;
        await vm.LoadAsync();

        Assert.True(fired);
        Assert.True(vm.ApplyCommand.CanExecute(null));
    }

    [Fact]
    public async Task Apply_RegistryWriteFailure_SetsError()
    {
        // The real registry service returns false (never throws) on denied
        // writes: Apply must surface that instead of reporting success.
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadString(It.IsAny<RegistryValue>())).Returns("");
        registry.Setup(r => r.WriteString(It.IsAny<RegistryValue>(), It.IsAny<string>())).Returns(false);
        var sc = new Mock<IServiceControl>();

        var vm = new DateTimeViewModel(registry.Object, sc.Object);
        await vm.LoadAsync();
        vm.NtpServers.Add("time.windows.com,0x9");

        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.NotNull(vm.ErrorMessage);
        sc.Verify(s => s.StopService(It.IsAny<string>()), Times.Never);
        sc.Verify(s => s.StartService(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Apply_ServiceStopFailure_SetsError()
    {
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadString(It.IsAny<RegistryValue>())).Returns("");
        registry.Setup(r => r.WriteString(It.IsAny<RegistryValue>(), It.IsAny<string>())).Returns(true);
        var sc = new Mock<IServiceControl>();
        sc.Setup(s => s.StopService(It.IsAny<string>())).Returns(false);
        sc.Setup(s => s.StartService(It.IsAny<string>())).Returns(true);

        var vm = new DateTimeViewModel(registry.Object, sc.Object);
        await vm.LoadAsync();
        vm.NtpServers.Add("time.windows.com,0x9");

        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public async Task Apply_WritesJoinedValue()
    {
        string? writtenValue = null;
        var registry = new Mock<IRegistrySettingService>();
        registry.Setup(r => r.ReadString(It.IsAny<RegistryValue>())).Returns("");
        registry.Setup(r => r.WriteString(
                It.Is<RegistryValue>(v =>
                    v.Hive == RegistryHive.LocalMachine &&
                    v.Path == W32TimeParametersPath &&
                    v.Name == "NtpServer"),
                It.IsAny<string>()))
            .Callback<RegistryValue, string>((_, val) => writtenValue = val)
            .Returns(true);

        var vm = new DateTimeViewModel(registry.Object, MockServiceControl());
        await vm.LoadAsync();

        vm.NtpServers.Add("time.windows.com,0x9");
        vm.NtpServers.Add("pool.ntp.org,0x2");

        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.NotNull(writtenValue);
        Assert.Contains("time.windows.com,0x9", writtenValue);
        Assert.Contains("pool.ntp.org,0x2", writtenValue);
    }
}
