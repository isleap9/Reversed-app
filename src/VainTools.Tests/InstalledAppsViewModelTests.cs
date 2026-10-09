using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VainTools.App.Services;
using VainTools.App.ViewModels;
using VainTools.Framework.Services;
using Xunit;

namespace VainTools.Tests;

public sealed class InstalledAppsViewModelTests
{
    private readonly Mock<IInstalledAppsService> _appsServiceMock = new();
    private readonly Mock<IRegistryTweakService> _registryTweakServiceMock = new();
    private readonly Mock<IDialogService> _dialogServiceMock = new();
    private readonly Mock<IInfoBarService> _infoBarServiceMock = new();
    private readonly TestableInstalledAppsViewModel _viewModel;

    public InstalledAppsViewModelTests()
    {
        _viewModel = new TestableInstalledAppsViewModel(
            _appsServiceMock.Object,
            _registryTweakServiceMock.Object,
            _dialogServiceMock.Object,
            _infoBarServiceMock.Object);
    }

    [Fact]
    public void Constructor_SetsIsElevatedFromRegistry()
    {
        var elevatedRegistry = new Mock<IRegistryTweakService>();
        elevatedRegistry.SetupGet(x => x.IsElevated).Returns(true);

        var vm = new TestableInstalledAppsViewModel(
            _appsServiceMock.Object,
            elevatedRegistry.Object,
            _dialogServiceMock.Object,
            _infoBarServiceMock.Object);

        Assert.True(vm.IsElevated);
        Assert.False(_viewModel.IsElevated);
    }

    [Fact]
    public void InitialState_IsCorrect()
    {
        Assert.Equal("Ready", _viewModel.StatusMessage);
        Assert.False(_viewModel.IsLoading);
        Assert.Equal(string.Empty, _viewModel.ErrorMessage);
        Assert.Equal(string.Empty, _viewModel.SearchQuery);
        Assert.False(_viewModel.HasPrograms);
        Assert.Equal("No programs", _viewModel.ProgramCountText);
        Assert.Empty(_viewModel.Programs);
    }

    [Fact]
    public async Task RefreshAsync_PopulatesPrograms()
    {
        _appsServiceMock
            .Setup(x => x.GetInstalledApps())
            .Returns([
                new InstalledApp("App A", "1.0", "Contoso", @"C:\A", "a.exe", string.Empty, "HKCU\\A"),
                new InstalledApp("App B", "2.0", "Fabrikam", @"C:\B", "b.exe", "b.exe /S", "HKCU\\B"),
            ]);

        await _viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(2, _viewModel.Programs.Count);
        Assert.True(_viewModel.HasPrograms);
        Assert.False(_viewModel.IsLoading);
        Assert.Equal("2 programs", _viewModel.ProgramCountText);
        Assert.Contains("2 programs found", _viewModel.StatusMessage);
    }

    [Fact]
    public async Task RefreshAsync_ReportsZeroOneManyCounts()
    {
        _appsServiceMock.Setup(x => x.GetInstalledApps()).Returns([]);
        await _viewModel.RefreshCommand.ExecuteAsync(null);
        Assert.Equal("No programs", _viewModel.ProgramCountText);

        _appsServiceMock
            .Setup(x => x.GetInstalledApps())
            .Returns([new InstalledApp("App A", "1.0", "Contoso", @"C:\A", "a.exe", string.Empty, "R")]);
        await _viewModel.RefreshCommand.ExecuteAsync(null);
        Assert.Equal("1 program", _viewModel.ProgramCountText);
    }

