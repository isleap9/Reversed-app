using Moq;
using VainTools.App.Services;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;

namespace VainTools.Tests;

public sealed class NetworkServiceTests
{
    private readonly Mock<IProcessRunner> _processRunnerMock = new();
    private readonly NetworkService _service;

    public NetworkServiceTests()
    {
        _service = new NetworkService(_processRunnerMock.Object, NullLogger<NetworkService>.Instance);
    }

    [Fact]
    public async Task GetAdaptersAsync_ParsesJsonOutput()
    {
        var json = """
            [{"Name":"Ethernet","InterfaceDescription":"Intel I219-V","Status":"Up","MacAddress":"00-11-22-33-44-55","LinkSpeed":"1 Gbps"}]
            """;

        _processRunnerMock
            .Setup(x => x.RunAsync("powershell.exe", It.Is<string>(s => s.Contains("Get-NetAdapter"))))
            .ReturnsAsync(new ProcessResult(0, json, string.Empty));

        var adapters = await _service.GetAdaptersAsync();

        Assert.Single(adapters);
        Assert.Equal("Ethernet", adapters[0].Name);
        Assert.Equal("Intel I219-V", adapters[0].InterfaceDescription);
    }

    [Fact]
    public async Task GetAdaptersAsync_EmptyOutput_ReturnsEmptyList()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("powershell.exe", It.Is<string>(s => s.Contains("Get-NetAdapter"))))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

        var adapters = await _service.GetAdaptersAsync();

        Assert.Empty(adapters);
    }

    [Fact]
    public async Task GetAdaptersAsync_ProcessFails_Throws()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("powershell.exe", It.Is<string>(s => s.Contains("Get-NetAdapter"))))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "Access denied"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetAdaptersAsync());
    }

    [Fact]
    public async Task IsAdapterBindingEnabledAsync_True()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("powershell.exe", It.Is<string>(s => s.Contains("Get-NetAdapterBinding"))))
            .ReturnsAsync(new ProcessResult(0, "True", string.Empty));

        var result = await _service.IsAdapterBindingEnabledAsync("Ethernet", "ms_tcpip");

        Assert.True(result);
    }

    [Fact]
    public async Task SetAdapterBindingAsync_Enable()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("powershell.exe", It.Is<string>(s => s.Contains("Enable-NetAdapterBinding"))))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

        await _service.SetAdapterBindingAsync("Ethernet", "ms_tcpip", true);

        _processRunnerMock.Verify(x => x.RunAsync("powershell.exe",
            It.Is<string>(s => s.Contains("Enable-NetAdapterBinding"))), Times.Once);
    }

    [Fact]
    public async Task SetAdapterBindingAsync_Disable()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("powershell.exe", It.Is<string>(s => s.Contains("Disable-NetAdapterBinding"))))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

        await _service.SetAdapterBindingAsync("Ethernet", "ms_tcpip", false);

        _processRunnerMock.Verify(x => x.RunAsync("powershell.exe",
            It.Is<string>(s => s.Contains("Disable-NetAdapterBinding"))), Times.Once);
    }
}
