using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VainTools.App.Services;
using VainTools.App.ViewModels;
using VainTools.Framework.Services;
using Xunit;

namespace VainTools.Tests;

public sealed class AppxManagerViewModelTests
{
    private readonly Mock<IAppxPackageService> _appxServiceMock = new();
    private readonly Mock<IRegistryTweakService> _registryTweakServiceMock = new();
    private readonly Mock<IDialogService> _dialogServiceMock = new();
    private readonly Mock<IInfoBarService> _infoBarServiceMock = new();
    private readonly AppxManagerViewModel _viewModel;

    public AppxManagerViewModelTests()
    {
        _viewModel = new AppxManagerViewModel(
            _appxServiceMock.Object,
            _registryTweakServiceMock.Object,
            _dialogServiceMock.Object,
            _infoBarServiceMock.Object,
            NullLogger<AppxManagerViewModel>.Instance);
    }

    [Fact]
    public void Constructor_SetsIsElevatedFromRegistry()
    {
        var elevatedRegistry = new Mock<IRegistryTweakService>();
        elevatedRegistry.SetupGet(x => x.IsElevated).Returns(true);

        var vm = new AppxManagerViewModel(
            _appxServiceMock.Object,
            elevatedRegistry.Object,
            _dialogServiceMock.Object,
            _infoBarServiceMock.Object,
            NullLogger<AppxManagerViewModel>.Instance);

        Assert.True(vm.IsElevated);
        Assert.False(_viewModel.IsElevated);
    }

    [Fact]
    public void InitialState_IsCorrect()
    {
        Assert.Equal("Ready", _viewModel.StatusMessage);
        Assert.False(_viewModel.IsLoading);
        Assert.Equal(string.Empty, _viewModel.ErrorMessage);
        Assert.False(_viewModel.HasPackages);
        Assert.Empty(_viewModel.Packages);
    }

    [Fact]
    public async Task RefreshAsync_PopulatesInstalledAndProvisioned()
    {
        _appxServiceMock
            .Setup(x => x.GetInstalledPackages())
            .Returns([new AppxPackage("Full.A_1.0", "A", "Pub", "1.0.0.0", @"C:\A", false)]);
        _appxServiceMock
            .Setup(x => x.GetProvisionedPackages())
            .Returns([new AppxPackage("Full.B_1.0", "B", "Pub", "1.0.0.0", @"C:\B", true)]);

        await _viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(2, _viewModel.Packages.Count);
        Assert.True(_viewModel.HasPackages);
        Assert.False(_viewModel.IsLoading);
        Assert.Contains("2 packages found", _viewModel.StatusMessage);
    }

