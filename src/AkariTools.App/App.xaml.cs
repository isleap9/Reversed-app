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
        this.InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<IHardwareService, HardwareService>();
            })
            .Build();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _instanceMutex = new System.Threading.Mutex(true, @"Local\AkariToolboxInstance", out bool createdNew);
        if (!createdNew)
        {
            // TODO: Activate existing instance
            Exit();
            return;
        }

        var hardwareService = Host.Services.GetRequiredService<IHardwareService>();
        var themeService = Host.Services.GetRequiredService<IThemeService>();

        MainWindow = new MainWindow(hardwareService, themeService);
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
    }
}
