using Microsoft.UI.Xaml;

namespace AkariTools.App;

public partial class App : Application
{
    public Window? MainWindow { get; private set; }

    public App()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var mutex = new System.Threading.Mutex(true, @"Local\AkariToolboxInstance", out bool createdNew);
        if (!createdNew)
        {
            // TODO: Activate existing instance
            Exit();
            return;
        }

        MainWindow = new MainWindow();
        MainWindow.Activate();
    }
}
