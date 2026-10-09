using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VainTools.App.Services;
using VainTools.App.ViewModels;
using VainTools.Framework.Services;
using Xunit;

namespace VainTools.Tests;

public sealed class StoreViewModelTests
{
    private readonly Mock<IStoreService> _storeServiceMock = new();
    private readonly Mock<IRegistryTweakService> _registryTweakServiceMock = new();
    private readonly Mock<IDialogService> _dialogServiceMock = new();
    private readonly Mock<IInfoBarService> _infoBarServiceMock = new();
    private readonly StoreViewModel _viewModel;

    public StoreViewModelTests()
    {
        _viewModel = new StoreViewModel(
            _storeServiceMock.Object,
            _registryTweakServiceMock.Object,
            _dialogServiceMock.Object,
            _infoBarServiceMock.Object,
            NullLogger<StoreViewModel>.Instance);
    }

    [Fact]
    public void Constructor_SetsIsElevatedFromRegistry()
    {
        var elevatedRegistry = new Mock<IRegistryTweakService>();
        elevatedRegistry.SetupGet(x => x.IsElevated).Returns(true);

        var vm = new StoreViewModel(
            _storeServiceMock.Object,
            elevatedRegistry.Object,
            _dialogServiceMock.Object,
            _infoBarServiceMock.Object,
            NullLogger<StoreViewModel>.Instance);

        Assert.True(vm.IsElevated);
        Assert.False(_viewModel.IsElevated);
    }

    [Fact]
    public void InitialState_IsCorrect()
    {
        Assert.Equal("Ready", _viewModel.StatusMessage);
        Assert.False(_viewModel.IsLoading);
        Assert.False(_viewModel.IsElevated);
        Assert.Equal(string.Empty, _viewModel.ErrorMessage);
        Assert.Equal(string.Empty, _viewModel.SearchQuery);
        Assert.False(_viewModel.HasApps);
        Assert.Equal("No apps", _viewModel.AppCountText);
        Assert.Empty(_viewModel.Apps);
    }

    [Fact]
    public async Task SearchAsync_PopulatesApps()
    {
        _storeServiceMock
            .Setup(x => x.SearchApps("7zip"))
            .Returns([
                new StoreApp("7-Zip", "7zip.7zip", "26.04", "winget"),
                new StoreApp("NanaZip", "M2Team.NanaZip", "7.0.0", "winget"),
            ]);
        _viewModel.SearchQuery = "7zip";

        await _viewModel.SearchCommand.ExecuteAsync(null);

        Assert.Equal(2, _viewModel.Apps.Count);
        Assert.True(_viewModel.HasApps);
        Assert.False(_viewModel.IsLoading);
        Assert.Equal("2 apps", _viewModel.AppCountText);
        Assert.Contains("2 apps found", _viewModel.StatusMessage);
    }

    [Fact]
    public async Task SearchAsync_ReportsZeroOneManyCounts()
    {
        _storeServiceMock
            .Setup(x => x.SearchApps("7zip"))
            .Returns([]);
        _viewModel.SearchQuery = "7zip";
        await _viewModel.SearchCommand.ExecuteAsync(null);
        Assert.Equal("No apps", _viewModel.AppCountText);
        Assert.False(_viewModel.HasApps);

        _storeServiceMock
            .Setup(x => x.SearchApps("7zip"))
            .Returns([new StoreApp("7-Zip", "7zip.7zip", "26.04", "winget")]);
        await _viewModel.SearchCommand.ExecuteAsync(null);
        Assert.Equal("1 app", _viewModel.AppCountText);
        Assert.True(_viewModel.HasApps);
    }

