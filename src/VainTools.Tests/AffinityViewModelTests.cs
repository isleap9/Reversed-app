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
        _affinityServiceMock.Setup(x => x.GetCpuCount()).Returns(4);
        _affinityServiceMock.Setup(x => x.GetAffinityMask(It.IsAny<int>())).Returns(0b1111UL);

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
    public void Refresh_PopulatesProcesses()
    {
        _affinityServiceMock
            .Setup(x => x.GetProcesses())
            .Returns([new ProcessInfo(1, "test.exe", 4), new ProcessInfo(2, "other.exe", 4)]);

        _viewModel.RefreshCommand.Execute(null);

        Assert.Equal(2, _viewModel.Processes.Count);
        Assert.Equal(1, _viewModel.Processes[0].Id);
        Assert.False(_viewModel.IsLoading);
        Assert.Contains("2 processes", _viewModel.StatusMessage);
    }

    [Fact]
    public void Refresh_WhenServiceFails_SetsErrorMessage()
    {
        _affinityServiceMock
            .Setup(x => x.GetProcesses())
            .Throws(new InvalidOperationException("P/Invoke failed"));

        _viewModel.RefreshCommand.Execute(null);

        Assert.Contains("P/Invoke failed", _viewModel.ErrorMessage);
        Assert.Contains("Could not get processes", _viewModel.StatusMessage);
        Assert.False(_viewModel.IsLoading);
    }

    [Fact]
    public void SelectProcessCommand_SetsSelectedProcessAndBuildsCpus()
    {
        var process = new ProcessInfo(1, "test.exe", 4);

        _viewModel.SelectProcessCommand.Execute(process);

        Assert.NotNull(_viewModel.SelectedProcess);
        Assert.Equal("test.exe", _viewModel.SelectedProcess.Name);
        Assert.Equal(4, _viewModel.Cpus.Count);
        Assert.All(_viewModel.Cpus, cpu => Assert.True(cpu.IsEnabled));
    }

    [Fact]
    public void SelectProcessCommand_ReflectsMaskBitsInCheckboxes()
    {
        _affinityServiceMock.Setup(x => x.GetAffinityMask(42)).Returns(0b0101UL);

        _viewModel.SelectProcessCommand.Execute(new ProcessInfo(42, "half.exe", 2));

        Assert.Equal(4, _viewModel.Cpus.Count);
        Assert.True(_viewModel.Cpus[0].IsEnabled);
        Assert.False(_viewModel.Cpus[1].IsEnabled);
        Assert.True(_viewModel.Cpus[2].IsEnabled);
        Assert.False(_viewModel.Cpus[3].IsEnabled);
    }

    [Fact]
    public void ApplyAffinityCommand_CanExecute_WhenProcessSelected()
    {
        Assert.False(_viewModel.ApplyAffinityCommand.CanExecute(null));

        _viewModel.SelectProcessCommand.Execute(new ProcessInfo(1, "test.exe", 4));

        Assert.True(_viewModel.ApplyAffinityCommand.CanExecute(null));
    }

    [Fact]
    public void ApplyAffinityCommand_WritesMaskFromCheckboxes()
    {
        _viewModel.SelectProcessCommand.Execute(new ProcessInfo(7, "test.exe", 4));
        _viewModel.Cpus[1].IsEnabled = false;
        _viewModel.Cpus[3].IsEnabled = false;

        _viewModel.ApplyAffinityCommand.Execute(null);

        _affinityServiceMock.Verify(x => x.SetAffinityMask(7, 0b0101UL), Times.Once);
    }

    [Fact]
    public void ApplyAffinityCommand_WhenServiceFails_SetsErrorMessage()
    {
        _affinityServiceMock
            .Setup(x => x.SetAffinityMask(It.IsAny<int>(), It.IsAny<ulong>()))
            .Throws(new InvalidOperationException("Access denied"));

        _viewModel.SelectProcessCommand.Execute(new ProcessInfo(7, "test.exe", 4));
        _viewModel.ApplyAffinityCommand.Execute(null);

        Assert.Contains("Access denied", _viewModel.ErrorMessage);
        Assert.Contains("Could not set affinity", _viewModel.StatusMessage);
    }
}
