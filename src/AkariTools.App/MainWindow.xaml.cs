using AkariTools.App.Views;
using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AkariTools.App;

public sealed partial class MainWindow : Window
{
    private readonly IThemeService _themeService;

    public MainWindow(IHardwareService hardwareService, IThemeService themeService)
    {
        this.InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        _themeService = themeService;
        ApplyTheme(_themeService.CurrentTheme);
        _themeService.ThemeChanged += OnThemeChanged;
        Closed += (_, _) => _themeService.ThemeChanged -= OnThemeChanged;

        ContentGrid.Children.Clear();
        ContentGrid.Children.Add(new HomePage(hardwareService, themeService));
        if (NavView.MenuItems.Count > 0 && NavView.MenuItems[0] is NavigationViewItem first)
            NavView.SelectedItem = first;
    }

    private void OnThemeChanged(object? sender, AppTheme theme) =>
        DispatcherQueue.TryEnqueue(() => ApplyTheme(theme));

    private void ApplyTheme(AppTheme theme)
    {
        if (Content is FrameworkElement root)
        {
            root.RequestedTheme = theme switch
            {
                AppTheme.Light => ElementTheme.Light,
                AppTheme.Dark => ElementTheme.Dark,
                AppTheme.FullDark => ElementTheme.Dark,
                _ => ElementTheme.Default,
            };
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        // TODO: Page navigation in future tasks.
    }
}