    [Fact]
    public async Task RefreshAsync_WhenServiceFails_SetsErrorMessage()
    {
        _appxServiceMock
            .Setup(x => x.GetInstalledPackages())
            .Throws(new InvalidOperationException("WinRT failure"));

        await _viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Contains("WinRT failure", _viewModel.ErrorMessage);
        Assert.Contains("Could not load packages", _viewModel.StatusMessage);
        Assert.False(_viewModel.IsLoading);
        _infoBarServiceMock.Verify(x => x.ShowError("Load failed", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RemovePackageAsync_WhenNotElevated_ShowsErrorAndSkipsService()
    {
        _viewModel.IsElevated = false;
        var package = new AppxPackage("Full.A_1.0", "A", "Pub", "1.0.0.0", @"C:\A", false);

        await _viewModel.RemovePackageCommand.ExecuteAsync(package);

        Assert.Contains("Administrator rights", _viewModel.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        _appxServiceMock.Verify(x => x.RemovePackageAsync(It.IsAny<string>()), Times.Never);
        _dialogServiceMock.Verify(
            x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task RemovePackageAsync_ConfirmsBeforeExecuting()
    {
        _viewModel.IsElevated = true;
        _dialogServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _appxServiceMock
            .Setup(x => x.RemovePackageAsync(It.IsAny<string>()))
            .ReturnsAsync(new DeploymentResult(true, null));
        var package = new AppxPackage("Full.A_1.0", "A", "Pub", "1.0.0.0", @"C:\A", false);

        await _viewModel.RemovePackageCommand.ExecuteAsync(package);

        _dialogServiceMock.Verify(
            x => x.ConfirmAsync("Remove Package", It.Is<string>(m => m.Contains("A")), "Remove", It.IsAny<string>()),
            Times.Once);
        _appxServiceMock.Verify(x => x.RemovePackageAsync("Full.A_1.0"), Times.Once);
    }

    [Fact]
    public async Task RemovePackageAsync_WhenCancelled_DoesNotCallService()
    {
        _viewModel.IsElevated = true;
        _dialogServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);
        var package = new AppxPackage("Full.A_1.0", "A", "Pub", "1.0.0.0", @"C:\A", false);

        await _viewModel.RemovePackageCommand.ExecuteAsync(package);

        _appxServiceMock.Verify(x => x.RemovePackageAsync(It.IsAny<string>()), Times.Never);
        Assert.Contains("cancelled", _viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RemovePackageAsync_OnSuccess_ReloadsList()
    {
        _viewModel.IsElevated = true;
        _dialogServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _appxServiceMock
            .Setup(x => x.RemovePackageAsync(It.IsAny<string>()))
            .ReturnsAsync(new DeploymentResult(true, null));
        _appxServiceMock.Setup(x => x.GetInstalledPackages()).Returns([]);
        _appxServiceMock.Setup(x => x.GetProvisionedPackages()).Returns([]);
        var package = new AppxPackage("Full.A_1.0", "A", "Pub", "1.0.0.0", @"C:\A", false);

        await _viewModel.RemovePackageCommand.ExecuteAsync(package);

        // RemovePackageAsync reloads via RefreshAsync, which calls both enumerations.
        _appxServiceMock.Verify(x => x.GetInstalledPackages(), Times.Once);
        _appxServiceMock.Verify(x => x.GetProvisionedPackages(), Times.Once);
        Assert.Contains("Removed A", _viewModel.StatusMessage);
    }

    [Fact]
    public async Task RemovePackageAsync_WhenResultFails_ShowsError()
    {
        _viewModel.IsElevated = true;
        _dialogServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _appxServiceMock
            .Setup(x => x.RemovePackageAsync(It.IsAny<string>()))
            .ReturnsAsync(new DeploymentResult(false, "Access is denied"));
        var package = new AppxPackage("Full.A_1.0", "A", "Pub", "1.0.0.0", @"C:\A", false);

        await _viewModel.RemovePackageCommand.ExecuteAsync(package);

        Assert.Contains("Access is denied", _viewModel.ErrorMessage);
        _infoBarServiceMock.Verify(x => x.ShowError("Remove failed", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RemovePackageAsync_WhenServiceThrows_ShowsError()
    {
        _viewModel.IsElevated = true;
        _dialogServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _appxServiceMock
            .Setup(x => x.RemovePackageAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("WinRT failure"));
        var package = new AppxPackage("Full.A_1.0", "A", "Pub", "1.0.0.0", @"C:\A", false);

        await _viewModel.RemovePackageCommand.ExecuteAsync(package);

        Assert.Contains("WinRT failure", _viewModel.ErrorMessage);
        Assert.False(_viewModel.IsLoading);
    }

    [Fact]
    public void RemovePackageCommand_CanExecute_RequiresPackageAndElevation()
    {
        var package = new AppxPackage("Full.A_1.0", "A", "Pub", "1.0.0.0", @"C:\A", false);

        _viewModel.IsElevated = true;
        Assert.True(_viewModel.RemovePackageCommand.CanExecute(package));

        _viewModel.IsElevated = false;
        Assert.False(_viewModel.RemovePackageCommand.CanExecute(package));

        Assert.False(_viewModel.RemovePackageCommand.CanExecute(null));
    }
}
