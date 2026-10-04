using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32;
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

    [Fact]
    public void Rules_SaveGetDelete_RoundTrips()
    {
        var (service, root) = CreateIsolatedService();
        try
        {
            Assert.Empty(service.GetRules());

            service.SaveRule("vain-test.exe", 0b0101UL);

            var saved = Assert.Single(service.GetRules());
            Assert.Equal("vain-test.exe", saved.ProcessName);
            Assert.Equal(0b0101UL, saved.Mask);

            service.SaveRule("vain-test.exe", 0b0011UL);
            Assert.Equal(0b0011UL, Assert.Single(service.GetRules()).Mask);

            service.DeleteRule("vain-test.exe");
            Assert.Empty(service.GetRules());
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public void SaveRule_WithEmptyName_Throws()
    {
        var (service, root) = CreateIsolatedService();
        try
        {
            Assert.Throws<ArgumentException>(() => service.SaveRule("  ", 1UL));
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public void SaveRule_WithZeroMask_Throws()
    {
        var (service, root) = CreateIsolatedService();
        try
        {
            Assert.Throws<ArgumentException>(() => service.SaveRule("vain-test.exe", 0UL));
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public void DeleteRule_WhenMissing_Throws()
    {
        var (service, root) = CreateIsolatedService();
        try
        {
            Assert.Throws<InvalidOperationException>(() => service.DeleteRule("vain-test-missing.exe"));
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public void ApplyRules_WithNoMatchingProcess_ReturnsZero()
    {
        var (service, root) = CreateIsolatedService();
        try
        {
            service.SaveRule("vain-process-that-does-not-exist.exe", 1UL);

            Assert.Equal(0, service.ApplyRules());
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public void ApplyRules_WithOwnProcessRule_ReappliesCurrentMask()
    {
        var (service, root) = CreateIsolatedService();
        try
        {
            var ownName = Environment.ProcessPath is string path
                ? Path.GetFileName(path)
                : "dotnet";
            var ownMask = service.GetAffinityMask(Environment.ProcessId);
            service.SaveRule(ownName, ownMask);

            Assert.True(service.ApplyRules() >= 1);
        }
        finally
        {
            Cleanup(root);
        }
    }

    private static (AffinityService Service, string RootPath) CreateIsolatedService()
    {
        var root = $@"SOFTWARE\VainTools\Test\{Guid.NewGuid():N}";
        var service = new AffinityService(
            NullLogger<AffinityService>.Instance,
            root + @"\AffinityRules");

        return (service, root);
    }

    private static void Cleanup(string rootPath)
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(rootPath, throwOnMissingSubKey: false);
        }
        catch
        {
            // Best-effort test cleanup; a leftover throwaway key is harmless.
        }
    }
}
