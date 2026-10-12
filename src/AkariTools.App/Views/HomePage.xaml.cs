using AkariTools.App.ViewModels;
using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AkariTools.App.Views;

public sealed partial class HomePage : Page
{
    private static readonly SolidColorBrush FullDarkBackground = new(Microsoft.UI.Colors.Black);

    private readonly IThemeService _themeService;

    public HomeViewModel? ViewModel => DataContext as HomeViewModel;

    public HomePage(IHardwareService hardwareService, IThemeService themeService)
    {
        this.InitializeComponent();
        _themeService = themeService;
        DataContext = new HomeViewModel(hardwareService, themeService);
        ApplyTheme(_themeService.CurrentTheme);
        _themeService.ThemeChanged += OnThemeChanged;
        Unloaded += (_, _) => _themeService.ThemeChanged -= OnThemeChanged;
    }

    private void OnThemeChanged(object? sender, AppTheme theme) =>
        DispatcherQueue.TryEnqueue(() => ApplyTheme(theme));

    private void ApplyTheme(AppTheme theme)
    {
        RequestedTheme = theme switch
        {
            AppTheme.Light => ElementTheme.Light,
            AppTheme.Dark => ElementTheme.Dark,
            AppTheme.FullDark => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };

        // Full Dark: darkest treatment — solid black behind the translucent cards.
        RootGrid.Background = theme == AppTheme.FullDark ? FullDarkBackground : null;

        // Star field: dim white stars on light theme so they stay subtle.
        StarFieldCtrl.ApplyLightDim(theme == AppTheme.Light);
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

    private async void AccentColor_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
            return;

        var picker = new ColorPicker
        {
            Color = ViewModel.AccentColor ?? Microsoft.UI.Colors.DodgerBlue,
            IsColorSliderVisible = true,
            IsColorChannelTextInputVisible = true,
            IsHexInputVisible = true,
            IsAlphaEnabled = true,
            MinWidth = 300,
        };

        var dialog = new ContentDialog
        {
            Title = "Accent Color",
            Content = picker,
            PrimaryButtonText = "Apply",
            SecondaryButtonText = "Use system",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot,
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
            ViewModel.SetAccentColor(picker.Color);
        else if (result == ContentDialogResult.Secondary)
            ViewModel.SetAccentColor(null);
    }
}
