using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace VainTools.Modules;

public static class SystemTweaksModule
{
    // Registry constants
    private const string ThemePath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string NotificationsPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\PushNotifications";
    private const string NotificationsConsentPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userNotificationListener";

    public enum ThemeMode { Light, Dark }

    public static void SetTheme(ThemeMode mode)
    {
        using var key = Registry.CurrentUser.OpenSubKey(ThemePath, true);
        if (key != null)
        {
            key.SetValue("SystemUsesLightTheme", mode == ThemeMode.Light ? 0 : 1, RegistryValueKind.DWord);
            key.SetValue("AppsUseLightTheme", mode == ThemeMode.Light ? 0 : 1, RegistryValueKind.DWord);
        }
    }

    public static ThemeMode GetTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(ThemePath);
        if (key != null)
        {
            var val = key.GetValue("AppsUseLightTheme");
            if (val is int i && i == 0) return ThemeMode.Dark;
        }
        return ThemeMode.Light;
    }

    public static void DisableNotifications()
    {
        using var key1 = Registry.CurrentUser.OpenSubKey(NotificationsPath, true);
        key1?.SetValue("ToastEnabled", 0, RegistryValueKind.DWord);

        using var key2 = Registry.CurrentUser.OpenSubKey(NotificationsConsentPath, true);
        key2?.SetValue("Value", "Deny", RegistryValueKind.String);

        using var key3 = Registry.LocalMachine.OpenSubKey(NotificationsPath, true);
        key3?.SetValue("NoCloudApplicationNotification", 1, RegistryValueKind.DWord);
    }

    public static void EnableNotifications()
    {
        using var key1 = Registry.CurrentUser.OpenSubKey(NotificationsPath, true);
        key1?.SetValue("ToastEnabled", 1, RegistryValueKind.DWord);

        using var key2 = Registry.CurrentUser.OpenSubKey(NotificationsConsentPath, true);
        key2?.SetValue("Value", "Allow", RegistryValueKind.String);
    }

    public static void SetPowerPlan(string planGuid)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "powercfg",
            Arguments = $"/setactive {planGuid}",
            UseShellExecute = false,
            CreateNoWindow = true,
        })?.WaitForExit();
    }

    public static void ImportPowerPlan(string path)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "powercfg",
            Arguments = $"/import \"{path}\" 01010110-0110-0001-0110-100101101110",
            UseShellExecute = false,
            CreateNoWindow = true,
        })?.WaitForExit();
    }

    public static void DeleteOtherPowerPlans()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powercfg",
            Arguments = "-list",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        var proc = Process.Start(psi);
        proc?.WaitForExit();
    }
}