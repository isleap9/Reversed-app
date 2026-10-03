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
    public void ToggleRunKeyEntryCommand_CanExecute_WhenEntrySelected()
    {
        var entry = new StartupEntryViewModel("test", "cmd", "HKCU", true);
        Assert.True(_viewModel.ToggleRunKeyEntryCommand.CanExecute(entry));
    }

    [Fact]
    public void ToggleScheduledTaskCommand_CanExecute_WhenEntrySelected()
    {
        var entry = new StartupEntryViewModel("test", "cmd", "Task", true);
        Assert.True(_viewModel.ToggleScheduledTaskCommand.CanExecute(entry));
    }
}