    [Fact]
    public async Task RefreshAsync_WhenServiceFails_SetsErrorMessage()
    {
        _appsServiceMock
            .Setup(x => x.GetInstalledApps())
            .Throws(new InvalidOperationException("Registry access failed"));

        await _viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Contains("Registry access failed", _viewModel.ErrorMessage);
        Assert.Contains("Could not load programs", _viewModel.StatusMessage);
        Assert.False(_viewModel.IsLoading);
        _infoBarServiceMock.Verify(x => x.ShowError("Load failed", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task UninstallAsync_WhenNotElevated_ShowsErrorAndSkipsService()
    {
        _viewModel.IsElevated = false;
        var app = new InstalledApp("App A", "1.0", "Contoso", @"C:\A", "a.exe", string.Empty, "HKCU\\A");

        await _viewModel.UninstallCommand.ExecuteAsync(app);

        Assert.Contains("Administrator rights", _viewModel.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(_viewModel.Launched);
        _dialogServiceMock.Verify(
            x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task UninstallAsync_WhenNoUninstallCommand_ShowsErrorAndSkipsLaunch()
    {
        _viewModel.IsElevated = true;
        var app = new InstalledApp("App A", "1.0", "Contoso", @"C:\A", string.Empty, string.Empty, "HKCU\\A");

        await _viewModel.UninstallCommand.ExecuteAsync(app);

        Assert.Contains("No uninstall command", _viewModel.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(_viewModel.Launched);
    }

    [Fact]
    public async Task UninstallAsync_ConfirmsBeforeExecuting()
    {
        _viewModel.IsElevated = true;
        _dialogServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        var app = new InstalledApp("App A", "1.0", "Contoso", @"C:\A", "a.exe", string.Empty, "HKCU\\A");

        await _viewModel.UninstallCommand.ExecuteAsync(app);

        // T-06-02/T-06-03: the raw command is displayed in the confirmation dialog.
        _dialogServiceMock.Verify(
            x => x.ConfirmAsync(
                "Uninstall",
                It.Is<string>(m => m.Contains("App A") && m.Contains("a.exe")),
                "Uninstall",
                It.IsAny<string>()),
            Times.Once);
        Assert.Single(_viewModel.Launched);
    }

    [Fact]
    public async Task UninstallAsync_UsesQuietUninstallStringWhenAvailable()
    {
        _viewModel.IsElevated = true;
        _dialogServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        var app = new InstalledApp("App A", "1.0", "Contoso", @"C:\A", "a.exe", "a.exe /S", "HKCU\\A");

        await _viewModel.UninstallCommand.ExecuteAsync(app);

        Assert.Equal(["a.exe /S"], _viewModel.Launched);
    }

    [Fact]
    public async Task UninstallAsync_UsesUninstallStringWhenNoQuietString()
    {
        _viewModel.IsElevated = true;
        _dialogServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        var app = new InstalledApp("App A", "1.0", "Contoso", @"C:\A", "a.exe", string.Empty, "HKCU\\A");

        await _viewModel.UninstallCommand.ExecuteAsync(app);

        Assert.Equal(["a.exe"], _viewModel.Launched);
    }

    [Fact]
    public void SelectUninstallCommand_PrefersQuietString()
    {
        var quiet = new InstalledApp("A", "1", "P", "L", "a.exe", "a.exe /S", "R");
        var loud = new InstalledApp("A", "1", "P", "L", "a.exe", string.Empty, "R");

        Assert.Equal("a.exe /S", InstalledAppsViewModel.SelectUninstallCommand(quiet));
        Assert.Equal("a.exe", InstalledAppsViewModel.SelectUninstallCommand(loud));
    }

    [Fact]
    public async Task UninstallAsync_WhenCancelled_DoesNotLaunch()
    {
        _viewModel.IsElevated = true;
        _dialogServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);
        var app = new InstalledApp("App A", "1.0", "Contoso", @"C:\A", "a.exe", string.Empty, "HKCU\\A");

        await _viewModel.UninstallCommand.ExecuteAsync(app);

        Assert.Empty(_viewModel.Launched);
        Assert.Contains("cancelled", _viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UninstallAsync_OnSuccess_ReloadsList()
    {
        _viewModel.IsElevated = true;
        _dialogServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _appsServiceMock.Setup(x => x.GetInstalledApps()).Returns([]);
        var app = new InstalledApp("App A", "1.0", "Contoso", @"C:\A", "a.exe", string.Empty, "HKCU\\A");

        await _viewModel.UninstallCommand.ExecuteAsync(app);

        _appsServiceMock.Verify(x => x.GetInstalledApps(), Times.Once);
        Assert.Contains("Uninstalled App A", _viewModel.StatusMessage);
    }

    [Fact]
    public async Task UninstallAsync_WhenLaunchFails_ShowsError()
    {
        _viewModel.IsElevated = true;
        _viewModel.LaunchException = new InvalidOperationException("No association");
        _dialogServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        var app = new InstalledApp("App A", "1.0", "Contoso", @"C:\A", "a.exe", string.Empty, "HKCU\\A");

        await _viewModel.UninstallCommand.ExecuteAsync(app);

        Assert.Contains("No association", _viewModel.ErrorMessage);
        _infoBarServiceMock.Verify(x => x.ShowError("Uninstall failed", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void CopyCommand_CopiesRawUninstallString()
    {
        var app = new InstalledApp("App A", "1.0", "Contoso", @"C:\A", "a.exe /X", "a.exe /S", "HKCU\\A");

        _viewModel.CopyCommandCommand.Execute(app);

        // The raw UninstallString is copied verbatim (D-04), not the quiet variant.
        Assert.Equal(["a.exe /X"], _viewModel.Copied);
        Assert.Contains("copied to clipboard", _viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CopyCommand_CanExecute_RequiresNonEmptyUninstallString()
    {
        var withCommand = new InstalledApp("A", "1", "P", "L", "a.exe", string.Empty, "R");
        var withoutCommand = new InstalledApp("B", "1", "P", "L", string.Empty, string.Empty, "R");

        Assert.True(_viewModel.CopyCommandCommand.CanExecute(withCommand));
        Assert.False(_viewModel.CopyCommandCommand.CanExecute(withoutCommand));
        Assert.False(_viewModel.CopyCommandCommand.CanExecute(null));
    }

    [Fact]
    public void UninstallCommand_CanExecute_RequiresAppAndElevation()
    {
        var app = new InstalledApp("App A", "1.0", "Contoso", @"C:\A", "a.exe", string.Empty, "HKCU\\A");

        _viewModel.IsElevated = true;
        Assert.True(_viewModel.UninstallCommand.CanExecute(app));

        _viewModel.IsElevated = false;
        Assert.False(_viewModel.UninstallCommand.CanExecute(app));

        Assert.False(_viewModel.UninstallCommand.CanExecute(null));
    }

    [Fact]
    public async Task SearchQuery_FiltersPrograms()
    {
        _appsServiceMock
            .Setup(x => x.GetInstalledApps())
            .Returns([
                new InstalledApp("Visual Studio", "17.0", "Microsoft", @"C:\VS", "vs.exe", string.Empty, "R1"),
                new InstalledApp("Notepad++", "8.0", "Don Ho", @"C:\N", "npp.exe", string.Empty, "R2"),
            ]);
        await _viewModel.RefreshCommand.ExecuteAsync(null);
        Assert.Equal(2, _viewModel.Programs.Count);

        _viewModel.SearchQuery = "visual";

        Assert.Single(_viewModel.Programs);
        Assert.Equal("Visual Studio", _viewModel.Programs[0].DisplayName);
        Assert.Equal("1 program", _viewModel.ProgramCountText);

        _viewModel.SearchQuery = "zzzz-no-match";
        Assert.Empty(_viewModel.Programs);
        Assert.False(_viewModel.HasPrograms);
        Assert.Equal("No programs", _viewModel.ProgramCountText);
    }

    /// <summary>
    /// Overrides the process-launch and clipboard seams so tests never touch the real system.
    /// </summary>
    private sealed class TestableInstalledAppsViewModel(
        IInstalledAppsService appsService,
        IRegistryTweakService registry,
        IDialogService dialogs,
        IInfoBarService infoBar)
        : InstalledAppsViewModel(
            appsService,
            registry,
            dialogs,
            infoBar,
            NullLogger<InstalledAppsViewModel>.Instance)
    {
        public List<string> Launched { get; } = [];
        public List<string> Copied { get; } = [];
        public Exception? LaunchException { get; set; }
        public int ExitCode { get; set; }

        protected override Task<int> RunUninstallAsync(UninstallLaunch launch)
        {
            if (LaunchException is not null)
            {
                throw LaunchException;
            }

            Launched.Add($"{launch.FileName} {launch.Arguments}".Trim());
            return Task.FromResult(ExitCode);
        }

        protected override void CopyToClipboardCore(string text) => Copied.Add(text);
    }
}
