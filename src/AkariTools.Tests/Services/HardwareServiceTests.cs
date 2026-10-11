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

        Assert.Contains("GB", info.Memory);
    }
}
