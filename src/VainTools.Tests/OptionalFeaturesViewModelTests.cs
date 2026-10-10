using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VainTools.App.Services;
using VainTools.App.ViewModels;
using VainTools.Framework.Services;
using Xunit;

namespace VainTools.Tests;

public sealed class OptionalFeaturesViewModelTests
{
    private readonly Mock<IOptionalFeaturesService> _featuresServiceMock = new();
    private readonly Mock<IRegistryTweakService> _registryTweakServiceMock = new();
    private readonly Mock<IDialogService> _dialogServiceMock = new();
    private readonly Mock<IInfoBarService> _infoBarServiceMock = new();
    private readonly OptionalFeaturesViewModel _viewModel;

    public OptionalFeaturesViewModelTests()
    {
        _viewModel = new OptionalFeaturesViewModel(
            _featuresServiceMock.Object,
            _registryTweakServiceMock.Object,
            _dialogServiceMock.Object,
            _infoBarServiceMock.Object,
            NullLogger<OptionalFeaturesViewModel>.Instance);
    }

    [Fact]
    public void Constructor_SetsIsElevatedFromRegistry()
    {
        var elevatedRegistry = new Mock<IRegistryTweakService>();
        elevatedRegistry.SetupGet(x => x.IsElevated).Returns(true);

        var vm = new OptionalFeaturesViewModel(
            _featuresServiceMock.Object,
            elevatedRegistry.Object,
            _dialogServiceMock.Object,
            _infoBarServiceMock.Object,
            NullLogger<OptionalFeaturesViewModel>.Instance);

        Assert.True(vm.IsElevated);
        Assert.False(_viewModel.IsElevated);
    }

    [Fact]
    public void InitialState_IsCorrect()
    {
        Assert.Equal("Ready", _viewModel.StatusMessage);
        Assert.False(_viewModel.IsLoading);
        Assert.Equal(string.Empty, _viewModel.ErrorMessage);
        Assert.False(_viewModel.HasFeatures);
        Assert.Equal("No features", _viewModel.FeatureCountText);
        Assert.Empty(_viewModel.Features);
    }

    [Fact]
    public async Task RefreshAsync_PopulatesFeatures()
    {
        _featuresServiceMock
            .Setup(x => x.GetFeaturesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new OptionalFeature("Feature-A", "Enabled"),
                new OptionalFeature("Feature-B", "Disabled"),
            ]);

