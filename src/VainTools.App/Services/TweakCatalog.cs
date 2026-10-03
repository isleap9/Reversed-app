using Microsoft.Win32;
using VainTools.App.Models;

namespace VainTools.App.Services;

/// <summary>
/// The real tweak set for the General and System pages.
///
/// Key paths and value names were recovered from <c>Vain Toolbox.exe</c>; see
/// .planning/research/VAIN-TOOLBOX-GROUND-TRUTH.md.
/// </summary>
public static class TweakCatalog
{
    // Registry paths used by these pages (recovered from the binary).
    private const string ExplorerAdvanced =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced";

    private const string ExplorerAutoplay =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers";

    private const string ThemesPersonalize =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    private const string PoliciesExplorer =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer";

    private const string PoliciesWindowsExplorer =
        @"SOFTWARE\Policies\Microsoft\Windows\Explorer";

    private const string BootAnimation =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\LogonUI\BootAnimation";

    private const string Dwm =
        @"SOFTWARE\Microsoft\Windows\Dwm";

    private const string DesktopWindowMetrics =
        @"Control Panel\Desktop\WindowMetrics";

    private const string Desktop = @"Control Panel\Desktop";

    /// <summary>Explorer / File Explorer tweaks (ExplorerPage).</summary>
    public static IReadOnlyList<RegistryTweak> Explorer { get; } =
    [
        new()
        {
            Id = "explorer.show-file-extensions",
            Name = "Show file extensions",
            Description = "Show known file type extensions in File Explorer.",
            Hive = RegistryHive.CurrentUser,
            KeyPath = ExplorerAdvanced,
            ValueName = "HideFileExt",
            EnabledValue = 0,      // HideFileExt = 0 means extensions ARE shown
            DisabledValue = 1,
            RequiresExplorerRestart = true,
            DefaultWhenUnset = TweakState.Enabled,
        },
        new()
        {
            Id = "explorer.show-hidden-files",
            Name = "Show hidden files",
            Description = "Show files and folders marked as hidden.",
            Hive = RegistryHive.CurrentUser,
            KeyPath = ExplorerAdvanced,
            ValueName = "Hidden",
            EnabledValue = 1,
            DisabledValue = 2,     // 2 = do not show hidden
            RequiresExplorerRestart = true,
            DefaultWhenUnset = TweakState.Disabled,
        },
        new()
        {
            Id = "explorer.show-protected-os-files",
            Name = "Show protected operating system files",
            Description = "Also reveal files Windows marks as protected. Handle with care.",
            Hive = RegistryHive.CurrentUser,
            KeyPath = ExplorerAdvanced,
            ValueName = "ShowSuperHidden",
            EnabledValue = 1,
            DisabledValue = 0,
            RequiresExplorerRestart = true,
        },
        new()
        {
            Id = "explorer.compact-mode",
            Name = "Compact mode",
            Description = "Reduce spacing in File Explorer for denser lists.",
            Hive = RegistryHive.CurrentUser,
            KeyPath = ExplorerAdvanced,
            ValueName = "UseCompactMode",
            EnabledValue = 1,
            DisabledValue = 0,
            RequiresExplorerRestart = true,
        },
        new()
        {
            Id = "explorer.open-to-this-pc",
            Name = "Open Explorer to This PC",
            Description = "Start File Explorer at This PC instead of Quick Access.",
            Hive = RegistryHive.CurrentUser,
            KeyPath = ExplorerAdvanced,
            ValueName = "LaunchTo",
            EnabledValue = 1,      // 1 = This PC
            DisabledValue = 2,     // 2 = Quick Access
            RequiresExplorerRestart = true,
            DefaultWhenUnset = TweakState.Disabled,
        },
        new()
        {
            Id = "explorer.show-recent-files",
            Name = "Show recently used files",
            Description = "Show recently used files in Quick Access.",
            Hive = RegistryHive.CurrentUser,
            KeyPath = ExplorerAdvanced,
            ValueName = "ShowRecent",
            EnabledValue = 1,
            DisabledValue = 0,
            RequiresExplorerRestart = true,
            DefaultWhenUnset = TweakState.Enabled,
        },
        new()
        {
            Id = "explorer.show-frequent-folders",
            Name = "Show frequently used folders",
            Description = "Show frequently used folders in Quick Access.",
            Hive = RegistryHive.CurrentUser,
            KeyPath = ExplorerAdvanced,
            ValueName = "ShowFrequent",
            EnabledValue = 1,
            DisabledValue = 0,
            RequiresExplorerRestart = true,
            DefaultWhenUnset = TweakState.Enabled,
        },
        new()
        {
            Id = "explorer.autoplay",
            Name = "Autoplay",
            Description = "Let Windows ask what to do when media or a device is inserted.",
            Hive = RegistryHive.CurrentUser,
            KeyPath = ExplorerAutoplay,
            ValueName = "DisableAutoplay",
            EnabledValue = 0,      // DisableAutoplay = 0 means autoplay is ON
            DisabledValue = 1,
            DefaultWhenUnset = TweakState.Enabled,
        },
        new()
        {
            Id = "explorer.autorun",
            Name = "Autorun for all drives",
            Description = "Allow autorun on every drive type. Off is the safer setting.",
            Hive = RegistryHive.LocalMachine,
            KeyPath = PoliciesExplorer,
            ValueName = "NoDriveTypeAutoRun",
            EnabledValue = 0,      // 0 = autorun allowed everywhere
            DisabledValue = 0xB5,  // 0xB5 = disabled on most drive types (Windows default)
            RequiresAdmin = true,
            DefaultWhenUnset = TweakState.Disabled,
        },
    ];

