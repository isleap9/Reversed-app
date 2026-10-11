using AkariTools.Framework.Models;
using AkariTools.Framework.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace AkariTools.App.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly IHardwareService _hardwareService;
    private readonly IThemeService _themeService;

    [ObservableProperty]
    private HardwareInfo _hardware = new();

    [ObservableProperty]
    private AppTheme _currentTheme;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccentBrush))]
    private Color? _accentColor;

    /// <summary>
    /// Bindable brush derived from <see cref="AccentColor"/>. XAML cannot bind a
    /// <see cref="Color"/> struct directly to Foreground, so cards bind to this instead.
    /// Null (follow system) falls back to the system accent resource, then DodgerBlue.
    /// </summary>
    public Brush AccentBrush
    {
        get
        {
            if (AccentColor is { } c)
                return new SolidColorBrush(c);
            if (Microsoft.UI.Xaml.Application.Current?.Resources.TryGetValue("SystemAccentColor", out var sys) == true
                && sys is Color systemAccent)
                return new SolidColorBrush(systemAccent);
            return new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);
        }
    }

    public HomeViewModel(IHardwareService hardwareService, IThemeService themeService)
    {
        _hardwareService = hardwareService;
        _themeService = themeService;
        CurrentTheme = _themeService.CurrentTheme;
        AccentColor = _themeService.GetAccentColor();
        _themeService.ThemeChanged += (_, theme) => CurrentTheme = theme;
        _themeService.AccentColorChanged += (_, color) => AccentColor = color;
    }

    [RelayCommand]
    public async Task LoadHardwareAsync()
    {
        Hardware = await _hardwareService.LoadHardwareInfoAsync();
    }

    [RelayCommand]
    public void SetTheme(AppTheme theme)
    {
        _themeService.SetTheme(theme);
    }

    [RelayCommand]
    public void SetAccentColor(Color? color)
    {
        _themeService.SetAccentColor(color);
        AccentColor = color;
    }
}
