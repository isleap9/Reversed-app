using AkariTools.Framework.Models;
using Microsoft.Win32;
using Windows.UI;

namespace AkariTools.Framework.Services;

public class ThemeService : IThemeService
{
    public const string DefaultRegistryPath = @"Software\AkariToolbox";
    private const string ThemeValueName = "Theme";
    private const string AccentValueName = "AccentColor";

    private readonly string _registryPath;

    public ThemeService() : this(DefaultRegistryPath) { }

    public ThemeService(string registryPath)
    {
        _registryPath = registryPath;
        CurrentTheme = ReadTheme();
    }

    public AppTheme CurrentTheme { get; private set; }

    public event EventHandler<AppTheme>? ThemeChanged;
    public event EventHandler<Color?>? AccentColorChanged;

    public void SetTheme(AppTheme theme)
    {
        CurrentTheme = theme;
        WriteValue(ThemeValueName, (int)theme);
        ThemeChanged?.Invoke(this, theme);
    }

    public Color? GetAccentColor()
    {
        if (ReadValue(AccentValueName) is not int argb)
            return null;
        return Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);
    }

    public void SetAccentColor(Color? color)
    {
        if (color is { } c)
            WriteValue(AccentValueName, (c.A << 24) | (c.R << 16) | (c.G << 8) | c.B);
        else
            DeleteValue(AccentValueName);
        AccentColorChanged?.Invoke(this, color);
    }

    private AppTheme ReadTheme() =>
        ReadValue(ThemeValueName) is int value && Enum.IsDefined(typeof(AppTheme), value)
            ? (AppTheme)value
            : AppTheme.System;

    // Registry failures are non-fatal: settings fall back to defaults and
    // the in-memory state still drives the UI for this session.
    private object? ReadValue(string name)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(_registryPath);
            return key?.GetValue(name);
        }
        catch { return null; }
    }

    private void WriteValue(string name, int value)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(_registryPath);
            key.SetValue(name, value, RegistryValueKind.DWord);
        }
        catch { }
    }

    private void DeleteValue(string name)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(_registryPath, writable: true);
            key?.DeleteValue(name, throwOnMissingValue: false);
        }
        catch { }
    }
}