    /// <summary>Classic context menu and shell integration (ContextMenuPage).</summary>
    public static IReadOnlyList<RegistryTweak> ContextMenu { get; } =
    [
        new()
        {
            Id = "contextmenu.classic-menu",
            Name = "Classic context menu",
            Description = "Use the Windows 10 style right-click menu instead of the compact Windows 11 one.",
            Hive = RegistryHive.CurrentUser,
            KeyPath = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32",
            ValueName = string.Empty,   // the presence of the key itself is the switch
            ValueKind = RegistryValueKind.String,
            EnabledValue = string.Empty,
            DisabledValue = string.Empty,
            RequiresExplorerRestart = true,
            RemoveKeyWhenDisabled = true,
        },
    ];

    /// <summary>Visual effects and animations (VisualPage).</summary>
    public static IReadOnlyList<RegistryTweak> Visual { get; } =
    [
        new()
        {
            Id = "visual.window-animations",
            Name = "Window animations",
            Description = "Animate windows when minimising and maximising.",
            Hive = RegistryHive.CurrentUser,
            KeyPath = DesktopWindowMetrics,
            ValueName = "MinAnimate",
            ValueKind = RegistryValueKind.String,
            EnabledValue = "1",
            DisabledValue = "0",
            RequiresExplorerRestart = true,
            DefaultWhenUnset = TweakState.Enabled,
        },
        new()
        {
            Id = "visual.taskbar-animations",
            Name = "Taskbar animations",
            Description = "Animate taskbar buttons and thumbnail previews.",
            Hive = RegistryHive.CurrentUser,
            KeyPath = ExplorerAdvanced,
            ValueName = "TaskbarAnimations",
            EnabledValue = 1,
            DisabledValue = 0,
            RequiresExplorerRestart = true,
            DefaultWhenUnset = TweakState.Enabled,
        },
        new()
        {
            Id = "visual.drag-full-windows",
            Name = "Show window contents while dragging",
            Description = "Draw window contents live while dragging instead of an outline.",
            Hive = RegistryHive.CurrentUser,
            KeyPath = Desktop,
            ValueName = "DragFullWindows",
            ValueKind = RegistryValueKind.String,
            EnabledValue = "1",
            DisabledValue = "0",
            DefaultWhenUnset = TweakState.Enabled,
        },
        new()
        {
            Id = "visual.menu-show-delay",
            Name = "Instant menus (no delay)",
            Description = "Remove the delay before menus appear.",
            Hive = RegistryHive.CurrentUser,
            KeyPath = Desktop,
            ValueName = "MenuShowDelay",
            ValueKind = RegistryValueKind.String,
            EnabledValue = "0",
            DisabledValue = "400",
            DefaultWhenUnset = TweakState.Disabled,
        },
        new()
        {
            Id = "visual.listview-shadows",
            Name = "List view shadows",
            Description = "Show drop shadows under list view items.",
            Hive = RegistryHive.CurrentUser,
            KeyPath = ExplorerAdvanced,
            ValueName = "ListviewShadow",
            EnabledValue = 1,
            DisabledValue = 0,
            RequiresExplorerRestart = true,
            DefaultWhenUnset = TweakState.Enabled,
        },
    ];

