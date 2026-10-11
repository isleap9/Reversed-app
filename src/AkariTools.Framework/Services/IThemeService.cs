using AkariTools.Framework.Models;
using Windows.UI;

namespace AkariTools.Framework.Services;

public interface IThemeService
{
    AppTheme CurrentTheme { get; }
    event EventHandler<AppTheme>? ThemeChanged;
    void SetTheme(AppTheme theme);

    /// <summary>The user's accent override, or null to follow the system accent.</summary>
    Color? GetAccentColor();
    void SetAccentColor(Color? color);
    event EventHandler<Color?>? AccentColorChanged;
}
