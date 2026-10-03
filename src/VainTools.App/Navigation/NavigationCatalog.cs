using VainTools.App.Features.About;
using VainTools.App.Features.Affinity;
using VainTools.App.Features.Apps;
using VainTools.App.Features.DeviceCleaner;
using VainTools.App.Features.DriverManager;
using VainTools.App.Features.DriveScanner;
using VainTools.App.Features.Experimental;
using VainTools.App.Features.General;
using VainTools.App.Features.Gpu;
using VainTools.App.Features.Gpu.Display;
using VainTools.App.Features.Gpu.Nvidia.Drs;
using VainTools.App.Features.Home;
using VainTools.App.Features.Network;
using VainTools.App.Features.Performance;
using VainTools.App.Features.Powereditor;
using VainTools.App.Features.Security;
using VainTools.App.Features.Sound;
using VainTools.App.Features.Startup;
using VainTools.App.Features.System;
using VainTools.App.Features.VainTools;

namespace VainTools.App.Navigation;

/// <summary>
/// A single navigable entry in the shell's NavigationView.
/// </summary>
/// <param name="Label">Display text shown in the nav pane.</param>
/// <param name="Glyph">Segoe Fluent Icons glyph for the entry.</param>
/// <param name="PageType">The page type navigated to when the entry is selected.</param>
public sealed record NavigationEntry(string Label, string Glyph, Type PageType);

/// <summary>
/// A named group of entries. Groups are rendered as expandable
/// <c>NavigationViewItem</c> parents so the pane mirrors Vain Toolbox's
/// grouped page tree.
/// </summary>
/// <param name="Label">Group heading text.</param>
/// <param name="Glyph">Segoe Fluent Icons glyph for the group.</param>
/// <param name="Children">Entries nested under this group.</param>
public sealed record NavigationGroup(string Label, string Glyph, IReadOnlyList<NavigationEntry> Children);

/// <summary>
/// The application's navigation model.
///
/// The tree mirrors the real Vain Toolbox layout recovered from
/// <c>Vain Toolbox.exe</c> (see .planning/research/VAIN-TOOLBOX-GROUND-TRUTH.md):
/// Home, Vain Tools, General (with five sub-pages), System, Security,
/// Experimental, Performance, Apps (four sub-pages), Sound, Affinity, Startup,
/// Power Editor, GPU (Display + NVIDIA), Network, Tools (three sub-pages), About.
/// </summary>
public static class NavigationCatalog
{
    /// <summary>Glyphs reused across the tree (Segoe Fluent Icons).</summary>
    private static class Glyph
    {
        public const string Home = "\uE80F";
        public const string Settings = "\uE713";
        public const string Folder = "\uEC50";
        public const string Menu = "\uE8A7";
        public const string Brush = "\uE790";
        public const string Clock = "\uE916";
        public const string Eye = "\uE7B3";
        public const string Shield = "\uE72E";
        public const string Beaker = "\uE9CE";
        public const string Speed = "\uE9D9";
        public const string Package = "\uE74C";
        public const string Apps = "\uE71D";
        public const string Feature = "\uE7B8";
        public const string Store = "\uE719";
        public const string Volume = "\uE767";
        public const string Cpu = "\uE9D2";
        public const string Power = "\uE945";
        public const string Gpu = "\uE7F4";
        public const string Monitor = "\uE7F8";
        public const string Network = "\uE968";
        public const string Broom = "\uE74D";
        public const string Info = "\uE946";
    }

    /// <summary>Ungrouped entries shown at the top of the nav pane.</summary>
    public static IReadOnlyList<NavigationEntry> TopLevel { get; } =
    [
        new("Home", Glyph.Home, typeof(HomePage)),
        new("Vain Tools", Glyph.Settings, typeof(VainToolsPage)),
    ];

    /// <summary>Groups rendered as expandable parents, in display order.</summary>
    public static IReadOnlyList<NavigationGroup> Groups { get; } =
    [
        new("General", Glyph.Settings,
        [
            new("General", Glyph.Settings, typeof(GeneralPage)),
            new("Explorer", Glyph.Folder, typeof(ExplorerPage)),
            new("Context Menu", Glyph.Menu, typeof(ContextMenuPage)),
            new("Visual", Glyph.Brush, typeof(VisualPage)),
            new("Date & Time", Glyph.Clock, typeof(DateTimePage)),
            new("Settings Visibility", Glyph.Eye, typeof(SettingsVisibilityPage)),
        ]),

        new("System", Glyph.Settings,
        [
            new("System", Glyph.Settings, typeof(SystemPage)),
            new("Security", Glyph.Shield, typeof(SecurityPage)),
            new("Experimental", Glyph.Beaker, typeof(ExperimentalPage)),
            new("Performance", Glyph.Speed, typeof(PerformancePage)),
        ]),

        new("Apps", Glyph.Package,
        [
            new("Appx Manager", Glyph.Package, typeof(AppxManagerPage)),
            new("Installed Apps", Glyph.Apps, typeof(InstalledAppsPage)),
            new("Optional Features", Glyph.Feature, typeof(OptionalFeaturesPage)),
            new("Store", Glyph.Store, typeof(StorePage)),
        ]),

        new("Sound", Glyph.Volume,
        [
            new("Sound", Glyph.Volume, typeof(SoundPage)),
        ]),

        new("Affinity", Glyph.Cpu,
        [
            new("Affinity", Glyph.Cpu, typeof(AffinityPage)),
        ]),

        new("Startup", Glyph.Feature,
        [
            new("Startup", Glyph.Feature, typeof(StartupPage)),
        ]),

        new("Power Editor", Glyph.Power,
        [
            new("Power Editor", Glyph.Power, typeof(PowereditorPage)),
        ]),

        new("GPU", Glyph.Gpu,
        [
            new("GPU", Glyph.Gpu, typeof(GpuPage)),
            new("Display", Glyph.Monitor, typeof(DisplayPage)),
            new("NVIDIA", Glyph.Speed, typeof(DrsPage)),
        ]),

        new("Network", Glyph.Network,
        [
            new("Network", Glyph.Network, typeof(NetworkPage)),
        ]),

        new("Tools", Glyph.Broom,
        [
            new("Device Cleaner", Glyph.Broom, typeof(DeviceCleanerPage)),
            new("Drive Scanner", Glyph.Speed, typeof(DriveScannerPage)),
            new("Driver Manager", Glyph.Feature, typeof(DriverManagerPage)),
        ]),
    ];

    /// <summary>Entries pinned to the bottom of the nav pane.</summary>
    public static IReadOnlyList<NavigationEntry> Footer { get; } =
    [
        new("About", Glyph.Info, typeof(AboutPage)),
    ];

    /// <summary>Every entry in the tree, flattened — used to resolve the selected item.</summary>
    public static IReadOnlyList<NavigationEntry> AllEntries { get; } =
        TopLevel
            .Concat(Groups.SelectMany(g => g.Children))
            .Concat(Footer)
            .ToList();
}
