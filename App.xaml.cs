using Microsoft.UI.Xaml;

namespace VainTools;

public partial class App : Application
{
    private Window? _mainWindow;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _mainWindow = new MainWindow();
        _mainWindow.Activate();
    }
}