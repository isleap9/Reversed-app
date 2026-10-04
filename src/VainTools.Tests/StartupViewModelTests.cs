using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VainTools.App.Services;
using VainTools.App.ViewModels;
using VainTools.Framework.Services;
using Xunit;

namespace VainTools.Tests;

public sealed class StartupViewModelTests
{
    private readonly Mock<IStartupService> _startupServiceMock = new();
    private readonly Mock<IRegistryTweakService> _registryTweakServiceMock = new();
    private readonly Mock<IDialogService> _dialogServiceMock = new();
    private readonly Mock<IInfoBarService> _infoBarServiceMock = new();
    private readonly StartupViewModel _viewModel;

    public StartupViewModelTests()
    {
        _viewModel = new StartupViewModel(
            _startupServiceMock.Object,
            _registryTweakServiceMock.Object,
            _dialogServiceMock.Object,
            _infoBarServiceMock.Object,
            NullLogger<StartupViewModel>.Instance);
    }

    [Fact]
    public void InitialState_IsCorrect()
    {
        Assert.False(_viewModel.IsElevated);
        Assert.False(_viewModel.IsLoading);
        Assert.Equal(string.Empty, _viewModel.ErrorMessage);
        Assert.NotNull(_viewModel.RunKeyEntries);
        Assert.Empty(_viewModel.RunKeyEntries);
        Assert.NotNull(_viewModel.ScheduledTasks);
        Assert.Empty(_viewModel.ScheduledTasks);
    }

    [Fact]
    public void IsElevated_Set_RaisesPropertyChanged()
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _viewModel.IsElevated = true;

        Assert.True(_viewModel.IsElevated);
        Assert.Contains(nameof(_viewModel.IsElevated), changed);
    }

    [Fact]
    public void IsLoading_Set_RaisesPropertyChanged()
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _viewModel.IsLoading = true;

        Assert.True(_viewModel.IsLoading);
        Assert.Contains(nameof(_viewModel.IsLoading), changed);
    }

    [Fact]
    public void ErrorMessage_Set_RaisesPropertyChanged()
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _viewModel.ErrorMessage = "Test error";

        Assert.Equal("Test error", _viewModel.ErrorMessage);
        Assert.Contains(nameof(_viewModel.ErrorMessage), changed);
    }

    [Fact]
    public void StatusMessage_Set_RaisesPropertyChanged()
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _viewModel.StatusMessage = "Test status";

        Assert.Equal("Test status", _viewModel.StatusMessage);
        Assert.Contains(nameof(_viewModel.StatusMessage), changed);
    }

    [Fact]
    public async Task RefreshAsync_PopulatesRunKeysAndScheduledTasks()
    {
        _startupServiceMock
            .Setup(x => x.GetRunKeyEntries())
            .Returns([new StartupEntry("OneDrive", @"C:\OneDrive.exe", "HKCU", true)]);
        _startupServiceMock
            .Setup(x => x.GetScheduledTasksAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new StartupEntry("EdgeUpdate", @"\Microsoft\Edge\", "Scheduled Task", true)]);

        await _viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Single(_viewModel.RunKeyEntries);
        Assert.Single(_viewModel.ScheduledTasks);
        Assert.False(_viewModel.IsLoading);
        Assert.Contains("1 Run key entries", _viewModel.StatusMessage);
    }

    [Fact]
    public async Task RefreshAsync_WhenServiceFails_SetsErrorMessage()
    {
        _startupServiceMock
            .Setup(x => x.GetRunKeyEntries())
            .Throws(new InvalidOperationException("Registry access failed"));

        await _viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Contains("Registry access failed", _viewModel.ErrorMessage);
        Assert.Contains("Could not load entries", _viewModel.StatusMessage);
        Assert.False(_viewModel.IsLoading);
    }

    [Fact]
    public void ToggleRunKeyEntryCommand_CanExecute_WhenEntrySelected()
    {
        var entry = new StartupEntry("test", "cmd", "HKCU", true);

        Assert.True(_viewModel.ToggleRunKeyEntryCommand.CanExecute(entry));
        Assert.False(_viewModel.ToggleRunKeyEntryCommand.CanExecute(null));
    }

    [Fact]
    public void ToggleScheduledTaskCommand_CanExecute_WhenEntrySelected()
    {
        var entry = new StartupEntry("test", "cmd", "Scheduled Task", true);

        Assert.True(_viewModel.ToggleScheduledTaskCommand.CanExecute(entry));
        Assert.False(_viewModel.ToggleScheduledTaskCommand.CanExecute(null));
    }

    [Fact]
    public async Task ToggleRunKeyEntryAsync_FlipsStateAndReloads()
    {
        var entry = new StartupEntry("OneDrive", @"C:\OneDrive.exe", "HKCU", true);

        await _viewModel.ToggleRunKeyEntryCommand.ExecuteAsync(entry);

        _startupServiceMock.Verify(x => x.ToggleRunKeyEntry(entry, false), Times.Once);
        Assert.Contains("Disabled OneDrive", _viewModel.StatusMessage);
    }

    [Fact]
    public async Task ToggleScheduledTaskAsync_FlipsStateAndReloads()
    {
        var entry = new StartupEntry("EdgeUpdate", @"\Microsoft\Edge\", "Scheduled Task", false);

        await _viewModel.ToggleScheduledTaskCommand.ExecuteAsync(entry);

        _startupServiceMock.Verify(
            x => x.ToggleScheduledTaskAsync(entry, true, It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Contains("Enabled EdgeUpdate", _viewModel.StatusMessage);
    }

    [Fact]
    public void IsUserToggle_IgnoresProgrammaticBindingEcho()
    {
        // A toggle bound to a record also fires Toggled when the binding pushes the model
        // value in. Treating that echo as a user action makes Refresh and Toggle recurse
        // into each other without end — which crashed the app and mass-disabled tasks.
        var enabled = new StartupEntry("A", "cmd", "HKCU", true);
        var disabled = new StartupEntry("B", "cmd", "HKCU", false);

        Assert.False(StartupViewModel.IsUserToggle(enabled, newIsOn: true));
        Assert.False(StartupViewModel.IsUserToggle(disabled, newIsOn: false));

        Assert.True(StartupViewModel.IsUserToggle(enabled, newIsOn: false));
        Assert.True(StartupViewModel.IsUserToggle(disabled, newIsOn: true));
    }

    [Fact]
    public async Task ToggleScheduledTaskAsync_WhenServiceFails_SetsErrorMessage()
    {
        _startupServiceMock
            .Setup(x => x.ToggleScheduledTaskAsync(It.IsAny<StartupEntry>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Access is denied"));

        var entry = new StartupEntry("EdgeUpdate", @"\Microsoft\Edge\", "Scheduled Task", true);
        await _viewModel.ToggleScheduledTaskCommand.ExecuteAsync(entry);

        Assert.Contains("Access is denied", _viewModel.ErrorMessage);
        Assert.Contains("Could not toggle task", _viewModel.StatusMessage);
    }
}
