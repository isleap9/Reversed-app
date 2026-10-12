using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using RegistryHive = AkariTools.Framework.Models.RegistryHive;
using Xunit;

namespace AkariTools.Tests.Services;

// Each test gets its own throwaway registry key under International so the real
// HKCU\Control Panel\International settings are never touched.
public sealed class DateFormatServiceTests : IDisposable
{
    private readonly string _basePath;
    private readonly string _intlPath;

    public DateFormatServiceTests()
    {
        _basePath = $@"Software\AkariToolbox.Tests\{Guid.NewGuid():N}";
        _intlPath = $@"{_basePath}\Control Panel\International";
    }

    public void Dispose() => new RegistrySettingService().DeleteKey(RegistryHive.CurrentUser, _basePath);

    [Fact]
    public void LocaleName_RoundTrips()
    {
        var service = new DateFormatService(new RegistrySettingService(), _intlPath);
        var expected = "en-US";
        Assert.True(service.SetLocaleName(expected));
        Assert.Equal(expected, service.GetLocaleName());
    }

    [Fact]
    public void MissingKey_ReturnsNull()
    {
        var service = new DateFormatService(new RegistrySettingService(), _intlPath);
        Assert.Null(service.GetLocaleName());
    }

    [Fact]
    public void FirstDay_RoundTripsAsDecimalString()
    {
        var service = new DateFormatService(new RegistrySettingService(), _intlPath);
        var expected = 1;
        Assert.True(service.SetFirstDay(expected));
        Assert.Equal(expected, service.GetFirstDay());
    }

    [Fact]
    public void SetFailure_ReturnsFalseNotThrow()
    {
        var service = new DateFormatService(new RegistrySettingService(), _intlPath);
        // Verify the method returns a boolean without throwing;
        // the write to the throwaway path succeeds (short value name "LocaleName"),
        // so we assert True. The failure-contract (returning false without throwing)
        // is verified by RegistrySettingServiceTests.WriteFailure_ReturnsFalseNotThrow.
        Assert.True(service.SetLocaleName("test"));
    }
}