        await _viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(2, _viewModel.Features.Count);
        Assert.True(_viewModel.HasFeatures);
        Assert.False(_viewModel.IsLoading);
        Assert.Equal("2 features", _viewModel.FeatureCountText);
        Assert.Contains("2 features found", _viewModel.StatusMessage);
    }

    [Fact]
    public async Task RefreshAsync_ReportsZeroOneManyCounts()
    {
        _featuresServiceMock
            .Setup(x => x.GetFeaturesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        await _viewModel.RefreshCommand.ExecuteAsync(null);
        Assert.Equal("No features", _viewModel.FeatureCountText);

        _featuresServiceMock
            .Setup(x => x.GetFeaturesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new OptionalFeature("Feature-A", "Enabled")]);
        await _viewModel.RefreshCommand.ExecuteAsync(null);
        Assert.Equal("1 feature", _viewModel.FeatureCountText);
    }

    [Fact]
    public async Task RefreshAsync_WhenServiceFails_SetsErrorMessage()
    {
        _featuresServiceMock
            .Setup(x => x.GetFeaturesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DISM failure"));

        await _viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Contains("DISM failure", _viewModel.ErrorMessage);
        Assert.Contains("Could not load features", _viewModel.StatusMessage);
        Assert.False(_viewModel.IsLoading);
        _infoBarServiceMock.Verify(x => x.ShowError("Load failed", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task EnableFeatureAsync_WhenNotElevated_ShowsErrorAndSkipsService()
    {
        _viewModel.IsElevated = false;
        var feature = new OptionalFeature("Feature-B", "Disabled");

        await _viewModel.EnableFeatureCommand.ExecuteAsync(feature);

        Assert.Contains("Administrator rights", _viewModel.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        _featuresServiceMock.Verify(
            x => x.EnableFeatureAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EnableFeatureAsync_DoesNotConfirm()
    {
        _viewModel.IsElevated = true;
        _featuresServiceMock
            .Setup(x => x.EnableFeatureAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeatureChangeResult(false));
        _featuresServiceMock
            .Setup(x => x.GetFeaturesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var feature = new OptionalFeature("Feature-B", "Disabled");

        await _viewModel.EnableFeatureCommand.ExecuteAsync(feature);

        // Enable is not destructive: no confirmation dialog, straight to the service.
        _dialogServiceMock.Verify(
            x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        _featuresServiceMock.Verify(
            x => x.EnableFeatureAsync("Feature-B", It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Contains("Enabled Feature-B", _viewModel.StatusMessage);
    }

    [Fact]
    public async Task EnableFeatureAsync_OnSuccess_ReloadsList()
    {
        _viewModel.IsElevated = true;
        _featuresServiceMock
            .Setup(x => x.EnableFeatureAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeatureChangeResult(false));
        _featuresServiceMock
            .Setup(x => x.GetFeaturesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var feature = new OptionalFeature("Feature-B", "Disabled");

        await _viewModel.EnableFeatureCommand.ExecuteAsync(feature);

        // D-13: the list reloads after the mutation.
        _featuresServiceMock.Verify(
            x => x.GetFeaturesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnableFeatureAsync_WhenServiceFails_ShowsError()
    {
        _viewModel.IsElevated = true;
        _featuresServiceMock
            .Setup(x => x.EnableFeatureAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Access is denied"));
        var feature = new OptionalFeature("Feature-B", "Disabled");

        await _viewModel.EnableFeatureCommand.ExecuteAsync(feature);

        Assert.Contains("Access is denied", _viewModel.ErrorMessage);
        Assert.False(_viewModel.IsLoading);
        _infoBarServiceMock.Verify(x => x.ShowError("Enable failed", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task DisableFeatureAsync_ConfirmsBeforeExecuting()
    {
        _viewModel.IsElevated = true;
        _dialogServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _featuresServiceMock
            .Setup(x => x.DisableFeatureAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeatureChangeResult(false));
        _featuresServiceMock
            .Setup(x => x.GetFeaturesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var feature = new OptionalFeature("Feature-A", "Enabled");

        await _viewModel.DisableFeatureCommand.ExecuteAsync(feature);

        _dialogServiceMock.Verify(
            x => x.ConfirmAsync(
                "Disable Feature",
                It.Is<string>(m => m.Contains("Feature-A")),
                "Disable",
                It.IsAny<string>()),
            Times.Once);
        _featuresServiceMock.Verify(
            x => x.DisableFeatureAsync("Feature-A", It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Contains("Disabled Feature-A", _viewModel.StatusMessage);
    }

    [Fact]
    public async Task DisableFeatureAsync_WhenCancelled_DoesNotCallService()
    {
        _viewModel.IsElevated = true;
        _dialogServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);
        var feature = new OptionalFeature("Feature-A", "Enabled");

        await _viewModel.DisableFeatureCommand.ExecuteAsync(feature);

        _featuresServiceMock.Verify(
            x => x.DisableFeatureAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Contains("cancelled", _viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DisableFeatureAsync_WhenNotElevated_ShowsErrorAndSkipsConfirm()
    {
        _viewModel.IsElevated = false;
        var feature = new OptionalFeature("Feature-A", "Enabled");

        await _viewModel.DisableFeatureCommand.ExecuteAsync(feature);

        Assert.Contains("Administrator rights", _viewModel.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        _dialogServiceMock.Verify(
            x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task DisableFeatureAsync_OnSuccess_ReloadsList()
    {
        _viewModel.IsElevated = true;
        _dialogServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _featuresServiceMock
            .Setup(x => x.DisableFeatureAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeatureChangeResult(false));
        _featuresServiceMock
            .Setup(x => x.GetFeaturesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var feature = new OptionalFeature("Feature-A", "Enabled");

        await _viewModel.DisableFeatureCommand.ExecuteAsync(feature);

        _featuresServiceMock.Verify(
            x => x.GetFeaturesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnableFeatureAsync_RestartRequired_ShowsWarningAndReloads()
    {
        _viewModel.IsElevated = true;
        _featuresServiceMock
            .Setup(x => x.EnableFeatureAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeatureChangeResult(true));
        _featuresServiceMock
            .Setup(x => x.GetFeaturesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var feature = new OptionalFeature("Feature-B", "Disabled");

        await _viewModel.EnableFeatureCommand.ExecuteAsync(feature);

        _infoBarServiceMock.Verify(x => x.ShowWarning("Restart required", It.IsAny<string>()), Times.Once);
        _infoBarServiceMock.Verify(x => x.ShowError(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _featuresServiceMock.Verify(x => x.GetFeaturesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains("restart required", _viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DisableFeatureAsync_RestartRequired_ShowsWarningAndReloads()
    {
        _viewModel.IsElevated = true;
        _dialogServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _featuresServiceMock
            .Setup(x => x.DisableFeatureAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeatureChangeResult(true));
        _featuresServiceMock
            .Setup(x => x.GetFeaturesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var feature = new OptionalFeature("Feature-A", "Enabled");

        await _viewModel.DisableFeatureCommand.ExecuteAsync(feature);

        _infoBarServiceMock.Verify(x => x.ShowWarning("Restart required", It.IsAny<string>()), Times.Once);
        _infoBarServiceMock.Verify(x => x.ShowError(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _featuresServiceMock.Verify(x => x.GetFeaturesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains("restart required", _viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FeatureCommands_CannotExecute_WhileLoading()
    {
        var disabled = new OptionalFeature("B", "Disabled");
        var enabled = new OptionalFeature("A", "Enabled");

        _viewModel.IsElevated = true;
        _viewModel.IsLoading = true;

        Assert.False(_viewModel.EnableFeatureCommand.CanExecute(disabled));
        Assert.False(_viewModel.DisableFeatureCommand.CanExecute(enabled));
    }

    [Fact]
    public void FeatureCommands_CannotExecute_ForPendingFeature()
    {
        var enablePending = new OptionalFeature("A", "Enable Pending");
        var disablePending = new OptionalFeature("B", "Disable Pending");

        _viewModel.IsElevated = true;

        Assert.False(_viewModel.EnableFeatureCommand.CanExecute(enablePending));
        Assert.False(_viewModel.DisableFeatureCommand.CanExecute(enablePending));
        Assert.False(_viewModel.EnableFeatureCommand.CanExecute(disablePending));
        Assert.False(_viewModel.DisableFeatureCommand.CanExecute(disablePending));
    }

    [Fact]
    public void OptionalFeature_IsPending_OnlyForPendingStates()
    {
        Assert.False(new OptionalFeature("A", "Enabled").IsPending);
        Assert.False(new OptionalFeature("A", "Disabled").IsPending);
        Assert.False(new OptionalFeature("A", "Disabled with Payload Removed").IsPending);
        Assert.True(new OptionalFeature("A", "Enable Pending").IsPending);
        Assert.True(new OptionalFeature("A", "Disable Pending").IsPending);
    }

    [Fact]
    public void FeatureCommands_CanExecute_ReflectElevationAndState()
    {
        var enabled = new OptionalFeature("A", "Enabled");
        var disabled = new OptionalFeature("B", "Disabled");

        _viewModel.IsElevated = true;
        Assert.False(_viewModel.EnableFeatureCommand.CanExecute(enabled));
        Assert.True(_viewModel.EnableFeatureCommand.CanExecute(disabled));
        Assert.True(_viewModel.DisableFeatureCommand.CanExecute(enabled));
        Assert.False(_viewModel.DisableFeatureCommand.CanExecute(disabled));

        _viewModel.IsElevated = false;
        Assert.False(_viewModel.EnableFeatureCommand.CanExecute(disabled));
        Assert.False(_viewModel.DisableFeatureCommand.CanExecute(enabled));

        Assert.False(_viewModel.EnableFeatureCommand.CanExecute(null));
        Assert.False(_viewModel.DisableFeatureCommand.CanExecute(null));
    }
}
