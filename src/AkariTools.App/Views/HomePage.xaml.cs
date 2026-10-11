using AkariTools.App.ViewModels;
using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AkariTools.App.Views;

public sealed partial class HomePage : Page
{
    public HomeViewModel? ViewModel => DataContext as HomeViewModel;

    public HomePage(IHardwareService hardwareService, IThemeService themeService)
    {
        this.InitializeComponent();
        DataContext = new HomeViewModel(hardwareService, themeService);
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
        {
            await ViewModel.LoadHardwareAsync();
        }
    }

    private void ThemeLight_Click(object sender, RoutedEventArgs e) => ViewModel?.SetTheme(AppTheme.Light);
    private void ThemeDark_Click(object sender, RoutedEventArgs e) => ViewModel?.SetTheme(AppTheme.Dark);
    private void ThemeFullDark_Click(object sender, RoutedEventArgs e) => ViewModel?.SetTheme(AppTheme.FullDark);
    private void ThemeSystem_Click(object sender, RoutedEventArgs e) => ViewModel?.SetTheme(AppTheme.System);

    private void AccentColor_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Open color picker dialog, then call ViewModel.SetAccentColor (null = follow system).
    }
}
