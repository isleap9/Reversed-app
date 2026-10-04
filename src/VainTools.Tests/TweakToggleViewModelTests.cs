using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32;
using Moq;
using VainTools.App.Models;
using VainTools.App.Services;
using VainTools.App.ViewModels;
using Xunit;

namespace VainTools.Tests;

public sealed class TweakToggleViewModelTests
{
    private readonly Mock<IRegistryTweakService> _registryMock = new();

    private static RegistryTweak Tweak => new()
    {
        Id = "test.tweak",
        Name = "Test tweak",
        Description = "A tweak used by tests.",
        Hive = RegistryHive.CurrentUser,
        KeyPath = @"Software\VainTools\Test",
        ValueName = "TestValue",
        EnabledValue = 1,
        DisabledValue = 0,
    };

    private TweakToggleViewModel CreateViewModel(TweakState state)
    {
        _registryMock.Setup(x => x.Read(It.IsAny<RegistryTweak>())).Returns(state);
        return new TweakToggleViewModel(Tweak, _registryMock.Object, NullLogger.Instance);
    }

    [Fact]
    public void Construction_ReflectsObservedState()
    {
        Assert.True(CreateViewModel(TweakState.Enabled).IsOn);
        Assert.False(CreateViewModel(TweakState.Disabled).IsOn);
    }

    [Fact]
    public void Construction_DoesNotWriteToRegistry()
    {
        _ = CreateViewModel(TweakState.Disabled);

        _registryMock.Verify(x => x.ApplyAsync(It.IsAny<RegistryTweak>()), Times.Never);
        _registryMock.Verify(x => x.RevertAsync(It.IsAny<RegistryTweak>()), Times.Never);
    }

    [Fact]
    public void BindingPushingSameValue_DoesNotWrite()
    {
        // A TwoWay binding pushes the control's initial state back into IsOn after the
        // page renders. That echo must not be mistaken for a user action, or opening a
        // page rewrites the registry.
        var vm = CreateViewModel(TweakState.Enabled);

        vm.IsOn = true;

        _registryMock.Verify(x => x.ApplyAsync(It.IsAny<RegistryTweak>()), Times.Never);
        _registryMock.Verify(x => x.RevertAsync(It.IsAny<RegistryTweak>()), Times.Never);
    }

    [Fact]
    public void UserTurningOff_WritesRevert()
    {
        var vm = CreateViewModel(TweakState.Enabled);

        vm.IsOn = false;

        _registryMock.Verify(x => x.RevertAsync(It.IsAny<RegistryTweak>()), Times.Once);
    }

    [Fact]
    public void UserTurningOn_WritesApply()
    {
        var vm = CreateViewModel(TweakState.Disabled);

        vm.IsOn = true;

        _registryMock.Verify(x => x.ApplyAsync(It.IsAny<RegistryTweak>()), Times.Once);
    }

    [Fact]
    public void Refresh_DoesNotWrite()
    {
        var vm = CreateViewModel(TweakState.Enabled);

        vm.Refresh();

        _registryMock.Verify(x => x.ApplyAsync(It.IsAny<RegistryTweak>()), Times.Never);
        _registryMock.Verify(x => x.RevertAsync(It.IsAny<RegistryTweak>()), Times.Never);
    }
}