    /// <summary>System-level tweaks (SystemPage).</summary>
    public static IReadOnlyList<RegistryTweak> System { get; } =
    [
        new()
        {
            Id = "system.startup-sound",
            Name = "Windows startup sound",
            Description = "Play the Windows startup sound at sign-in.",
            Hive = RegistryHive.LocalMachine,
            KeyPath = BootAnimation,
            ValueName = "DisableStartupSound",
            EnabledValue = 0,      // DisableStartupSound = 0 means the sound plays
            DisabledValue = 1,
            RequiresAdmin = true,
            DefaultWhenUnset = TweakState.Enabled,
        },
        new()
        {
            Id = "system.transparency",
            Name = "Transparency effects",
            Description = "Enable acrylic and transparency effects across Windows.",
            Hive = RegistryHive.CurrentUser,
            KeyPath = ThemesPersonalize,
            ValueName = "EnableTransparency",
            EnabledValue = 1,
            DisabledValue = 0,
            DefaultWhenUnset = TweakState.Enabled,
        },
        new()
        {
            Id = "system.apps-use-dark-mode",
            Name = "Dark mode for apps",
            Description = "Use the dark theme for Windows apps.",
            Hive = RegistryHive.CurrentUser,
            KeyPath = ThemesPersonalize,
            ValueName = "AppsUseLightTheme",
            EnabledValue = 0,      // AppsUseLightTheme = 0 means dark
            DisabledValue = 1,
            DefaultWhenUnset = TweakState.Disabled,
        },
        new()
        {
            Id = "system.system-uses-dark-mode",
            Name = "Dark mode for Windows",
            Description = "Use the dark theme for the Windows shell.",
            Hive = RegistryHive.CurrentUser,
            KeyPath = ThemesPersonalize,
            ValueName = "SystemUsesLightTheme",
            EnabledValue = 0,
            DisabledValue = 1,
            DefaultWhenUnset = TweakState.Disabled,
        },
        new()
        {
            Id = "system.disable-notification-center",
            Name = "Disable Notification Center",
            Description = "Turn off toast notifications and the Action Center.",
            Hive = RegistryHive.CurrentUser,
            KeyPath = PoliciesWindowsExplorer,
            ValueName = "DisableNotificationCenter",
            EnabledValue = 1,
            DisabledValue = 0,
            DefaultWhenUnset = TweakState.Disabled,
        },
    ];

    /// <summary>Security tweaks (SecurityPage).</summary>
    public static IReadOnlyList<RegistryTweak> Security { get; } =
    [
        new()
        {
            Id = "security-defender-disable",
            Name = "Disable Windows Defender",
            Description = "Turn off Windows Defender real-time protection.",
            Hive = RegistryHive.LocalMachine,
            KeyPath = @"SOFTWARE\Policies\Microsoft\Windows Defender",
            ValueName = "DisableAntiSpyware",
            EnabledValue = 1,
            DisabledValue = 0,
            RequiresAdmin = true,
            DefaultWhenUnset = TweakState.Disabled,
        },
        new()
        {
            Id = "security-defender-tamper",
            Name = "Disable Tamper Protection",
            Description = "Disable Tamper Protection for Windows Defender. Note: Windows may prevent this change even with admin rights.",
            Hive = RegistryHive.LocalMachine,
            KeyPath = @"SOFTWARE\Microsoft\Windows Defender\Features",
            ValueName = "TamperProtection",
            EnabledValue = 0,
            DisabledValue = 5,
            RequiresAdmin = true,
            DefaultWhenUnset = TweakState.Disabled,
        },
        new()
        {
            Id = "security-vbs",
            Name = "Enable Virtualization-Based Security",
            Description = "Enable Virtualization-Based Security (VBS). Reboot required.",
            Hive = RegistryHive.LocalMachine,
            KeyPath = @"SYSTEM\CurrentControlSet\Control\DeviceGuard",
            ValueName = "EnableVirtualizationBasedSecurity",
            EnabledValue = 1,
            DisabledValue = 0,
            RequiresAdmin = true,
            DefaultWhenUnset = TweakState.Disabled,
        },
        new()
        {
            Id = "security-memory-integrity",
            Name = "Enable Memory Integrity",
            Description = "Enable Memory Integrity (Hypervisor-enforced Code Integrity). Reboot required.",
            Hive = RegistryHive.LocalMachine,
            KeyPath = @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity",
            ValueName = "Enabled",
            EnabledValue = 1,
            DisabledValue = 0,
            RequiresAdmin = true,
            DefaultWhenUnset = TweakState.Disabled,
        },
        new()
        {
            Id = "security-vulnerable-driver-blocklist",
            Name = "Enable Vulnerable Driver Blocklist",
            Description = "Enable the Vulnerable Driver Blocklist. Reboot required.",
            Hive = RegistryHive.LocalMachine,
            KeyPath = @"SYSTEM\CurrentControlSet\Control\CI\Config",
            ValueName = "VulnerableDriverBlocklistEnable",
            EnabledValue = 1,
            DisabledValue = 0,
            RequiresAdmin = true,
            DefaultWhenUnset = TweakState.Disabled,
        },
        new()
        {
            Id = "security-spectre-meltdown",
            Name = "Enable Spectre & Meltdown Mitigations",
            Description = "Enable Spectre and Meltdown CPU mitigations. May impact performance.",
            Hive = RegistryHive.LocalMachine,
            KeyPath = @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            ValueName = "FeatureSettingsOverride",
            EnabledValue = 0,
            DisabledValue = 3,
            RequiresAdmin = true,
            DefaultWhenUnset = TweakState.Disabled,
        },
        new()
        {
            Id = "security-uac",
            Name = "Disable User Account Control",
            Description = "Disable User Account Control. This reduces security — only disable if you understand the risks.",
            Hive = RegistryHive.LocalMachine,
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
            ValueName = "EnableLUA",
            EnabledValue = 0,
            DisabledValue = 1,
            RequiresAdmin = true,
            DefaultWhenUnset = TweakState.Disabled,
        },
        new()
        {
            Id = "security-smartscreen",
            Name = "Disable SmartScreen",
            Description = "Disable SmartScreen. This reduces protection against malicious apps.",
            Hive = RegistryHive.LocalMachine,
            KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\System",
            ValueName = "EnableSmartScreen",
            EnabledValue = 0,
            DisabledValue = 1,
            RequiresAdmin = true,
            DefaultWhenUnset = TweakState.Disabled,
        },
    ];

