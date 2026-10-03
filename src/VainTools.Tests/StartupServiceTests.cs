using Microsoft.Extensions.Logging.Abstractions;
using VainTools.App.Services;
using Xunit;

namespace VainTools.Tests;

public sealed class StartupServiceTests
{
    private readonly StartupService _service;

    public StartupServiceTests()
    {
        _service = new StartupService(NullLogger<StartupService>.Instance);
    }

    [Fact]
    public void GetRunKeyEntries_ReturnsList()
    {
        var entries = _service.GetRunKeyEntries();
        Assert.NotNull(entries);
    }

    [Fact]
    public void GetRunKeyEntries_AllHaveNames()
    {
        var entries = _service.GetRunKeyEntries();
        Assert.All(entries, e => Assert.False(string.IsNullOrWhiteSpace(e.Name)));
    }

    [Fact]
    public void GetRunKeyEntries_AllHaveSource()
    {
        var entries = _service.GetRunKeyEntries();
        Assert.All(entries, e => Assert.False(string.IsNullOrWhiteSpace(e.Source)));
    }

    [Fact]
    public void GetScheduledTasks_ReturnsList()
    {
        var tasks = _service.GetScheduledTasks();
        Assert.NotNull(tasks);
    }

    [Fact]
    public void GetScheduledTasks_AllHaveNames()
    {
        var tasks = _service.GetScheduledTasks();
        Assert.All(tasks, t => Assert.False(string.IsNullOrWhiteSpace(t.Name)));
    }

    [Fact]
    public void ToggleRunKeyEntry_WithValidEntry_DoesNotThrow()
    {
        var entry = new StartupEntry("test", "cmd", "HKCU", true);
        _service.ToggleRunKeyEntry(entry, false);
    }

    [Fact]
    public void ToggleScheduledTask_WithValidEntry_DoesNotThrow()
    {
        var entry = new StartupEntry("test", "cmd", "Task", true);
        _service.ToggleScheduledTask(entry, false);
    }
}
