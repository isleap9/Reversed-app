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
        var registry = new RegistrySettingService();
        var service = new DateFormatService(registry, _intlPath);
        Assert.True(service.SetFirstDay(1));
        Assert.Equal(1, service.GetFirstDay());
        // The real iFirstDayOfWeek representation is REG_SZ "1", not REG_DWORD.
        Assert.Equal("1", registry.ReadString(new RegistryValue(RegistryHive.CurrentUser, _intlPath, "iFirstDayOfWeek")));
    }

    [Fact]
    public void FirstDay_Unparseable_ReturnsNull()
    {
        var registry = new RegistrySettingService();
        var service = new DateFormatService(registry, _intlPath);
        Assert.True(registry.WriteString(new RegistryValue(RegistryHive.CurrentUser, _intlPath, "iFirstDayOfWeek"), "not-a-number"));
        Assert.Null(service.GetFirstDay());
    }

    [Fact]
    public void SetFailure_ReturnsFalseNotThrow()
    {
        var service = new DateFormatService(new RegistrySettingService(), _intlPath);
        // A value name past the 16383-char registry limit forces a real failure
        // of this service: it must return false without throwing.
        Assert.False(service.SetValue(new string('v', 20000), "x"));
    }
}
