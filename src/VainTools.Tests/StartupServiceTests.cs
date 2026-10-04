using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VainTools.App.Services;
using Xunit;

namespace VainTools.Tests;

public sealed class StartupServiceTests
{
    /// <summary>One record of real schtasks /FO LIST /V output.</summary>
    private const string SchtasksSample = """
        HostName:                             TESTPC
        TaskName:                             \Microsoft\Edge\Update
        Next Run Time:                        N/A
        Status:                               Ready
        Logon Mode:                           Interactive only
        Schedule Type:                        At logon time

        HostName:                             TESTPC
        TaskName:                             \VainDisabledTask
        Next Run Time:                        N/A
        Status:                               Disabled
        Logon Mode:                           Interactive only
        Schedule Type:                        At system start up

        HostName:                             TESTPC
        TaskName:                             \EveryHour
        Next Run Time:                        04/10/2026 03:00:00
        Status:                               Ready
        Logon Mode:                           Interactive only
        Schedule Type:                        One Time Only, Hourly

        """;

    private readonly Mock<IProcessRunner> _processRunnerMock = new();
    private readonly StartupService _service;

    public StartupServiceTests()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ProcessResult(0, SchtasksSample, string.Empty));

        _service = new StartupService(
            _processRunnerMock.Object,
            NullLogger<StartupService>.Instance);
    }

    [Fact]
    public void GetRunKeyEntries_ReturnsList()
    {
        Assert.NotNull(_service.GetRunKeyEntries());
    }

    [Fact]
    public void GetRunKeyEntries_AllHaveNameAndKnownHive()
    {
        var entries = _service.GetRunKeyEntries();

        Assert.All(entries, e =>
        {
            Assert.False(string.IsNullOrWhiteSpace(e.Name));
            Assert.Contains(e.Source, new[] { "HKCU", "HKLM" });
        });
    }

    [Fact]
    public void GetRunKeyEntries_DisplayNamesDoNotCarryDisabledMarker()
    {
        var entries = _service.GetRunKeyEntries();

        Assert.All(entries, e => Assert.False(e.Name.StartsWith('-')));
    }

    [Fact]
    public async Task GetScheduledTasksAsync_OnlyReturnsBootOrLogonTasks()
    {
        var tasks = await _service.GetScheduledTasksAsync();

        Assert.Equal(2, tasks.Count);
        Assert.Contains(tasks, t => t.Name == "Update");
        Assert.Contains(tasks, t => t.Name == "VainDisabledTask");
        Assert.DoesNotContain(tasks, t => t.Name == "EveryHour");
    }

    [Fact]
    public async Task GetScheduledTasksAsync_SplitsNameFromFolderPath()
    {
        var tasks = await _service.GetScheduledTasksAsync();

        var edge = Assert.Single(tasks, t => t.Name == "Update");
        Assert.Equal(@"\Microsoft\Edge\", edge.Command);
        Assert.Equal("Scheduled Task", edge.Source);
    }

    [Fact]
    public async Task GetScheduledTasksAsync_ReportsEnabledState()
    {
        var tasks = await _service.GetScheduledTasksAsync();

        Assert.True(Assert.Single(tasks, t => t.Name == "Update").IsEnabled);
        Assert.False(Assert.Single(tasks, t => t.Name == "VainDisabledTask").IsEnabled);
    }

    [Fact]
    public async Task GetScheduledTasksAsync_WhenSchtasksFails_ReturnsEmpty()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "ERROR: Access is denied."));

        Assert.Empty(await _service.GetScheduledTasksAsync());
    }

    [Fact]
    public async Task ToggleScheduledTaskAsync_Disable_InvokesSchtasks()
    {
        var entry = new StartupEntry("VainToolsTest", @"\", "Scheduled Task", true);

        await _service.ToggleScheduledTaskAsync(entry, isEnabled: false);

        _processRunnerMock.Verify(
            x => x.RunAsync("schtasks.exe", "/Change /TN \"\\VainToolsTest\" /DISABLE"),
            Times.Once);
    }

    [Fact]
    public async Task ToggleScheduledTaskAsync_Enable_InvokesSchtasks()
    {
        var entry = new StartupEntry("VainToolsTest", @"\", "Scheduled Task", false);

        await _service.ToggleScheduledTaskAsync(entry, isEnabled: true);

        _processRunnerMock.Verify(
            x => x.RunAsync("schtasks.exe", "/Change /TN \"\\VainToolsTest\" /ENABLE"),
            Times.Once);
    }

    [Fact]
    public async Task ToggleScheduledTaskAsync_IncludesFolderPathInTaskName()
    {
        var entry = new StartupEntry("Update", @"\Microsoft\Edge\", "Scheduled Task", true);

        await _service.ToggleScheduledTaskAsync(entry, isEnabled: false);

        _processRunnerMock.Verify(
            x => x.RunAsync("schtasks.exe", "/Change /TN \"\\Microsoft\\Edge\\Update\" /DISABLE"),
            Times.Once);
    }

    [Fact]
    public async Task ToggleScheduledTaskAsync_WhenSchtasksFails_Throws()
    {
        _processRunnerMock
            .Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "ERROR: Access is denied."));

        var entry = new StartupEntry("VainToolsTest", @"\", "Scheduled Task", true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ToggleScheduledTaskAsync(entry, isEnabled: false));

        Assert.Contains("Access is denied", ex.Message);
    }

    [Fact]
    public void ToggleRunKeyEntry_WhenEntryIsGone_Throws()
    {
        var entry = new StartupEntry("VainToolsDefinitelyMissing", @"\", "HKCU", true);

        Assert.Throws<InvalidOperationException>(
            () => _service.ToggleRunKeyEntry(entry, isEnabled: false));
    }
}