    /// <summary>Performance tweaks (PerformancePage).</summary>
    public static IReadOnlyList<RegistryTweak> Performance { get; } =
    [
        new()
        {
            Id = "perf-skiptick",
            Name = "Skip Tick Override",
            Description = "Override the system timer resolution skip tick. May improve timer accuracy.",
            Hive = RegistryHive.LocalMachine,
            KeyPath = @"SYSTEM\CurrentControlSet\Control\Session Manager\kernel",
            ValueName = "SkipTickOverride",
            EnabledValue = 1,
            DisabledValue = 0,
            RequiresAdmin = true,
            DefaultWhenUnset = TweakState.Disabled,
        },
        new()
        {
            Id = "perf-platform-tick",
            Name = "Use Platform Tick",
            Description = "Use the platform tick source for timer resolution.",
            Hive = RegistryHive.LocalMachine,
            KeyPath = @"SYSTEM\CurrentControlSet\Control\Session Manager\kernel",
            ValueName = "UsePlatformTick",
            EnabledValue = 1,
            DisabledValue = 0,
            RequiresAdmin = true,
            DefaultWhenUnset = TweakState.Disabled,
        },
        new()
        {
            Id = "perf-timer-expiration",
            Name = "Timer Expiration",
            Description = "Set the timer expiration behavior for the system.",
            Hive = RegistryHive.LocalMachine,
            KeyPath = @"SYSTEM\CurrentControlSet\Control\Session Manager\kernel",
            ValueName = "TimerExpiration",
            EnabledValue = 1,
            DisabledValue = 0,
            RequiresAdmin = true,
            DefaultWhenUnset = TweakState.Disabled,
        },
        new()
        {
            Id = "perf-mpo",
            Name = "Multiplane Overlay (MPO)",
            Description = "Enable Multiplane Overlay for display composition. Requires Explorer restart.",
            Hive = RegistryHive.LocalMachine,
            KeyPath = @"SOFTWARE\Microsoft\Windows\Dwm",
            ValueName = "OverlayTestMode",
            EnabledValue = 1,
            DisabledValue = 0,
            RequiresAdmin = true,
            RequiresExplorerRestart = true,
            DefaultWhenUnset = TweakState.Disabled,
        },
        new()
        {
            Id = "perf-gpu-scheduling",
            Name = "Hardware-Accelerated GPU Scheduling",
            Description = "Enable Hardware-Accelerated GPU Scheduling. Value 2 = enabled, 1 = disabled.",
            Hive = RegistryHive.LocalMachine,
            KeyPath = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
            ValueName = "HwSchMode",
            EnabledValue = 2,
            DisabledValue = 1,
            RequiresAdmin = true,
            DefaultWhenUnset = TweakState.Disabled,
        },
        new()
        {
            Id = "perf-working-set",
            Name = "Working Set Adjustment",
            Description = "Favor system performance over app responsiveness by using a large system cache.",
            Hive = RegistryHive.LocalMachine,
            KeyPath = @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            ValueName = "LargeSystemCache",
            EnabledValue = 1,
            DisabledValue = 0,
            RequiresAdmin = true,
            DefaultWhenUnset = TweakState.Disabled,
        },
        new()
        {
            Id = "perf-game-mode",
            Name = "Game Mode",
            Description = "Allow Windows to automatically enable Game Mode for games.",
            Hive = RegistryHive.CurrentUser,
            KeyPath = @"Software\Microsoft\GameBar",
            ValueName = "AllowAutoGameMode",
            EnabledValue = 1,
            DisabledValue = 0,
            RequiresAdmin = false,
            DefaultWhenUnset = TweakState.Disabled,
        },
        new()
        {
            Id = "perf-game-dvr",
            Name = "Game DVR",
            Description = "Enable Game DVR for background recording of gameplay.",
            Hive = RegistryHive.CurrentUser,
            KeyPath = @"System\GameConfigStore",
            ValueName = "GameDVR_Enabled",
            EnabledValue = 1,
            DisabledValue = 0,
            RequiresAdmin = false,
            DefaultWhenUnset = TweakState.Disabled,
        },
    ];

