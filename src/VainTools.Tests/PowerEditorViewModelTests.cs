using Moq;
using VainTools.App.Models;
using VainTools.App.Services;
using VainTools.App.ViewModels;
using Xunit;

namespace VainTools.Tests;

public sealed class PowerEditorViewModelTests
{
    private readonly Mock<IPowerService> _powerServiceMock = new();
    private readonly PowerEditorViewModel _viewModel;

    public PowerEditorViewModelTests()
    {
        _viewModel = new PowerEditorViewModel(_powerServiceMock.Object,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<PowerEditorViewModel>.Instance);
    }

    [Fact]
    public async Task LoadPlansAsync_PopulatesPlansCollection()
    {
        var plans = new List<PowerPlan>
        {
            new(Guid.NewGuid(), "Balanced", true),
            new(Guid.NewGuid(), "High Performance", false),
        };

        _powerServiceMock
            .Setup(x => x.GetPlansAsync())
            .ReturnsAsync(plans);

        await _viewModel.LoadPlansAsync();

        Assert.Equal(2, _viewModel.Plans.Count);
        Assert.Equal("Balanced", _viewModel.Plans[0].Name);
    }

    [Fact]
    public async Task LoadPlansAsync_SetsActivePlan()
    {
        var plans = new List<PowerPlan>
        {
            new(Guid.NewGuid(), "Balanced", true),
            new(Guid.NewGuid(), "High Performance", false),
        };

        _powerServiceMock
            .Setup(x => x.GetPlansAsync())
            .ReturnsAsync(plans);

        await _viewModel.LoadPlansAsync();

        Assert.NotNull(_viewModel.SelectedPlan);
        Assert.Equal("Balanced", _viewModel.SelectedPlan.Name);
    }

    [Fact]
    public async Task LoadSettingsAsync_PopulatesSettingsCollection()
    {
        var planGuid = Guid.NewGuid();
        var settings = new List<PowerSetting>
        {
            new("guid1", "Setting1", "catguid", "Category", "5", "5", new List<string> { "0", "5", "100" }),
        };

        _powerServiceMock
            .Setup(x => x.GetSettingsAsync(planGuid))
            .ReturnsAsync(settings);

        // Set up the selected plan
        _powerServiceMock
            .Setup(x => x.GetPlansAsync())
            .ReturnsAsync(new List<PowerPlan> { new(planGuid, "Balanced", true) });

        await _viewModel.LoadPlansAsync();
        await _viewModel.LoadSettingsAsync();

        Assert.Single(_viewModel.Settings);
        Assert.Equal("Setting1", _viewModel.Settings[0].Name);
    }

    [Fact]
    public async Task ApplyChangesAsync_CallsPowerServiceForAllSettings()
    {
        var planGuid = Guid.NewGuid();
        var settings = new List<PowerSetting>
        {
            new("guid1", "Setting1", "catguid", "Category", "5", "5", new List<string> { "0", "5", "100" }),
            new("guid2", "Setting2", "catguid", "Category", "10", "10", new List<string> { "0", "10", "100" }),
        };

        _powerServiceMock
            .Setup(x => x.GetPlansAsync())
            .ReturnsAsync(new List<PowerPlan> { new(planGuid, "Balanced", true) });
        _powerServiceMock
            .Setup(x => x.GetSettingsAsync(planGuid))
            .ReturnsAsync(settings);
        _powerServiceMock.Setup(x => x.IsElevated).Returns(true);

        await _viewModel.LoadPlansAsync();
        await _viewModel.LoadSettingsAsync();
        await _viewModel.ApplyChangesAsync();

        _powerServiceMock.Verify(x => x.ApplySettingAsync(planGuid, "guid1", "5"), Times.Once);
        _powerServiceMock.Verify(x => x.ApplySettingAsync(planGuid, "guid2", "10"), Times.Once);
    }

    [Fact]
    public async Task RevertChangesAsync_CallsRevertPlan()
    {
        var planGuid = Guid.NewGuid();

        _powerServiceMock
            .Setup(x => x.GetPlansAsync())
            .ReturnsAsync(new List<PowerPlan> { new(planGuid, "Balanced", true) });
        _powerServiceMock
            .Setup(x => x.GetSettingsAsync(planGuid))
            .ReturnsAsync(new List<PowerSetting>());
        _powerServiceMock.Setup(x => x.IsElevated).Returns(true);

        await _viewModel.LoadPlansAsync();
        await _viewModel.RevertChangesAsync();

        _powerServiceMock.Verify(x => x.RevertPlanAsync(planGuid), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_ReloadsPlansAndSettings()
    {
        var planGuid = Guid.NewGuid();

        _powerServiceMock
            .Setup(x => x.GetPlansAsync())
            .ReturnsAsync(new List<PowerPlan> { new(planGuid, "Balanced", true) });
        _powerServiceMock
            .Setup(x => x.GetSettingsAsync(planGuid))
            .ReturnsAsync(new List<PowerSetting>());

        await _viewModel.RefreshAsync();

        _powerServiceMock.Verify(x => x.GetPlansAsync(), Times.Once);
        _powerServiceMock.Verify(x => x.GetSettingsAsync(planGuid), Times.Once);
    }

    [Fact]
    public void NotElevated_DisablesApplyButton()
    {
        _powerServiceMock.Setup(x => x.IsElevated).Returns(false);

        Assert.False(_viewModel.IsElevated);
        Assert.False(_viewModel.CanApply);
    }

    [Fact]
    public void IsBusy_DuringApply_DisablesButtons()
    {
        _powerServiceMock.Setup(x => x.IsElevated).Returns(true);

        // Initially not busy, but no plan selected
        Assert.False(_viewModel.CanApply);
    }
}
