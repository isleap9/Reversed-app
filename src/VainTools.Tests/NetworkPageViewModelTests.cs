using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32;
using Moq;
using VainTools.App.Services;
using VainTools.App.ViewModels;
using VainTools.Framework.Services;
using Xunit;

namespace VainTools.Tests;

/// <summary>
/// Pins the DNS/NTP registry write paths (NET-02/NET-03). Exercising the writes
/// end-to-end needs elevation, so the mocked-registry contract plus the VM's
/// elevation handling stands in for a live write.
/// </summary>
public sealed class NetworkPageViewModelTests
{
    private const string TcpipParameters = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters";
    private const string W32TimeParameters = @"SYSTEM\CurrentControlSet\Services\W32Time\Parameters";

    private readonly Mock<INetworkService> _networkServiceMock = new();
    private readonly Mock<IRegistryTweakService> _registryMock = new();
    private readonly Mock<IDialogService> _dialogServiceMock = new();
    private readonly Mock<IInfoBarService> _infoBarServiceMock = new();
    private readonly NetworkPageViewModel _viewModel;

    public NetworkPageViewModelTests()
    {
        _viewModel = new NetworkPageViewModel(
            _networkServiceMock.Object,
            _registryMock.Object,
            _dialogServiceMock.Object,
            _infoBarServiceMock.Object,
            NullLogger<NetworkPageViewModel>.Instance);
    }

    [Fact]
    public async Task LoadDnsNtpAsync_ReadsBothDocumentedPaths()
    {
        _registryMock
            .Setup(x => x.ReadString(RegistryHive.LocalMachine, TcpipParameters, "NameServer"))
            .Returns("1.1.1.1");
        _registryMock
            .Setup(x => x.ReadString(RegistryHive.LocalMachine, W32TimeParameters, "NtpServer"))
            .Returns("time.windows.com");

        await _viewModel.LoadDnsNtpCommand.ExecuteAsync(null);

        Assert.Equal("1.1.1.1", _viewModel.DnsServer);
        Assert.Equal("time.windows.com", _viewModel.NtpServer);
    }

    [Fact]
    public async Task SaveDnsNtpAsync_WritesDnsToTcpipParameters()
    {
        _viewModel.DnsServer = "1.1.1.1";
        _viewModel.NtpServer = "time.windows.com";

        await _viewModel.SaveDnsNtpCommand.ExecuteAsync(null);

        _registryMock.Verify(
            x => x.WriteString(RegistryHive.LocalMachine, TcpipParameters, "NameServer", "1.1.1.1"),
            Times.Once);
    }

    [Fact]
    public async Task SaveDnsNtpAsync_WritesNtpToW32TimeParameters()
    {
        _viewModel.DnsServer = "1.1.1.1";
        _viewModel.NtpServer = "time.windows.com";

        await _viewModel.SaveDnsNtpCommand.ExecuteAsync(null);

        _registryMock.Verify(
            x => x.WriteString(RegistryHive.LocalMachine, W32TimeParameters, "NtpServer", "time.windows.com"),
            Times.Once);
    }

    [Fact]
    public async Task SaveDnsNtpAsync_WhenWriteFails_ReportsError()
    {
        _registryMock
            .Setup(x => x.WriteString(It.IsAny<RegistryHive>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException("Access to the registry key is denied."));

        await _viewModel.SaveDnsNtpCommand.ExecuteAsync(null);

        Assert.Contains("Could not save settings", _viewModel.StatusMessage);
        _infoBarServiceMock.Verify(
            x => x.ShowError("Save failed", It.IsAny<string>()),
            Times.Once);
    }
}
