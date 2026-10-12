using AkariTools.App.Helpers;
using AkariTools.Framework.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

namespace AkariTools.App;

public partial class App : Application
{
    private System.Threading.Mutex? _instanceMutex;

    public Window? MainWindow { get; private set; }

    public IHost Host { get; }

    public App()
    {
        LaunchLog.Write("App ctor start");
        this.InitializeComponent();
        this.UnhandledException += (_, e) =>
        {
            LaunchLog.WriteException("UnhandledException", e.Exception);
        };
        LaunchLog.Write("App InitializeComponent done");

        try
        {
            Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IThemeService, ThemeService>();
                    services.AddSingleton<IHardwareService, HardwareService>();
                    services.AddSingleton<IRegistrySettingService, RegistrySettingService>();
                    services.AddSingleton<IServiceControl, ServiceControl>();
                    services.AddSingleton<ICliRunner, CliRunner>();
                    services.AddSingleton<IDateFormatService, DateFormatService>();
                    services.AddSingleton<ITimeZoneService, TimeZoneService>();
                })
                .Build();
            LaunchLog.Write("Host built");
        }
        catch (Exception ex)
        {
            LaunchLog.WriteException("Host.Build failed", ex);
            throw;
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        LaunchLog.Write("OnLaunched start");
        _instanceMutex = new System.Threading.Mutex(true, @"Local\AkariToolboxInstance", out bool createdNew);
        LaunchLog.Write($"Mutex createdNew={createdNew}");
        if (!createdNew)
        {
            // TODO: Activate existing instance
            LaunchLog.Write("Second instance, exiting");
            Exit();
            return;
        }

        try
        {
            var hardwareService = Host.Services.GetRequiredService<IHardwareService>();
            var themeService = Host.Services.GetRequiredService<IThemeService>();
            var registrySettingService = Host.Services.GetRequiredService<IRegistrySettingService>();
            var serviceControl = Host.Services.GetRequiredService<IServiceControl>();
            var dateFormatService = Host.Services.GetRequiredService<IDateFormatService>();
            var timeZoneService = Host.Services.GetRequiredService<ITimeZoneService>();
            var cliRunner = Host.Services.GetRequiredService<ICliRunner>();
            LaunchLog.Write("Services resolved");

            MainWindow = new MainWindow(hardwareService, themeService, registrySettingService, serviceControl, dateFormatService, timeZoneService, cliRunner);
            LaunchLog.Write("MainWindow created");
            MainWindow.Closed += (_, _) =>
            {
                try { _instanceMutex?.ReleaseMutex(); }
                catch { }
                finally
                {
                    _instanceMutex?.Dispose();
                    _instanceMutex = null;
                }
                Host.Dispose();
            };
            MainWindow.Activate();
            LaunchLog.Write("MainWindow activated");
        }
        catch (Exception ex)
        {
            LaunchLog.WriteException("OnLaunched failed", ex);
            throw;
        }
    }
}
