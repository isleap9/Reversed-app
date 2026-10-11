using AkariTools.Framework.Services;
using Xunit;

namespace AkariTools.Tests.Services;

public sealed class HardwareServiceTests
{
    [Fact]
    public async Task LoadHardwareInfoAsync_ReturnsNonEmptyStrings()
    {
        var info = await new HardwareService().LoadHardwareInfoAsync();

        Assert.NotNull(info);
        Assert.False(string.IsNullOrWhiteSpace(info.Processor));
        Assert.False(string.IsNullOrWhiteSpace(info.Graphics));
        Assert.False(string.IsNullOrWhiteSpace(info.Memory));
        Assert.False(string.IsNullOrWhiteSpace(info.Motherboard));
    }

    [Fact]
    public async Task LoadHardwareInfoAsync_MemoryContainsGb()
    {
        var info = await new HardwareService().LoadHardwareInfoAsync();

        // VMs may expose no Win32_PhysicalMemory rows; "Unknown" is the
        // documented fallback and must not fail CI.
        Assert.True(
            info.Memory.Contains("GB") || info.Memory == "Unknown",
            $"Expected memory to contain 'GB' or be 'Unknown', but was '{info.Memory}'.");
    }
}
