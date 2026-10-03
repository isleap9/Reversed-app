using Microsoft.Extensions.Logging.Abstractions;
using VainTools.App.Services;
using Xunit;

namespace VainTools.Tests;

public sealed class AffinityServiceTests
{
    private readonly AffinityService _service;

    public AffinityServiceTests()
    {
        _service = new AffinityService(NullLogger<AffinityService>.Instance);
    }

    [Fact]
    public void GetCpuCount_ReturnsPositiveValue()
    {
        var count = _service.GetCpuCount();
        Assert.True(count > 0, "CPU count should be positive");
    }

    [Fact]
    public void GetProcesses_ReturnsNonEmptyList()
    {
        var processes = _service.GetProcesses();
        Assert.NotNull(processes);
        Assert.NotEmpty(processes);
    }

    [Fact]
    public void GetProcesses_ContainsCurrentProcess()
    {
        var processes = _service.GetProcesses();
        var currentPid = Environment.ProcessId;
        Assert.Contains(processes, p => p.Id == currentPid);
    }

    [Fact]
    public void GetProcesses_AllHavePositiveCpuCount()
    {
        var processes = _service.GetProcesses();
        Assert.All(processes, p => Assert.True(p.CpuCount > 0, $"Process {p.Name} should have positive CPU count"));
    }

    [Fact]
    public void GetAffinityMask_ForCurrentProcess_ReturnsNonZero()
    {
        var mask = _service.GetAffinityMask(Environment.ProcessId);
        Assert.NotEqual(0UL, mask);
    }

    [Fact]
    public void GetAffinityMask_ForInvalidProcess_Throws()
    {
        Assert.Throws<ArgumentException>(() => _service.GetAffinityMask(-1));
    }

    [Fact]
    public void SetAffinityMask_ForCurrentProcess_DoesNotThrow()
    {
        var originalMask = _service.GetAffinityMask(Environment.ProcessId);
        _service.SetAffinityMask(Environment.ProcessId, originalMask);
    }

    [Fact]
    public void SetAffinityMask_ForInvalidProcess_Throws()
    {
        Assert.Throws<ArgumentException>(() => _service.SetAffinityMask(-1, 1UL));
    }
}
