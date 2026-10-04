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
        Assert.True(_service.GetCpuCount() > 0);
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

        Assert.Contains(processes, p => p.Id == Environment.ProcessId);
    }

    [Fact]
    public void GetProcesses_AllHaveNamesAndPositiveCpuCount()
    {
        var processes = _service.GetProcesses();

        Assert.All(processes, p =>
        {
            Assert.False(string.IsNullOrWhiteSpace(p.Name));
            Assert.True(p.CpuCount > 0, $"Process {p.Name} should report at least one CPU");
        });
    }

    [Fact]
    public void GetProcesses_SkipsSystemProcesses()
    {
        var processes = _service.GetProcesses();

        Assert.DoesNotContain(processes, p => p.Id is 0 or 4);
    }

    [Fact]
    public void GetAffinityMask_ForCurrentProcess_ReturnsNonZero()
    {
        Assert.NotEqual(0UL, _service.GetAffinityMask(Environment.ProcessId));
    }

    [Fact]
    public void GetAffinityMask_ForUnknownProcess_Throws()
    {
        Assert.ThrowsAny<Exception>(() => _service.GetAffinityMask(-1));
    }

    [Fact]
    public void GetSystemAffinityMask_ReturnsNonZero()
    {
        Assert.NotEqual(0UL, _service.GetSystemAffinityMask());
    }

    [Fact]
    public void SetAffinityMask_ForCurrentProcess_RoundTrips()
    {
        var original = _service.GetAffinityMask(Environment.ProcessId);

        try
        {
            _service.SetAffinityMask(Environment.ProcessId, original);

            Assert.Equal(original, _service.GetAffinityMask(Environment.ProcessId));
        }
        finally
        {
            _service.SetAffinityMask(Environment.ProcessId, original);
        }
    }

    [Fact]
    public void SetAffinityMask_ForUnknownProcess_Throws()
    {
        Assert.ThrowsAny<Exception>(() => _service.SetAffinityMask(-1, 1UL));
    }
}
