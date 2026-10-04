using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VainTools.App.Services;
using VainTools.App.ViewModels;
using VainTools.Framework.Services;
using Xunit;

namespace VainTools.Tests;

public sealed class SoundPageViewModelTests
{
    private readonly Mock<ISoundService> _soundServiceMock = new();
    private readonly Mock<IRegistryTweakService> _registryTweakServiceMock = new();
    private readonly Mock<IDialogService> _dialogServiceMock = new();
    private readonly Mock<IInfoBarService> _infoBarServiceMock = new();
    private readonly SoundPageViewModel _viewModel;

    public SoundPageViewModelTests()
    {
        _soundServiceMock
            .Setup(x => x.GetDevicesAsync())
            .ReturnsAsync([]);

        _viewModel = new SoundPageViewModel(
            _soundServiceMock.Object,
            _registryTweakServiceMock.Object,
            _dialogServiceMock.Object,
            _infoBarServiceMock.Object,
            NullLogger<SoundPageViewModel>.Instance);
    }

    [Fact]
    public async Task SelectDeviceAsync_LoadsVolumeLevelAndMute()
    {
        _soundServiceMock
            .Setup(x => x.GetVolumeInfoAsync("id-1"))
            .ReturnsAsync(new VolumeInfo(0.42f, true, 0.0f, 1.0f));
        var device = new AudioDevice("id-1", "Speakers", true, true);

        await _viewModel.SelectDeviceCommand.ExecuteAsync(device);

        Assert.Equal(device, _viewModel.SelectedDevice);
        Assert.Equal(42.0, _viewModel.VolumeLevel, precision: 3);
        Assert.True(_viewModel.IsMuted);
        Assert.Contains("42", _viewModel.StatusMessage);
    }

    [Fact]
    public async Task SelectDeviceAsync_WithNull_ClearsSelectionWithoutServiceCall()
    {
        await _viewModel.SelectDeviceCommand.ExecuteAsync(null);

        Assert.Null(_viewModel.SelectedDevice);
        _soundServiceMock.Verify(x => x.GetVolumeInfoAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SelectDeviceAsync_WhenServiceFails_ReportsStatus()
    {
        _soundServiceMock
            .Setup(x => x.GetVolumeInfoAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("No such device"));
        var device = new AudioDevice("id-9", "Ghost", false, true);

        await _viewModel.SelectDeviceCommand.ExecuteAsync(device);

        Assert.Contains("Could not get volume", _viewModel.StatusMessage);
    }

    [Fact]
    public async Task IsUserVolumeChange_IgnoresProgrammaticSets()
    {
        _soundServiceMock
            .Setup(x => x.GetVolumeInfoAsync("id-1"))
            .ReturnsAsync(new VolumeInfo(0.42f, false, 0.0f, 1.0f));

        await _viewModel.SelectDeviceCommand.ExecuteAsync(new AudioDevice("id-1", "Speakers", true, true));

        Assert.False(_viewModel.IsUserVolumeChange(42.0));
        Assert.False(_viewModel.IsUserVolumeChange(42.004));
        Assert.True(_viewModel.IsUserVolumeChange(80.0));
    }

    [Fact]
    public async Task IsUserMuteChange_IgnoresProgrammaticSets()
    {
        _soundServiceMock
            .Setup(x => x.GetVolumeInfoAsync("id-1"))
            .ReturnsAsync(new VolumeInfo(0.5f, true, 0.0f, 1.0f));

        await _viewModel.SelectDeviceCommand.ExecuteAsync(new AudioDevice("id-1", "Speakers", true, true));

        Assert.False(_viewModel.IsUserMuteChange(true));
        Assert.True(_viewModel.IsUserMuteChange(false));
    }

    [Fact]
    public async Task SetVolumeAsync_WritesThroughServiceAndUpdatesReadout()
    {
        await _viewModel.SetVolumeAsync("id-1", 0.5f);

        _soundServiceMock.Verify(x => x.SetVolumeAsync("id-1", 0.5f), Times.Once);
        Assert.Equal(50.0, _viewModel.VolumeLevel, precision: 3);
        Assert.Equal("50 %", _viewModel.VolumeText);
    }

    [Fact]
    public async Task SetVolumeAsync_WhenServiceFails_ReportsError()
    {
        _soundServiceMock
            .Setup(x => x.SetVolumeAsync(It.IsAny<string>(), It.IsAny<float>()))
            .ThrowsAsync(new InvalidOperationException("Access denied"));

        await _viewModel.SetVolumeAsync("id-1", 0.5f);

        _infoBarServiceMock.Verify(
            x => x.ShowError("Volume change failed", It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task SetMuteAsync_WritesThroughService()
    {
        await _viewModel.SetMuteAsync("id-1", true);

        _soundServiceMock.Verify(x => x.SetMuteAsync("id-1", true), Times.Once);
        Assert.True(_viewModel.IsMuted);
    }
}
