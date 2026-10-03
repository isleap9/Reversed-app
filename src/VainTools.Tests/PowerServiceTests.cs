using Moq;
using VainTools.App.Services;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;

namespace VainTools.Tests;

public sealed class PowerServiceTests
{
    private readonly Mock<IProcessRunner> _processRunnerMock = new();
    private readonly PowerService _service;

    public PowerServiceTests()
    {
        _service = new PowerService(_processRunnerMock.Object, NullLogger<PowerService>.Instance);
    }

    [Fact]
    public async Task GetPlansAsync_ParsesPowerCfgListOutput()
    {
        var output = """
            Power Scheme GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (Balanced)
            Power Scheme GUID: 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c  (High Performance)
            Power Scheme GUID: a1841308-3541-4fab-bc81-f71556f20b4a  (Power Saver)
            """;

        _processRunnerMock
            .Setup(x => x.RunAsync("powercfg.exe", "/list"))
            .ReturnsAsync(new ProcessResult(0, output, string.Empty));

        var plans = await _service.GetPlansAsync();

        Assert.Equal(3, plans.Count);
        Assert.Equal("Balanced", plans[0].Name);
        Assert.Equal(new Guid("381b4222-f694-41f0-9685-ff5bb260df2e"), plans[0].Guid);
    }

    [Fact]
    public async Task GetPlansAsync_EmptyOutput_ReturnsEmptyList()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("powercfg.exe", "/list"))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

        var plans = await _service.GetPlansAsync();

        Assert.Empty(plans);
    }

    [Fact]
    public async Task GetSettingsAsync_ParsesPowerCfgQueryOutput()
    {
        var output = """
            Power Scheme GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (Balanced)
            Subgroup GUID: 54533251-6be2-4135-9c32-8817097453e1  (Processor power management)
            Power Setting GUID: 893dee8e-2bef-41e0-89c6-b55d0929964c  (Minimum processor state)
            Possible Setting Index: 0x00000000
            Possible Setting Index: 0x00000064
            Current AC Power Setting Index: 0x00000005
            Current DC Power Setting Index: 0x00000005
            """;

        _processRunnerMock
            .Setup(x => x.RunAsync("powercfg.exe", It.Is<string>(s => s.Contains("/query"))))
            .ReturnsAsync(new ProcessResult(0, output, string.Empty));

        var settings = await _service.GetSettingsAsync(Guid.NewGuid());

        Assert.Single(settings);
        Assert.Equal("Minimum processor state", settings[0].Name);
        Assert.Equal("Processor power management", settings[0].CategoryName);
        Assert.Equal("5", settings[0].CurrentAcValue);
        Assert.Equal("5", settings[0].CurrentDcValue);
        Assert.Equal(2, settings[0].PossibleValues.Count);
    }

    [Fact]
    public async Task GetSettingsAsync_EmptyOutput_ReturnsEmptyList()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("powercfg.exe", It.Is<string>(s => s.Contains("/query"))))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

        var settings = await _service.GetSettingsAsync(Guid.NewGuid());

        Assert.Empty(settings);
    }

    [Fact]
    public async Task ApplySettingAsync_CallsSetAcAndDcValueIndex()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("powercfg.exe", It.Is<string>(s => s.Contains("/setacvalueindex"))))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));
        _processRunnerMock
            .Setup(x => x.RunAsync("powercfg.exe", It.Is<string>(s => s.Contains("/setdcvalueindex"))))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

        var planGuid = Guid.NewGuid();
        await _service.ApplySettingAsync(planGuid, "893dee8e-2bef-41e0-89c6-b55d0929964c", "5");

        _processRunnerMock.Verify(x => x.RunAsync("powercfg.exe",
            $"/setacvalueindex {planGuid} 893dee8e-2bef-41e0-89c6-b55d0929964c 5"), Times.Once);
        _processRunnerMock.Verify(x => x.RunAsync("powercfg.exe",
            $"/setdcvalueindex {planGuid} 893dee8e-2bef-41e0-89c6-b55d0929964c 5"), Times.Once);
    }

    [Fact]
    public async Task RevertPlanAsync_CallsRestoreDefaultSchemes()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("powercfg.exe", "/restoredefaultschemes"))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

        await _service.RevertPlanAsync(Guid.NewGuid());

        _processRunnerMock.Verify(x => x.RunAsync("powercfg.exe", "/restoredefaultschemes"), Times.Once);
    }

    [Fact]
    public async Task GetPlansAsync_ProcessFails_Throws()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("powercfg.exe", "/list"))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "Access denied"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetPlansAsync());
    }

    [Fact]
    public async Task GetSettingsAsync_ProcessFails_Throws()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("powercfg.exe", It.Is<string>(s => s.Contains("/query"))))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "Access denied"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetSettingsAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ApplySettingAsync_ProcessFails_Throws()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("powercfg.exe", It.Is<string>(s => s.Contains("/setacvalueindex"))))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "Access denied"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ApplySettingAsync(Guid.NewGuid(), "guid", "5"));
    }

    [Fact]
    public async Task RevertPlanAsync_ProcessFails_Throws()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("powercfg.exe", "/restoredefaultschemes"))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "Access denied"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.RevertPlanAsync(Guid.NewGuid()));
    }
}
