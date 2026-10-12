using System.ServiceProcess;
using Microsoft.Win32;

namespace AkariTools.Framework.Services;

public sealed class ServiceControl : IServiceControl
{
    private const string ServicesKeyPath = @"SYSTEM\CurrentControlSet\Services";

    private static int? ToStartValue(ServiceStartMode mode) => mode switch
    {
        ServiceStartMode.Boot => 0,
        ServiceStartMode.System => 1,
        ServiceStartMode.Automatic => 2,
        ServiceStartMode.Manual => 3,
        ServiceStartMode.Disabled => 4,
        _ => null,
    };

    private static ServiceStartMode? ToStartMode(int value) => value switch
    {
        0 => ServiceStartMode.Boot,
        1 => ServiceStartMode.System,
        2 => ServiceStartMode.Automatic,
        3 => ServiceStartMode.Manual,
        4 => ServiceStartMode.Disabled,
        _ => null,
    };

    public ServiceStartMode? GetStartMode(string name)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey($@"{ServicesKeyPath}\{name}");
            return key?.GetValue("Start") is int start ? ToStartMode(start) : null;
        }
        catch
        {
            return null;
        }
    }

    public bool SetStartMode(string name, ServiceStartMode mode)
    {
        try
        {
            var start = ToStartValue(mode);
            if (start is null)
                return false;
            using var key = Registry.LocalMachine.OpenSubKey($@"{ServicesKeyPath}\{name}", writable: true);
            if (key is null)
                return false;
            key.SetValue("Start", start.Value, RegistryValueKind.DWord);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool? IsRunning(string name)
    {
        try
        {
            using var controller = new ServiceController(name);
            return controller.Status == ServiceControllerStatus.Running;
        }
        catch
        {
            return null;
        }
    }

    public bool StartService(string name)
    {
        try
        {
            using var controller = new ServiceController(name);
            if (controller.Status == ServiceControllerStatus.Running)
                return true;
            controller.Start();
            // Wait for the transition: returning true must mean running, so a
            // service that refuses to start reports failure (Review Focus #4).
            controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool StopService(string name)
    {
        try
        {
            using var controller = new ServiceController(name);
            if (controller.Status == ServiceControllerStatus.Stopped)
                return true;
            controller.Stop();
            // Wait for the transition: a service that hangs or refuses to
            // stop reports failure instead of hanging the caller forever.
            controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
            return true;
        }
        catch
        {
            return false;
        }
    }
}
