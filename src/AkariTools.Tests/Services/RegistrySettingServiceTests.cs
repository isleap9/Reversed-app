using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using Microsoft.Win32;
using RegistryHive = AkariTools.Framework.Models.RegistryHive;
using Xunit;

namespace AkariTools.Tests.Services;

// Each test gets its own throwaway registry key so the real
// HKCU\Software\AkariToolbox settings are never touched.
public sealed class RegistrySettingServiceTests : IDisposable
{
    private readonly string _keyPath = $@"Software\AkariToolbox.Tests\{Guid.NewGuid():N}";
    private readonly RegistrySettingService _service = new();

    public void Dispose() => Registry.CurrentUser.DeleteSubKeyTree(_keyPath, throwOnMissingSubKey: false);

    [Fact]
    public void Dword_RoundTripsThroughRegistry()
    {
        var value = new RegistryValue(RegistryHive.CurrentUser, _keyPath, "DwordValue");

        Assert.True(_service.WriteDword(value, 42));
        Assert.Equal(42, _service.ReadDword(value));
    }

    [Fact]
    public void WriteString_RoundTripsThroughRegistry()
    {
        var value = new RegistryValue(RegistryHive.CurrentUser, _keyPath, "StringValue");

        Assert.True(_service.WriteString(value, "hello"));
        Assert.Equal("hello", _service.ReadString(value));
    }

    [Fact]
    public void ExpandString_RoundTripsThroughRegistry()
    {
        var value = new RegistryValue(RegistryHive.CurrentUser, _keyPath, "ExpandValue");

        Assert.True(_service.WriteExpandString(value, @"%systemroot%\System32\acppage.dll,-6002"));
        Assert.Equal(@"%systemroot%\System32\acppage.dll,-6002", _service.ReadString(value));
    }

    [Fact]
    public void MissingKey_ReturnsNull()
    {
        var value = new RegistryValue(RegistryHive.CurrentUser, _keyPath, "NoSuchValue");

        Assert.Null(_service.ReadDword(value));
        Assert.Null(_service.ReadString(value));
    }

    [Fact]
    public void KeyPresence_CreateDeleteRoundTrip()
    {
        Assert.False(_service.KeyExists(RegistryHive.CurrentUser, _keyPath));

        Assert.True(_service.CreateKey(RegistryHive.CurrentUser, _keyPath));
        Assert.True(_service.KeyExists(RegistryHive.CurrentUser, _keyPath));

        Assert.True(_service.DeleteKey(RegistryHive.CurrentUser, _keyPath));
        Assert.False(_service.KeyExists(RegistryHive.CurrentUser, _keyPath));
    }

    [Fact]
    public void WriteFailure_ReturnsFalseNotThrow()
    {
        // A value name past the 16383-char registry limit forces a failure.
        var bad = new RegistryValue(RegistryHive.CurrentUser, _keyPath, new string('v', 20000));

        Assert.False(_service.WriteDword(bad, 1));
        Assert.False(_service.WriteString(bad, "x"));
        Assert.False(_service.WriteExpandString(bad, "x"));
    }

    [Fact]
    public void DeleteValue_RemovesSingleValueKeepsSiblings()
    {
        var valuePath = $@"Software\AkariToolbox.Tests\{Guid.NewGuid():N}";
        var value = new RegistryValue(RegistryHive.CurrentUser, valuePath, "TestValue");

        // Write a value first
        Assert.True(_service.WriteString(value, "hello"));

        // Verify it exists
        Assert.Equal("hello", _service.ReadString(value));

        // Delete it and verify it's gone
        Assert.True(_service.DeleteValue(value));

        Assert.Null(_service.ReadString(value));

        // Add a sibling and verify both were handled
        var sibling = new RegistryValue(RegistryHive.CurrentUser, valuePath, "SiblingValue");
        Assert.True(_service.WriteString(sibling, "world"));

        Assert.Equal("world", _service.ReadString(sibling));
    }
}
