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
