using System.ServiceProcess;

namespace AkariTools.Framework.Services;

public interface IServiceControl
{
    ServiceStartMode? GetStartMode(string name);
    bool SetStartMode(string name, ServiceStartMode mode);
    bool? IsRunning(string name);
    bool StartService(string name);
    bool StopService(string name);
}
