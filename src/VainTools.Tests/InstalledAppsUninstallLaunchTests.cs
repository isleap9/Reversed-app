using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VainTools.App.Services;
using VainTools.App.ViewModels;
using VainTools.Framework.Services;
using Xunit;

namespace VainTools.Tests;

/// <summary>
/// End-to-end uninstall-launch tests. These construct <see cref="InstalledAppsViewModel"/>
/// directly — with no test subclass — so the REAL launch seam runs and real
/// <c>cmd.exe</c> processes start (CR-01: the old seam was overridden, which is why the
/// "system cannot find the file specified" defect shipped).
///
/// <para>
/// Only <c>cmd.exe /c exit N</c> style command lines are used. They are argument-bearing
/// (the exact failing shape), they exit immediately with a controlled code, and they
/// perform no system mutation.
/// </para>
/// </summary>
public sealed class InstalledAppsUninstallLaunchTests
{
    private const string ProbeRegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\VainToolsTest\Probe";

    private readonly Mock<IInstalledAppsService> _appsServiceMock = new();
    private readonly Mock<IRegistryTweakService> _registryTweakServiceMock = new();
    private readonly Mock<IDialogService> _dialogServiceMock = new();
    private readonly Mock<IInfoBarService> _infoBarServiceMock = new();
    private readonly InstalledAppsViewModel _viewModel;

    public InstalledAppsUninstallLaunchTests()
    {
        _registryTweakServiceMock.SetupGet(x => x.IsElevated).Returns(true);
        _dialogServiceMock
            .Setup(x => x.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // The list reloads empty after every launch unless a test says otherwise.
        _appsServiceMock.Setup(x => x.GetInstalledApps()).Returns([]);

        _viewModel = new InstalledAppsViewModel(
            _appsServiceMock.Object,
            _registryTweakServiceMock.Object,
            _dialogServiceMock.Object,
            _infoBarServiceMock.Object,
            NullLogger<InstalledAppsViewModel>.Instance);
    }

    [Fact]
    public async Task Uninstall_BareNameWithArguments_Launches()
    {
        var app = Probe("cmd.exe /c exit 0");

        await _viewModel.UninstallCommand.ExecuteAsync(app);

        AssertNoError();
        _infoBarServiceMock.Verify(
            x => x.ShowSuccess("Program uninstalled", It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task Uninstall_QuotedFullPathWithArguments_Launches()
    {
        var command = $"\"{Path.Combine(Environment.SystemDirectory, "cmd.exe")}\" /c exit 0";
        var app = Probe(command);

        await _viewModel.UninstallCommand.ExecuteAsync(app);

        AssertNoError();
        _infoBarServiceMock.Verify(
            x => x.ShowSuccess("Program uninstalled", It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task Uninstall_NonZeroExit_ReportsFailureWithCode()
    {
        var app = Probe("cmd.exe /c exit 7");

        await _viewModel.UninstallCommand.ExecuteAsync(app);

        Assert.Contains("7", _viewModel.ErrorMessage, StringComparison.Ordinal);
        _infoBarServiceMock.Verify(
            x => x.ShowError("Uninstall failed", It.Is<string>(m => m.Contains("7"))),
            Times.Once);
    }

    [Fact]
    public async Task Uninstall_PassesArgumentsVerbatim()
    {
        // The embedded quotes only survive if the argument text is handed over byte-for-byte.
        var app = Probe("cmd.exe /c if \"a b\"==\"a b\" exit 4");

        await _viewModel.UninstallCommand.ExecuteAsync(app);

        _infoBarServiceMock.Verify(
            x => x.ShowError("Uninstall failed", It.Is<string>(m => m.Contains("code 4"))),
            Times.Once);
    }

    [Fact]
    public async Task Uninstall_MissingExecutable_ReportsFailure()
    {
        var missing = $@"C:\VainToolsMissing-{Guid.NewGuid():N}\uninst.exe";
        var app = Probe($"\"{missing}\" /S");

        await _viewModel.UninstallCommand.ExecuteAsync(app);

        _infoBarServiceMock.Verify(x => x.ShowError("Uninstall failed", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Uninstall_RestartRequiredExit_WarnsRestart()
    {
        var app = Probe("cmd.exe /c exit 3010");

        await _viewModel.UninstallCommand.ExecuteAsync(app);

        _infoBarServiceMock.Verify(
            x => x.ShowWarning("Restart required", It.Is<string>(m => m.Contains("Restart Windows"))),
            Times.Once);
    }

    [Fact]
    public async Task Uninstall_CancelledExit_ReportsCancelled()
    {
        var app = Probe("cmd.exe /c exit 1602");

        await _viewModel.UninstallCommand.ExecuteAsync(app);

        _infoBarServiceMock.Verify(
            x => x.ShowInfo("Uninstall cancelled", It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task Uninstall_EntryStillListed_ReportsStillListed()
    {
        var app = Probe("cmd.exe /c exit 0");
        // The uninstaller exits 0 but the registry entry survives.
        _appsServiceMock.Setup(x => x.GetInstalledApps()).Returns([app]);

        await _viewModel.UninstallCommand.ExecuteAsync(app);

        _infoBarServiceMock.Verify(
            x => x.ShowInfo("Uninstaller finished", It.Is<string>(m => m.Contains("still listed"))),
            Times.Once);
        _infoBarServiceMock.Verify(x => x.ShowSuccess(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        Assert.Contains("still listed", _viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Uninstall_ConfirmationNamesProgramArgumentsAndRegistryKey()
    {
        var app = Probe("cmd.exe /c exit 0");

        await _viewModel.UninstallCommand.ExecuteAsync(app);

        _dialogServiceMock.Verify(
            x => x.ConfirmAsync(
                "Uninstall",
                It.Is<string>(m =>
                    m.Contains(ProbeRegistryPath, StringComparison.Ordinal)
                    && m.Contains("Program:", StringComparison.Ordinal)
                    && m.Contains("Arguments: /c exit 0", StringComparison.Ordinal)),
                "Uninstall",
                It.IsAny<string>()),
            Times.Once);
    }

    private static InstalledApp Probe(string uninstallString) => new(
        "Probe Program",
        "1.0",
        "Vain Tools Test",
        string.Empty,
        uninstallString,
        string.Empty,
        ProbeRegistryPath);

    private void AssertNoError() => _infoBarServiceMock.Verify(
        x => x.ShowError(It.IsAny<string>(), It.IsAny<string>()),
        Times.Never);
}
