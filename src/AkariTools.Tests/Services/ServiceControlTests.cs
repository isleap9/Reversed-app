using AkariTools.Framework.Services;
using Xunit;

namespace AkariTools.Tests.Services;

public sealed class ServiceControlTests
{
    private const string BogusService = "AkariBogus123NoSuchService";
    private readonly ServiceControl _control = new();

    [Fact]
    public void BogusService_GetStartModeReturnsNull()
    {
        Assert.Null(_control.GetStartMode(BogusService));
    }

    [Fact]
    public void BogusService_SetStartModeReturnsFalse()
    {
        Assert.False(_control.SetStartMode(BogusService, System.ServiceProcess.ServiceStartMode.Automatic));
    }

    [Fact]
    public void BogusService_IsRunningReturnsNull()
    {
        Assert.Null(_control.IsRunning(BogusService));
    }

    [Fact]
    public void BogusService_StartStopReturnFalse()
    {
        Assert.False(_control.StartService(BogusService));
        Assert.False(_control.StopService(BogusService));
    }
}
