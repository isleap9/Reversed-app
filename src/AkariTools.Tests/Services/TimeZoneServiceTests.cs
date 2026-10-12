using AkariTools.Framework.Services;
using Moq;
using Xunit;

namespace AkariTools.Tests.Services;

/// <summary>TimeZoneService reads via TimeZoneInfo; only writes go through ICliRunner.</summary>
public sealed class TimeZoneServiceTests
{
    [Fact]
    public void GetTimeZones_ReturnsNonEmptyOptionsWithKeys()
    {
        var service = new TimeZoneService(new Mock<ICliRunner>().Object);

        var zones = service.GetTimeZones();

        Assert.NotEmpty(zones);
        Assert.All(zones, z =>
        {
            Assert.False(string.IsNullOrWhiteSpace(z.KeyName));
            Assert.False(string.IsNullOrWhiteSpace(z.DisplayName));
        });
    }

    [Fact]
    public void GetTimeZones_DoesNotTouchCliRunner()
    {
        var runner = new Mock<ICliRunner>(MockBehavior.Strict);
        var service = new TimeZoneService(runner.Object);

        _ = service.GetTimeZones();

        runner.Verify(r => r.Run(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void GetCurrentKey_EqualsLocalId()
    {
        var service = new TimeZoneService(new Mock<ICliRunner>().Object);

        Assert.Equal(TimeZoneInfo.Local.Id, service.GetCurrentKey());
    }

    [Fact]
    public void SetTimeZone_PassesExactArgs()
    {
        var runner = new Mock<ICliRunner>();
        runner.Setup(r => r.Run("tzutil.exe", "/s \"W. Europe Standard Time\"")).Returns(string.Empty);
        var service = new TimeZoneService(runner.Object);

        Assert.True(service.SetTimeZone("W. Europe Standard Time"));
        runner.Verify(r => r.Run("tzutil.exe", "/s \"W. Europe Standard Time\""), Times.Once);
    }

    [Fact]
    public void SetTimeZone_RunnerFailure_ReturnsFalse()
    {
        var runner = new Mock<ICliRunner>();
        runner.Setup(r => r.Run("tzutil.exe", "/s \"W. Europe Standard Time\"")).Returns((string?)null);
        var service = new TimeZoneService(runner.Object);

        Assert.False(service.SetTimeZone("W. Europe Standard Time"));
    }
}