    [Fact]
    public async Task SearchAsync_WhenServiceFails_ShowsUserFriendlyError()
    {
        _storeServiceMock
            .Setup(x => x.SearchApps("7zip"))
            .Throws(new InvalidOperationException("The system cannot find the file specified"));
        _viewModel.SearchQuery = "7zip";

        await _viewModel.SearchCommand.ExecuteAsync(null);

        Assert.Contains("The system cannot find", _viewModel.ErrorMessage);
        Assert.Contains("Could not search apps", _viewModel.StatusMessage);
        Assert.False(_viewModel.IsLoading);
        // D-12 / T-06-14: a user-friendly sentence on screen, not the raw exception.
        _infoBarServiceMock.Verify(
            x => x.ShowError(
                "Search failed",
                It.Is<string>(m => m.Contains("winget is available") && m.Contains("network connection"))),
            Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyQuery_DoesNotCallService()
    {
        await _viewModel.SearchCommand.ExecuteAsync(null);

        _storeServiceMock.Verify(x => x.SearchApps(It.IsAny<string>()), Times.Never);
        Assert.Contains("no query", _viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(_viewModel.Apps);
    }

    [Fact]
    public async Task RefreshAsync_ReRunsCurrentSearch()
    {
        _storeServiceMock
            .Setup(x => x.SearchApps("7zip"))
            .Returns([new StoreApp("7-Zip", "7zip.7zip", "26.04", "winget")]);
        _viewModel.SearchQuery = "7zip";

        await _viewModel.RefreshCommand.ExecuteAsync(null);
        await _viewModel.RefreshCommand.ExecuteAsync(null);

        _storeServiceMock.Verify(x => x.SearchApps("7zip"), Times.Exactly(2));
    }

    [Fact]
    public async Task InstallAppAsync_WhenNotElevated_ShowsErrorAndSkipsConfirmAndService()
    {
        _viewModel.IsElevated = false;
        var app = new StoreApp("7-Zip", "7zip.7zip", "26.04", "winget");

        await _viewModel.InstallAppCommand.ExecuteAsync(app);

        Assert.Contains("Administrator rights", _viewModel.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        _dialogServiceMock.Verify(
            x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        _storeServiceMock.Verify(x => x.InstallAppAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InstallAppAsync_ConfirmsBeforeExecutingAndNamesAppAndId()
    {
        // D-11 / T-06-10: the confirmation must name the app and its package id.
        _viewModel.IsElevated = true;
        _dialogServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _storeServiceMock
            .Setup(x => x.InstallAppAsync("7zip.7zip"))
            .ReturnsAsync(new ProcessResult(0, "Successfully installed", string.Empty));
        var app = new StoreApp("7-Zip", "7zip.7zip", "26.04", "winget");

        await _viewModel.InstallAppCommand.ExecuteAsync(app);

        _dialogServiceMock.Verify(
            x => x.ConfirmAsync(
                "Install",
                It.Is<string>(m => m.Contains("7-Zip") && m.Contains("7zip.7zip")),
                "Install",
                It.IsAny<string>()),
            Times.Once);
        _storeServiceMock.Verify(x => x.InstallAppAsync("7zip.7zip"), Times.Once);
        Assert.Contains("Installed 7-Zip", _viewModel.StatusMessage);
    }

    [Fact]
    public async Task InstallAppAsync_UsesThePackageIdNotTheDisplayName()
    {
        _viewModel.IsElevated = true;
        _dialogServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _storeServiceMock
            .Setup(x => x.InstallAppAsync("7zip.7zip"))
            .ReturnsAsync(new ProcessResult(0, "Successfully installed", string.Empty));

        // winget install runs with --exact on the id, never on the display name.
        await _viewModel.InstallAppCommand.ExecuteAsync(
            new StoreApp("7-Zip", "7zip.7zip", "26.04", "winget"));

        _storeServiceMock.Verify(x => x.InstallAppAsync("7-Zip"), Times.Never);
        _storeServiceMock.Verify(x => x.InstallAppAsync("7zip.7zip"), Times.Once);
    }

    [Fact]
    public async Task InstallAppAsync_ShowsProgressWhileWingetRuns()
    {
        var gate = new TaskCompletionSource<ProcessResult>();
        _viewModel.IsElevated = true;
        _dialogServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _storeServiceMock
            .Setup(x => x.InstallAppAsync("7zip.7zip"))
            .Returns(gate.Task);

        // The command suspends on the service call, so the in-flight state is observable.
        var running = _viewModel.InstallAppCommand.ExecuteAsync(
            new StoreApp("7-Zip", "7zip.7zip", "26.04", "winget"));

        Assert.Contains("Installing 7-Zip…", _viewModel.StatusMessage);
        Assert.True(_viewModel.IsLoading);
        Assert.False(running.IsCompleted);

        gate.SetResult(new ProcessResult(0, "Successfully installed", string.Empty));
        await running;

        Assert.False(_viewModel.IsLoading);
        Assert.Contains("Installed 7-Zip", _viewModel.StatusMessage);
    }

    [Fact]
    public async Task InstallAppAsync_WhenCancelled_DoesNotCallService()
    {
        _viewModel.IsElevated = true;
        _dialogServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);
        var app = new StoreApp("7-Zip", "7zip.7zip", "26.04", "winget");

        await _viewModel.InstallAppCommand.ExecuteAsync(app);

        _storeServiceMock.Verify(x => x.InstallAppAsync(It.IsAny<string>()), Times.Never);
        Assert.Contains("cancelled", _viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.False(_viewModel.IsLoading);
    }

    [Fact]
    public async Task InstallAppAsync_OnSuccess_ShowsSuccessAndClearsLoading()
    {
        _viewModel.IsElevated = true;
        _dialogServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _storeServiceMock
            .Setup(x => x.InstallAppAsync("7zip.7zip"))
            .ReturnsAsync(new ProcessResult(0, "Successfully installed", string.Empty));

        await _viewModel.InstallAppCommand.ExecuteAsync(
            new StoreApp("7-Zip", "7zip.7zip", "26.04", "winget"));

        _infoBarServiceMock.Verify(
            x => x.ShowSuccess("App installed", It.Is<string>(m => m.Contains("7-Zip"))),
            Times.Once);
        _infoBarServiceMock.Verify(x => x.ShowError(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        Assert.False(_viewModel.IsLoading);
        Assert.Equal(string.Empty, _viewModel.ErrorMessage);
    }

    [Fact]
    public async Task InstallAppAsync_OnNonZeroExit_ShowsWingetError()
    {
        // D-12: winget's own message is surfaced; it is written for a console user.
        _viewModel.IsElevated = true;
        _dialogServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _storeServiceMock
            .Setup(x => x.InstallAppAsync("7zip.7zip"))
            .ReturnsAsync(new ProcessResult(
                -1978335294, string.Empty, "No package found matching input criteria."));

        await _viewModel.InstallAppCommand.ExecuteAsync(
            new StoreApp("7-Zip", "7zip.7zip", "26.04", "winget"));

        Assert.Contains("No package found matching input criteria.", _viewModel.ErrorMessage);
        Assert.Contains("Could not install 7-Zip", _viewModel.StatusMessage);
        Assert.False(_viewModel.IsLoading);
        _infoBarServiceMock.Verify(
            x => x.ShowError("Install failed", It.Is<string>(m => m.Contains("No package found"))),
            Times.Once);
        _infoBarServiceMock.Verify(x => x.ShowSuccess(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InstallAppAsync_OnEmptyStderr_ReportsExitCode()
    {
        _viewModel.IsElevated = true;
        _dialogServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _storeServiceMock
            .Setup(x => x.InstallAppAsync("7zip.7zip"))
            .ReturnsAsync(new ProcessResult(42, string.Empty, "   "));

        await _viewModel.InstallAppCommand.ExecuteAsync(
            new StoreApp("7-Zip", "7zip.7zip", "26.04", "winget"));

        Assert.Contains("42", _viewModel.ErrorMessage);
    }

    [Fact]
    public async Task InstallAppAsync_WhenServiceThrows_ShowsErrorAndKeepsDetailInLog()
    {
        _viewModel.IsElevated = true;
        _dialogServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _storeServiceMock
            .Setup(x => x.InstallAppAsync("7zip.7zip"))
            .ThrowsAsync(new InvalidOperationException("winget.exe was not found"));

        await _viewModel.InstallAppCommand.ExecuteAsync(
            new StoreApp("7-Zip", "7zip.7zip", "26.04", "winget"));

        Assert.Contains("winget.exe was not found", _viewModel.ErrorMessage);
        Assert.Contains("Could not install 7-Zip", _viewModel.StatusMessage);
        Assert.False(_viewModel.IsLoading);
        _infoBarServiceMock.Verify(x => x.ShowError("Install failed", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task InstallAppCommand_CannotExecute_WhenNotElevated()
    {
        var app = new StoreApp("7-Zip", "7zip.7zip", "26.04", "winget");

        _viewModel.IsElevated = false;
        Assert.False(_viewModel.InstallAppCommand.CanExecute(app));

        _viewModel.IsElevated = true;
        Assert.True(_viewModel.InstallAppCommand.CanExecute(app));

        Assert.False(_viewModel.InstallAppCommand.CanExecute(null));
    }

    [Fact]
    public async Task InstallAppCommand_CanExecute_ReflectsIsLoading()
    {
        var app = new StoreApp("7-Zip", "7zip.7zip", "26.04", "winget");
        _viewModel.IsElevated = true;
        Assert.True(_viewModel.InstallAppCommand.CanExecute(app));

        _viewModel.IsLoading = true;
        Assert.False(_viewModel.InstallAppCommand.CanExecute(app));

        _viewModel.IsLoading = false;
        Assert.True(_viewModel.InstallAppCommand.CanExecute(app));
    }

    [Fact]
    public async Task SearchAsync_SurfacesFailureWhenWingetReturnsNoResults()
    {
        // An empty result is a valid search outcome: the page shows its empty state, not
        // an error.
        _storeServiceMock.Setup(x => x.SearchApps("zzzz")).Returns([]);
        _viewModel.SearchQuery = "zzzz";

        await _viewModel.SearchCommand.ExecuteAsync(null);

        Assert.Equal("No apps", _viewModel.AppCountText);
        Assert.Equal(string.Empty, _viewModel.ErrorMessage);
        _infoBarServiceMock.Verify(x => x.ShowError(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