    /// <summary>Network offload tweaks (NetworkPage). Per-interface GUID paths.</summary>
    public static IReadOnlyList<RegistryTweak> Network { get; } =
    [
        new()
        {
            Id = "network-lso-v2-ipv4",
            Name = "Large Send Offload v2 (IPv4)",
            Description = "Enable Large Send Offload v2 for IPv4 to reduce CPU usage during network transfers.",
            Hive = RegistryHive.LocalMachine,
            KeyPath = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{GUID}",
            ValueName = "LsoV2Enabled",
            EnabledValue = 1,
            DisabledValue = 0,
            RequiresAdmin = true,
            DefaultWhenUnset = TweakState.Enabled,
        },
        new()
        {
            Id = "network-rss",
            Name = "Receive Side Scaling",
            Description = "Enable Receive Side Scaling to distribute network processing across CPU cores.",
            Hive = RegistryHive.LocalMachine,
            KeyPath = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{GUID}",
            ValueName = "RSSEnabled",
            EnabledValue = 1,
            DisabledValue = 0,
            RequiresAdmin = true,
            DefaultWhenUnset = TweakState.Enabled,
        },
        new()
        {
            Id = "network-checksum-offload-ipv4",
            Name = "TCP Checksum Offload (IPv4)",
            Description = "Enable TCP checksum offload for IPv4 to reduce CPU usage.",
            Hive = RegistryHive.LocalMachine,
            KeyPath = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{GUID}",
            ValueName = "TCPChecksumOffloadIPv4",
            EnabledValue = 1,
            DisabledValue = 0,
            RequiresAdmin = true,
            DefaultWhenUnset = TweakState.Enabled,
        },
    ];

    /// <summary>Audio enhancement tweaks (SoundPage).</summary>
    public static IReadOnlyList<RegistryTweak> Sound { get; } =
    [
        new()
        {
            Id = "sound.spatial-audio",
            Name = "Spatial Audio",
            Description = "Enable spatial audio for headphones and speakers.",
            Hive = RegistryHive.LocalMachine,
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Audio",
            ValueName = "DisableSpatialAudio",
            EnabledValue = 0,
            DisabledValue = 1,
            RequiresAdmin = true,
        },
        new()
        {
            Id = "sound.audio-enhancements",
            Name = "Audio Enhancements",
            Description = "Enable Windows audio enhancements like bass boost and virtualization.",
            Hive = RegistryHive.LocalMachine,
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Audio",
            ValueName = "DisableAudioEnhancements",
            EnabledValue = 0,
            DisabledValue = 1,
            RequiresAdmin = true,
        },
    ];

    /// <summary>Every tweak, flattened — used by the General overview page.</summary>
    public static IReadOnlyList<RegistryTweak> All { get; } =
        Explorer.Concat(ContextMenu).Concat(Visual).Concat(System).Concat(Security).Concat(Performance).Concat(Network).Concat(Sound).ToList();

    /// <summary>Lookup by <see cref="RegistryTweak.Id"/>.</summary>
    public static RegistryTweak? Find(string id) =>
        All.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.Ordinal));
}
