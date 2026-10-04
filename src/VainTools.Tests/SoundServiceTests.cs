using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32;
using Moq;
using VainTools.App.Services;
using Xunit;

namespace VainTools.Tests;

/// <summary>
/// Covers the registry-backed settings mapping in <see cref="SoundService"/>.
/// The WASAPI device/volume paths need real audio hardware and stay runtime-only.
/// </summary>
public sealed class SoundServiceTests
{
    private readonly Mock<IRegistryTweakService> _registryMock = new();
    private readonly SoundService _service;

    public SoundServiceTests()
    {
        _service = new SoundService(_registryMock.Object, NullLogger<SoundService>.Instance);
    }

    [Fact]
    public async Task GetSpatialAudioSettingsAsync_MapsRegistryValues()
    {
        _registryMock
            .Setup(x => x.ReadDword(RegistryHive.LocalMachine, It.IsAny<string>(), "SpatialAudioEnabled"))
            .Returns(1);
        _registryMock
            .Setup(x => x.ReadDword(RegistryHive.LocalMachine, It.IsAny<string>(), "SpatialAudioType"))
            .Returns(2);

        var settings = await _service.GetSpatialAudioSettingsAsync();

        Assert.True(settings.Enabled);
        Assert.Equal(2, settings.Type);
    }

    [Fact]
    public async Task SetSpatialAudioSettingsAsync_WritesBothValues()
    {
        await _service.SetSpatialAudioSettingsAsync(new SpatialAudioSettings(true, 3));

        _registryMock.Verify(
            x => x.WriteDword(RegistryHive.LocalMachine, It.IsAny<string>(), "SpatialAudioEnabled", 1),
            Times.Once);
        _registryMock.Verify(
            x => x.WriteDword(RegistryHive.LocalMachine, It.IsAny<string>(), "SpatialAudioType", 3),
            Times.Once);
    }

    [Fact]
    public async Task GetEnhancementSettingsAsync_DefaultsToEnabledWhenUnset()
    {
        _registryMock
            .Setup(x => x.ReadDword(RegistryHive.LocalMachine, It.IsAny<string>(), It.IsAny<string>()))
            .Returns((int?)null);

        var settings = await _service.GetEnhancementSettingsAsync();

        Assert.True(settings.Enabled);
        Assert.False(settings.LoudnessEqualization);
    }

    [Fact]
    public async Task GetEnhancementSettingsAsync_MapsAllFlags()
    {
        _registryMock
            .Setup(x => x.ReadDword(RegistryHive.LocalMachine, It.IsAny<string>(), "AudioEnhancementsEnabled"))
            .Returns(1);
        _registryMock
            .Setup(x => x.ReadDword(RegistryHive.LocalMachine, It.IsAny<string>(), "LoudnessEqualization"))
            .Returns(1);
        _registryMock
            .Setup(x => x.ReadDword(RegistryHive.LocalMachine, It.IsAny<string>(), "BassBoost"))
            .Returns(0);
        _registryMock
            .Setup(x => x.ReadDword(RegistryHive.LocalMachine, It.IsAny<string>(), "VirtualSurround"))
            .Returns(1);
        _registryMock
            .Setup(x => x.ReadDword(RegistryHive.LocalMachine, It.IsAny<string>(), "RoomCorrection"))
            .Returns(0);

        var settings = await _service.GetEnhancementSettingsAsync();

        Assert.True(settings.Enabled);
        Assert.True(settings.LoudnessEqualization);
        Assert.False(settings.BassBoost);
        Assert.True(settings.VirtualSurround);
        Assert.False(settings.RoomCorrection);
    }

    [Fact]
    public async Task SetEnhancementSettingsAsync_WritesAllFiveValues()
    {
        await _service.SetEnhancementSettingsAsync(
            new AudioEnhancementSettings(true, true, false, true, false));

        _registryMock.Verify(
            x => x.WriteDword(RegistryHive.LocalMachine, It.IsAny<string>(), "AudioEnhancementsEnabled", 1),
            Times.Once);
        _registryMock.Verify(
            x => x.WriteDword(RegistryHive.LocalMachine, It.IsAny<string>(), "LoudnessEqualization", 1),
            Times.Once);
        _registryMock.Verify(
            x => x.WriteDword(RegistryHive.LocalMachine, It.IsAny<string>(), "BassBoost", 0),
            Times.Once);
        _registryMock.Verify(
            x => x.WriteDword(RegistryHive.LocalMachine, It.IsAny<string>(), "VirtualSurround", 1),
            Times.Once);
        _registryMock.Verify(
            x => x.WriteDword(RegistryHive.LocalMachine, It.IsAny<string>(), "RoomCorrection", 0),
            Times.Once);
    }
}
