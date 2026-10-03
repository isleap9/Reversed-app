using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VainTools.Pages;
using VainTools.GpuGovernor.Views;

namespace VainTools;

public sealed partial class MainWindow : Window
{
    private readonly Dictionary<string, Page> _pages = new();
    public NavigationViewItem? SelectedNavItem { get; set; }

    public MainWindow()
    {
        this.InitializeComponent();
        this.Title = "Vain Tools";

        _pages["dashboard"] = new DashboardPage();
        _pages["gpu"] = new GpuGovernorView();
        _pages["profiles"] = new ProfilesPage();
        _pages["system"] = new SystemTweaksPage();
        _pages["screenshots"] = new ScreenshotsPage();
        _pages["taskbar"] = new TaskbarPage();

        NavView.SelectionChanged += NavView_SelectionChanged;
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            if (_pages.TryGetValue(tag, out var page))
            {
                ContentFrame.Navigate(page.GetType());
            }
        }
    }
}