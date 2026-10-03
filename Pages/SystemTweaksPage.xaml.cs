using Microsoft.UI.Xaml.Controls;
using VainTools.Modules;

namespace VainTools.Pages;

public sealed partial class SystemTweaksPage : Page
{
    public SystemTweaksPage()
    {
        this.InitializeComponent();
        DarkThemeToggle.IsOn = SystemTweaksModule.GetTheme() == SystemTweaksModule.ThemeMode.Dark;
    }

    private void DarkThemeToggle_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        SystemTweaksModule.SetTheme(
            DarkThemeToggle.IsOn
                ? SystemTweaksModule.ThemeMode.Dark
                : SystemTweaksModule.ThemeMode.Light);
    }

    private void NotificationsToggle_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (NotificationsToggle.IsOn)
            SystemTweaksModule.DisableNotifications();
        else
            SystemTweaksModule.EnableNotifications();
    }
}