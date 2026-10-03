using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VainTools.App.Services;
using VainTools.App.ViewModels;
using VainTools.Framework.Services;
using Xunit;

namespace VainTools.Tests;

public sealed class AffinityViewModelTests
{
    private readonly Mock<IAffinityService> _affinityServiceMock = new();
    private readonly Mock<IRegistryTweakService> _registryTweakServiceMock = new();
    private readonly Mock<IDialogService> _dialogServiceMock = new();
    private readonly Mock<IInfoBarService> _infoBarServiceMock = new();
    private readonly AffinityViewModel _viewModel;

    public AffinityViewModelTests()
    {
        _viewModel = new AffinityViewModel(
            _affinityServiceMock.Object,
            _registryTweakServiceMock.Object,
            _dialogServiceMock.Object,
            _infoBarServiceMock.Object,
            NullLogger<AffinityViewModel>.Instance);
    }

    [Fact]
    public void InitialState_IsCorrect()
    {
        Assert.False(_viewModel.IsElevated);
        Assert.False(_viewModel.IsLoading);
        Assert.Equal(string.Empty, _viewModel.ErrorMessage);
        Assert.NotNull(_viewModel.Processes);
        Assert.Empty(_viewModel.Processes);
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
    public void SelectProcessCommand_SetsSelectedProcess()
    {
        var process = new ProcessViewModel(1, "test.exe", 4);
        _viewModel.SelectProcessCommand.Execute(process);

        Assert.NotNull(_viewModel.SelectedProcess);
        Assert.Equal("test.exe", _viewModel.SelectedProcess.Name);
    }

    [Fact]
    public void ApplyAffinityCommand_CanExecute_WhenProcessSelected()
    {
        var process = new ProcessViewModel(1, "test.exe", 4);
        _viewModel.SelectProcessCommand.Execute(process);

        Assert.True(_viewModel.ApplyAffinityCommand.CanExecute(null));
    }

    [Fact]
    public void ApplyAffinityCommand_CannotExecute_WhenNoProcessSelected()
    {
        Assert.False(_viewModel.ApplyAffinityCommand.CanExecute(null));
    }
}
