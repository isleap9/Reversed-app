using AkariTools.App.Views;
using AkariTools.Framework.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AkariTools.App;

public sealed partial class MainWindow : Window
{
    public MainWindow(IHardwareService hardwareService, IThemeService themeService)
    {
        this.InitializeComponent();
        Title = "Akari Toolbox";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        ContentGrid.Children.Clear();
        ContentGrid.Children.Add(new HomePage(hardwareService, themeService));
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        // TODO: Page navigation in future tasks.
    }
}
