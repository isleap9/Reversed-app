using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VainTools.App.Services;
using Xunit;

namespace VainTools.Tests;

/// <summary>
/// Service tests for <see cref="OptionalFeaturesService"/> mock <see cref="IProcessRunner"/>
/// with canned <c>dism.exe</c> output — the real DISM store is never touched.
/// </summary>
public sealed class OptionalFeaturesServiceTests
{
    private readonly Mock<IProcessRunner> _processRunnerMock = new();
    private readonly OptionalFeaturesService _service;

    public OptionalFeaturesServiceTests()
    {
        _service = new OptionalFeaturesService(
            _processRunnerMock.Object,
            NullLogger<OptionalFeaturesService>.Instance);
    }

    [Fact]
    public async Task GetFeatures_ParsesNameAndStatePairs()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("dism.exe", It.IsAny<string>()))
            .ReturnsAsync(new ProcessResult(0, CannedFeaturesOutput, string.Empty));

        var features = await _service.GetFeaturesAsync();

        Assert.Equal(2, features.Count);
        Assert.Contains(features, f => f.Name == "Microsoft-Windows-Subsystem-Linux" && f.State == "Disabled");
        Assert.Contains(features, f => f.Name == "Microsoft-Hyper-V" && f.State == "Enabled");
    }

    [Fact]
    public async Task GetFeatures_ReturnsEmptyOnEmptyOutput()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("dism.exe", It.IsAny<string>()))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

        Assert.Empty(await _service.GetFeaturesAsync());
    }

    [Fact]
    public async Task GetFeatures_ThrowsWhenDismFails()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("dism.exe", It.IsAny<string>()))
            .ReturnsAsync(new ProcessResult(87, string.Empty, "An error occurred"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetFeaturesAsync());
    }

    [Fact]
    public async Task EnableFeatureAsync_CallsDismEnable()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("dism.exe", It.IsAny<string>()))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

        await _service.EnableFeatureAsync("Microsoft-Hyper-V");

        _processRunnerMock.Verify(
            x => x.RunAsync("dism.exe", It.Is<string>(a =>
                a.Contains("/Enable-Feature") && a.Contains("Microsoft-Hyper-V") && a.Contains("/NoRestart"))),
            Times.Once);
    }

    [Fact]
    public async Task DisableFeatureAsync_CallsDismDisable()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("dism.exe", It.IsAny<string>()))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

        await _service.DisableFeatureAsync("Microsoft-Hyper-V");

        _processRunnerMock.Verify(
            x => x.RunAsync("dism.exe", It.Is<string>(a =>
                a.Contains("/Disable-Feature") && a.Contains("Microsoft-Hyper-V") && a.Contains("/NoRestart"))),
            Times.Once);
    }

    [Fact]
    public async Task EnableFeatureAsync_ThrowsWhenDismFails()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("dism.exe", It.IsAny<string>()))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "Access is denied"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.EnableFeatureAsync("Microsoft-Hyper-V"));
        Assert.Contains("Access is denied", ex.Message);
    }

    [Fact]
    public async Task DisableFeatureAsync_ThrowsWhenDismFails()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync("dism.exe", It.IsAny<string>()))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "Access is denied"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DisableFeatureAsync("Microsoft-Hyper-V"));
    }

    [Fact]
    public async Task EnableFeatureAsync_WithEmptyName_ThrowsArgument()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.EnableFeatureAsync("  "));
    }

    [Fact]
    public void ParseFeatures_SkipsBlocksMissingState()
    {
        const string output = "Feature Name : Orphaned\r\n\r\nFeature Name : Good\r\nState : Enabled\r\n";

        var features = OptionalFeaturesService.ParseFeatures(output);

        Assert.Equal([new OptionalFeature("Good", "Enabled")], features);
    }

    private const string CannedFeaturesOutput = """
        Deployment Image Servicing and Management tool
        Version: 10.0.26100.1150

        Features listing for package : Microsoft-Windows-Foundation-Package~31bf3856ad364e35~amd64~~10.0.26100.1

        ----------------------------------------------------------------------
        Feature Name : Microsoft-Windows-Subsystem-Linux
        State : Disabled

        Feature Name : Microsoft-Hyper-V
        State : Enabled

        The operation completed successfully.
        """;
}
