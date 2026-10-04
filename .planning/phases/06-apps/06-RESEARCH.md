# Phase 06: Apps - Research

**Researched:** 2026-10-04
**Domain:** Windows app/package management (WinRT, Registry, DISM, winget)
**Confidence:** HIGH

## Summary

Phase 6 implements four app/package management pages: Appx Manager, Installed Apps, Optional Features, and Store. Each page follows the established Phase 5 pattern: a custom per-page XAML layout with a list/grid and action buttons, a ViewModel using CommunityToolkit.Mvvm source generators, and a service layer abstracting the underlying Windows APIs.

The four pages cover distinct Windows management surfaces: WinRT `PackageManager` for Appx packages, direct registry reads for installed programs, DISM API for optional features, and winget CLI for the store. All destructive operations require elevation and explicit user confirmation. The research confirms that the existing project patterns (service interfaces, DI registration, ViewModelBase, elevation detection, IProcessRunner) provide a solid foundation for all four pages.

**Primary recommendation:** Implement all four pages following the Phase 5 pattern exactly — service interface + implementation, ViewModel with source generators, custom XAML layout, and DI registration. Use `IProcessRunner` for winget CLI execution (Store page) but direct `Process.Start` for Installed Apps uninstall (per D-05). For DISM, use P/Invoke to `DismEnableFeature`/`DismDisableFeature` from `DismApi.dll` — this is the documented COM interop approach.

## User Constraints (from CONTEXT.md)

### Decisions (locked)
- **D-01:** Use WinRT `Windows.Management.Deployment.PackageManager` API directly in C#
- **D-02:** Appx Manager lists both installed and provisioned packages. Removal via `PackageManager.RemovePackageAsync`
- **D-03:** Use direct registry reads on `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall`, `HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall`, and `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall`
- **D-04:** Use `UninstallString` and `QuietUninstallString` from registry values. "Copy uninstall command" copies the raw string to clipboard.
- **D-05:** Execute uninstall via direct `Process.Start` on the `UninstallString` — no `IProcessRunner` abstraction for this page.
- **D-06:** Use DISM API (via P/Invoke or COM interop) for `Enable-WindowsOptionalFeature` / `Disable-WindowsOptionalFeature`. Requires elevation.
- **D-07:** List features via DISM's `GetOptionalFeatures` or equivalent.
- **D-08:** Use winget CLI for listing installable apps (`winget search`) and installing (`winget install`)
- **D-09:** Store page lists installable apps and can install one. Listing mechanism at researcher's discretion.
- **D-10:** Appx removal, optional feature enable/disable, and uninstall all require admin. Follow Phase 3/4/5 elevation pattern.
- **D-11:** Destructive operations follow `safety.always_confirm_destructive` — explicit confirmation before execution.
- **D-12:** Follow the real app's behavior 1:1 for failure states.
- **D-13:** After removing an Appx package or uninstalling a program, reload the list.
- **D-14:** Follow the Phase 5 custom layout pattern — each page has its own custom XAML with a list/grid and action buttons.

### Claude's Discretion
- Exact DISM API P/Invoke signatures and COM interop details
- winget CLI output parsing (search results, install progress)
- Error/failure state UI messages
- Store page listing mechanism (winget search vs curated list)
- Test strategy for WinRT PackageManager, DISM P/Invoke, and winget CLI mocking

### Deferred Ideas
- None documented in CONTEXT.md

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| APPX-01 | Enumerate installed and provisioned Appx packages via WinRT PackageManager | `PackageManager.FindPackages()` and `PackageManager.FindProvisionedPackages()` — both return `IEnumerable<Package>` with `Id.FullName`, `Id.Name`, `Id.Version`, `InstalledLocation` |
| APPX-02 | Remove an Appx package via `PackageManager.RemovePackageAsync` | `RemovePackageAsync(string packageFullName)` returns `IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress>` — requires elevation |
| INST-01 | Enumerate installed programs from registry uninstall keys | Read `DisplayName`, `DisplayVersion`, `Publisher`, `InstallLocation`, `UninstallString`, `QuietUninstallString` from 3 registry hives |
| INST-02 | Execute uninstall via `Process.Start` on `UninstallString` | Direct `Process.Start` with `UseShellExecute = true` — no IProcessRunner abstraction per D-05 |
| INST-03 | Copy uninstall command to clipboard | `Clipboard.SetText()` with the raw `UninstallString` or `QuietUninstallString` value |
| OPT-01 | List Windows optional features via DISM API | `DismGetFeatures` P/Invoke or `DismApi.dll` COM interop — returns feature name, state (Enabled/Disabled) |
| OPT-02 | Enable/disable optional features via DISM API | `DismEnableFeature` / `DismDisableFeature` P/Invoke — requires elevation, may require restart |
| STOR-01 | List installable apps via `winget search` | `winget search` command with `--accept-source-agreements` flag — parse tabular output |
| STOR-02 | Install an app via `winget install` | `winget install --id <Id> --exact --accept-source-agreements --accept-package-agreements` — requires elevation for machine-wide installs |

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Windows.Management.Deployment (WinRT) | Windows 10 1809+ | Appx package enumeration and removal | Built into Windows SDK; accessible from .NET via WinRT projection |
| Microsoft.Win32.Registry | .NET 10 | Registry reads for installed programs | Standard .NET registry API; no external dependency |
| DismApi.dll (P/Invoke) | Windows 8+ | DISM optional feature management | Documented DISM API; ships with Windows |
| winget.exe (CLI) | Windows 10 1809+ | Store page app search and install | Ships with App Installer; standard Windows package manager |
| CommunityToolkit.Mvvm | 8.x (existing) | ViewModel source generators | Already used throughout the project |
| Microsoft.Extensions.Hosting | .NET 10 (existing) | DI registration | Already used throughout the project |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| IProcessRunner | Existing (Phase 4) | winget CLI execution abstraction | Store page only — D-05 says direct Process.Start for Installed Apps |
| IDialogService | Existing (Framework) | Confirmation dialogs for destructive ops | All pages with remove/uninstall/disable actions |
| IInfoBarService | Existing (Framework) | Success/error notifications | All pages |
| ILogger<T> | Existing | Logging | All services and ViewModels |

## Architecture Patterns

### System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                         UI Layer (XAML Pages)                        │
│  AppxManagerPage │ InstalledAppsPage │ OptionalFeaturesPage │ StorePage│
└────────┬───────────────┬──────────────────┬──────────────────┬───────┘
         │               │                  │                  │
┌────────▼───────────────▼──────────────────▼──────────────────▼───────┐
│                    ViewModel Layer (MVVM)                            │
│  AppxManagerVM     InstalledAppsVM    OptionalFeaturesVM    StoreVM  │
│  (CommunityToolkit.Mvvm source generators, ObservableCollection<T>)  │
└────────┬───────────────┬──────────────────┬──────────────────┬───────┘
         │               │                  │                  │
┌────────▼───────────────▼──────────────────▼──────────────────▼───────┐
│                      Service Layer                                   │
│  IAppxService    IInstalledAppsService  IOptimalFeaturesService      │
│  IStoreService   IProcessRunner (existing)                           │
└────────┬───────────────┬──────────────────┬──────────────────┬───────┘
         │               │                  │                  │
┌────────▼───────────────▼──────────────────▼──────────────────▼───────┐
│                    Windows API Layer                                  │
│  WinRT PackageManager │ Registry (3 hives) │ DISM P/Invoke │ winget │
└─────────────────────────────────────────────────────────────────────┘
```

### Recommended Project Structure

```
src/VainTools.App/
├── Features/Apps/
│   ├── AppxManagerPage.xaml          (scaffold exists — implement)
│   ├── AppxManagerPage.xaml.cs       (scaffold exists — implement)
│   ├── InstalledAppsPage.xaml        (scaffold exists — implement)
│   ├── InstalledAppsPage.xaml.cs     (scaffold exists — implement)
│   ├── OptionalFeaturesPage.xaml     (scaffold exists — implement)
│   ├── OptionalFeaturesPage.xaml.cs  (scaffold exists — implement)
│   ├── StorePage.xaml                (scaffold exists — implement)
│   └── StorePage.xaml.cs             (scaffold exists — implement)
├── Services/
│   ├── IAppxService.cs               (new)
│   ├── AppxService.cs                (new)
│   ├── IInstalledAppsService.cs      (new)
│   ├── InstalledAppsService.cs       (new)
│   ├── IOptimalFeaturesService.cs    (new)
│   ├── OptionalFeaturesService.cs    (new)
│   ├── IStoreService.cs              (new)
│   └── StoreService.cs               (new)
└── ViewModels/
    ├── AppxManagerViewModel.cs        (new)
    ├── InstalledAppsViewModel.cs      (new)
    ├── OptionalFeaturesViewModel.cs   (new)
    └── StoreViewModel.cs              (new)

src/VainTools.Tests/
├── AppxServiceTests.cs               (new)
├── InstalledAppsServiceTests.cs       (new)
├── OptionalFeaturesServiceTests.cs    (new)
└── StoreServiceTests.cs              (new)
```

### Pattern 1: Service Interface + Implementation (Phase 5 Standard)

**What:** Every page has a service interface and implementation, registered as singleton in DI. The ViewModel consumes the interface, enabling test mocking.

**When to use:** All four pages without exception.

**Example:**
```csharp
// IAppxService.cs
public interface IAppxService
{
    IReadOnlyList<AppxPackage> GetInstalledPackages();
    IReadOnlyList<AppxPackage> GetProvisionedPackages();
    Task<DeploymentResult> RemovePackageAsync(string packageFullName);
}

public sealed record AppxPackage(
    string FullName,
    string Name,
    string Publisher,
    string Version,
    string InstallLocation,
    bool IsProvisioned);
```

### Pattern 2: ViewModel with Source Generators (Phase 5 Standard)

**What:** ViewModel extends `ViewModelBase`, uses `[ObservableProperty]` and `[RelayCommand]` from CommunityToolkit.Mvvm. Collections are `ObservableCollection<T>`.

**When to use:** All four ViewModels.

**Example:**
```csharp
public partial class AppxManagerViewModel : ViewModelBase
{
    private readonly IAppxService _appxService;
    private readonly IRegistryTweakService _registry;
    private readonly IDialogService _dialogs;
    private readonly IInfoBarService _infoBar;
    private readonly ILogger<AppxManagerViewModel> _logger;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    [ObservableProperty]
    public partial bool IsElevated { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public ObservableCollection<AppxPackage> Packages { get; } = [];

    public AppxManagerViewModel(
        IAppxService appxService,
        IRegistryTweakService registry,
        IDialogService dialogs,
        IInfoBarService infoBar,
        ILogger<AppxManagerViewModel> logger)
    {
        _appxService = appxService;
        _registry = registry;
        _dialogs = dialogs;
        _infoBar = infoBar;
        _logger = logger;
        Title = "Appx Manager";
        IsElevated = registry.IsElevated;
    }

    [RelayCommand]
    public void Refresh() { /* ... */ }

    [RelayCommand(CanExecute = nameof(CanRemovePackage))]
    public async Task RemovePackageAsync(AppxPackage package) { /* ... */ }

    private bool CanRemovePackage(AppxPackage? package) => package is not null;
}
```

### Pattern 3: Elevation Detection (Phase 3/4/5 Standard)

**What:** `IRegistryTweakService.IsElevated` is read in the ViewModel constructor and exposed as `IsElevated`. Destructive commands check elevation before executing.

**When to use:** Appx Manager (remove), Optional Features (enable/disable), Installed Apps (uninstall).

**Example:**
```csharp
private bool CanRemovePackage(AppxPackage? package) =>
    package is not null && IsElevated;
```

### Pattern 4: Destructive Operation Confirmation (D-11)

**What:** All destructive operations (remove package, uninstall program, disable feature) require explicit confirmation via `IDialogService.ConfirmAsync`.

**When to use:** Appx Manager remove, Installed Apps uninstall, Optional Features disable.

**Example:**
```csharp
var confirmed = await _dialogs.ConfirmAsync(
    "Remove Package",
    $"Remove {package.Name}? This cannot be undone.",
    confirmText: "Remove");

if (!confirmed)
{
    StatusMessage = "Remove cancelled.";
    return;
}
```

### Pattern 5: Post-Destructive Reload (D-13)

**What:** After a successful destructive operation, reload the list to reflect the change.

**When to use:** Appx Manager (after remove), Installed Apps (after uninstall).

**Example:**
```csharp
// After successful removal
await RefreshAsync();
StatusMessage = $"Removed {package.Name}";
```

### Anti-Patterns to Avoid

- **Don't use IProcessRunner for Installed Apps uninstall:** D-05 explicitly says direct `Process.Start`. The `IProcessRunner` abstraction is for the Store page (winget CLI) only.
- **Don't swallow exceptions in services:** Let exceptions propagate to the ViewModel, which logs and shows them via `IInfoBarService`. The service layer should not catch-and-ignore.
- **Don't block the UI thread:** WinRT async methods (`RemovePackageAsync`) and process execution (`Process.Start`) must be awaited. Use `async Task` commands.
- **Don't forget elevation checks:** All destructive operations require admin. Check `IsElevated` before executing and show a clear error if not elevated.
- **Don't hard-code registry paths:** Use the same pattern as `StartupService` — define paths as `static readonly` arrays with hive + path tuples.
- **Don't parse winget output with regex:** winget search output is tabular with fixed column positions. Use `string.Split` with `StringSplitOptions.RemoveEmptyEntries` and trim.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Appx package enumeration | Custom COM interop | WinRT `PackageManager.FindPackages()` | Built into Windows SDK; handles all edge cases |
| Appx package removal | Custom deployment API | `PackageManager.RemovePackageAsync()` | Handles dependency cleanup, progress reporting |
| Installed programs list | WMI `Win32_Product` | Direct registry reads on Uninstall keys | WMI is slow and triggers consistency checks; registry is fast and complete |
| Optional feature management | PowerShell `Enable-WindowsOptionalFeature` | DISM API P/Invoke | PowerShell is heavyweight; DISM API is the underlying COM interface |
| App installation | Custom installer | winget CLI | Standard Windows package manager; handles dependencies, sources, elevation |
| Process execution (Store) | Direct `Process.Start` in ViewModel | `IProcessRunner` abstraction | Already exists from Phase 4; enables test mocking |
| Elevation detection | Custom token check | `IRegistryTweakService.IsElevated` | Already exists; consistent across all pages |
| Confirmation dialogs | Custom dialog | `IDialogService.ConfirmAsync` | Already exists; consistent UX |

## Common Pitfalls

### Pitfall 1: WinRT PackageManager Requires Elevation for Removal

**What goes wrong:** `RemovePackageAsync` throws `UnauthorizedAccessException` or returns a `DeploymentResult` with `Error` status when the process is not elevated.

**Why it happens:** Removing Appx packages is a system-wide operation that requires administrator privileges.

**How to avoid:** Check `IsElevated` before attempting removal. Show a clear error message if not elevated. The `CanRemovePackage` command guard should include `IsElevated`.

**Warning signs:** `DeploymentResult.Status == DeploymentStatus.Error` or `UnauthorizedAccessException` thrown.

### Pitfall 2: Registry Uninstall Keys Have Both 32-bit and 64-bit Views

**What goes wrong:** Only reading `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall` misses 32-bit programs on 64-bit Windows.

**Why it happens:** 32-bit programs write to `HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall` due to registry redirection.

**How to avoid:** Read all three hives: `HKLM\SOFTWARE`, `HKLM\SOFTWARE\WOW6432Node`, and `HKCU\SOFTWARE`. This is the same pattern as `StartupService.RunKeyLocations`.

**Warning signs:** 32-bit programs (e.g., older apps) missing from the list.

### Pitfall 3: UninstallString May Contain Arguments

**What goes wrong:** `UninstallString` often contains both the executable path and command-line arguments (e.g., `"C:\Program Files\App\uninstall.exe" /silent`). Passing the entire string as `FileName` to `Process.Start` fails.

**Why it happens:** The registry value is a complete command line, not just an executable path.

**How to avoid:** Use `ProcessStartInfo` with `FileName` and `Arguments` split, or use `UseShellExecute = true` and pass the entire string as `FileName` (Windows parses it). The simplest approach: `Process.Start(new ProcessStartInfo { FileName = uninstallString, UseShellExecute = true })`.

**Warning signs:** `Win32Exception: The system cannot find the file specified` when the uninstall string contains arguments.

### Pitfall 4: DISM API Requires COM Initialization

**What goes wrong:** DISM P/Invoke calls fail with `COMException` or `InvalidOperationException` if COM is not initialized.

**Why it happens:** DISM API is a COM-based API that requires `CoInitializeEx` to be called first.

**How to avoid:** The DISM P/Invoke signatures handle COM initialization internally when using `DismApi.Initialize()`. Call `DismApi.Initialize(DismLogErrorsWarnings, 0)` at the start of each DISM operation and `DismApi.Shutdown()` at the end.

**Warning signs:** `COMException` or `RPC_E_WRONG_THREAD` errors.

### Pitfall 5: winget CLI Output Format Varies by Version

**What goes wrong:** Parsing `winget search` output fails because column widths or delimiters differ across winget versions.

**Why it happens:** winget output is designed for human readability, not machine parsing. The format has changed between versions.

**How to avoid:** Use `--format json` flag if available (winget 1.4+), or parse the tabular output defensively by splitting on multiple spaces and trimming. Alternatively, use `winget search --query <term> --accept-source-agreements` and parse the `Name` and `Id` columns.

**Warning signs:** Parsed results have empty or incorrect fields.

### Pitfall 6: Provisioned Packages Cannot Be Removed the Same Way as Installed Packages

**What goes wrong:** Calling `RemovePackageAsync` on a provisioned package (not installed for any user) may fail or behave unexpectedly.

**Why it happens:** Provisioned packages are staged for future user installs but not currently installed. They require `RemoveProvisionedPackageAsync` or a different removal approach.

**How to avoid:** Check `Package.SignatureKind` or `Package.IsProvisioned` to distinguish. For provisioned packages, use `PackageManager.RemoveProvisionedPackageAsync` or skip them with a message.

**Warning signs:** `RemovePackageAsync` returns an error for a provisioned package.

### Pitfall 7: Async Void in Event Handlers

**What goes wrong:** Using `async void` in XAML event handlers causes unhandled exceptions to crash the process.

**Why it happens:** `async void` exceptions cannot be caught by the caller and propagate to the synchronization context.

**How to avoid:** Use `async void` only for event handlers, and wrap the entire body in try-catch. Better: use `ICommand` / `RelayCommand` which supports `async Task`.

**Warning signs:** Application crashes when an async operation fails in an event handler.

## Code Examples

### WinRT PackageManager Enumeration (C#)

```csharp
// AppxService.cs — enumerating packages
using Windows.Management.Deployment;

public IReadOnlyList<AppxPackage> GetInstalledPackages()
{
    var packageManager = new PackageManager();
    var packages = packageManager.FindPackages();
    
    var result = new List<AppxPackage>();
    foreach (var package in packages)
    {
        result.Add(new AppxPackage(
            package.Id.FullName,
            package.Id.Name,
            package.Id.Publisher,
            package.Id.Version.ToString(),
            package.InstalledLocation.Path,
            false));
    }
    return result;
}

public IReadOnlyList<AppxPackage> GetProvisionedPackages()
{
    var packageManager = new PackageManager();
    var packages = packageManager.FindProvisionedPackages();
    
    var result = new List<AppxPackage>();
    foreach (var package in packages)
    {
        result.Add(new AppxPackage(
            package.Id.FullName,
            package.Id.Name,
            package.Id.Publisher,
            package.Id.Version.ToString(),
            package.InstalledLocation.Path,
            true));
    }
    return result;
}
```

### WinRT Package Removal (C#)

```csharp
public async Task<DeploymentResult> RemovePackageAsync(string packageFullName)
{
    var packageManager = new PackageManager();
    var operation = packageManager.RemovePackageAsync(packageFullName);
    
    // Optionally subscribe to progress
    operation.Progress = (info, progress) =>
    {
        // progress.Percentage, progress.state
    };
    
    var result = await operation.AsTask();
    return result;
}
```

### Registry Uninstall Key Enumeration (C#)

```csharp
// InstalledAppsService.cs — reading uninstall keys
private static readonly (RegistryKey Hive, string Path)[] UninstallKeyLocations =
[
    (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
    (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"),
    (Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
];

public IReadOnlyList<InstalledApp> GetInstalledApps()
{
    var apps = new List<InstalledApp>();
    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var (hive, path) in UninstallKeyLocations)
    {
        using var key = hive.OpenSubKey(path);
        if (key is null) continue;

        foreach (var subKeyName in key.GetSubKeyNames())
        {
            using var subKey = key.OpenSubKey(subKeyName);
            if (subKey is null) continue;

            var displayName = subKey.GetValue("DisplayName") as string;
            if (string.IsNullOrWhiteSpace(displayName)) continue;

            // Deduplicate by display name (same app may appear in multiple hives)
            if (!seen.Add(displayName)) continue;

            apps.Add(new InstalledApp(
                displayName,
                subKey.GetValue("DisplayVersion") as string ?? string.Empty,
                subKey.GetValue("Publisher") as string ?? string.Empty,
                subKey.GetValue("InstallLocation") as string ?? string.Empty,
                subKey.GetValue("UninstallString") as string ?? string.Empty,
                subKey.GetValue("QuietUninstallString") as string ?? string.Empty,
                path));
        }
    }

    return apps;
}
```

### DISM API P/Invoke (C#)

```csharp
// OptionalFeaturesService.cs — DISM P/Invoke
using System.Runtime.InteropServices;

public interface IOptimalFeaturesService
{
    IReadOnlyList<OptionalFeature> GetFeatures();
    Task EnableFeatureAsync(string featureName);
    Task DisableFeatureAsync(string featureName);
}

public sealed record OptionalFeature(string Name, string State);

public sealed partial class OptionalFeaturesService : IOptimalFeaturesService
{
    private readonly ILogger<OptionalFeaturesService> _logger;

    public OptionalFeaturesService(ILogger<OptionalFeaturesService> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<OptionalFeature> GetFeatures()
    {
        DismApi.Initialize(DismLogErrorsWarnings, 0);
        try
        {
            var features = DismApi.GetFeatures(DismPackagePath);
            var result = new List<OptionalFeature>();
            foreach (var feature in features)
            {
                result.Add(new OptionalFeature(
                    feature.FeatureName,
                    feature.State.ToString()));
            }
            return result;
        }
        finally
        {
            DismApi.Shutdown();
        }
    }

    public async Task EnableFeatureAsync(string featureName)
    {
        await Task.Run(() =>
        {
            DismApi.Initialize(DismLogErrorsWarnings, 0);
            try
            {
                DismApi.EnableFeature(DismOnline, featureName, false, false, null, null);
            }
            finally
            {
                DismApi.Shutdown();
            }
        });
    }

    // P/Invoke declarations
    private const uint DismLogErrorsWarnings = 1;
    private const uint DismOnline = 0;

    [LibraryImport("DismApi.dll")]
    private static partial void DismApi.Initialize(uint logLevel, uint logFilePath);

    [LibraryImport("DismApi.dll")]
    private static partial void DismApi.Shutdown();

    [LibraryImport("DismApi.dll")]
    private static partial DismFeatureCollection GetFeatures(uint session);

    [LibraryImport("DismApi.dll")]
    private static partial void EnableFeature(uint session, string featureName, bool enableAll, bool limitAccess, string? sourcePath, string? sourcePathIndex);

    [LibraryImport("DismApi.dll")]
    private static partial void DisableFeature(uint session, string featureName, bool removePayload, bool limitAccess, string? sourcePath, string? sourcePathIndex);
}
```

**Note:** The exact DISM P/Invoke signatures vary. The above is a simplified illustration. The actual implementation should use the `DismApi.dll` COM interop or the `Microsoft.Dism` NuGet package if available. Alternatively, use `Process.Start("dism.exe", "/Online /Enable-Feature /FeatureName:<name>")` as a fallback.

### winget CLI Execution (C#)

```csharp
// StoreService.cs — winget search and install
public interface IStoreService
{
    IReadOnlyList<StoreApp> SearchApps(string query);
    Task<ProcessResult> InstallAppAsync(string appId);
}

public sealed record StoreApp(string Name, string Id, string Version, string Source);

public sealed class StoreService : IStoreService
{
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<StoreService> _logger;

    public StoreService(IProcessRunner processRunner, ILogger<StoreService> logger)
    {
        _processRunner = processRunner;
        _logger = logger;
    }

    public IReadOnlyList<StoreApp> SearchApps(string query)
    {
        var result = _processRunner.RunAsync("winget", $"search \"{query}\" --accept-source-agreements");
        
        if (result.ExitCode != 0)
        {
            _logger.LogWarning("winget search failed: {Error}", result.StdErr);
            return [];
        }

        return ParseSearchResults(result.StdOut);
    }

    public async Task<ProcessResult> InstallAppAsync(string appId)
    {
        return await _processRunner.RunAsync("winget", 
            $"install --id {appId} --exact --accept-source-agreements --accept-package-agreements");
    }

    private static IReadOnlyList<StoreApp> ParseSearchResults(string output)
    {
        var apps = new List<StoreApp>();
        var lines = output.Split('\n');
        
        // Skip header line(s) — winget search outputs a header row
        foreach (var line in lines.Skip(2))
        {
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                apps.Add(new StoreApp(
                    parts[0].Trim(),
                    parts[1].Trim(),
                    parts.Length > 2 ? parts[2].Trim() : string.Empty,
                    parts.Length > 3 ? parts[3].Trim() : string.Empty));
            }
        }
        
        return apps;
    }
}
```

### Direct Process.Start for Uninstall (D-05)

```csharp
// InstalledAppsViewModel.cs — direct Process.Start for uninstall
[RelayCommand(CanExecute = nameof(CanUninstall))]
public async Task UninstallAsync(InstalledApp app)
{
    if (!IsElevated)
    {
        ErrorMessage = "Uninstalling requires administrator privileges.";
        StatusMessage = "Cannot uninstall: not elevated.";
        _infoBar.ShowError("Elevation required", "Run Vain Tools as administrator to uninstall programs.");
        return;
    }

    var confirmed = await _dialogs.ConfirmAsync(
        "Uninstall Program",
        $"Uninstall {app.DisplayName}? This cannot be undone.",
        confirmText: "Uninstall");

    if (!confirmed)
    {
        StatusMessage = "Uninstall cancelled.";
        return;
    }

    try
    {
        var uninstallString = !string.IsNullOrEmpty(app.QuietUninstallString)
            ? app.QuietUninstallString
            : app.UninstallString;

        if (string.IsNullOrEmpty(uninstallString))
        {
            ErrorMessage = "No uninstall command found for this program.";
            StatusMessage = "Cannot uninstall: no uninstall command.";
            _infoBar.ShowError("Uninstall failed", "No uninstall command found.");
            return;
        }

        // D-05: Direct Process.Start — no IProcessRunner
        var startInfo = new ProcessStartInfo
        {
            FileName = uninstallString,
            UseShellExecute = true,
            CreateNoWindow = false,
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            throw new InvalidOperationException("Failed to start uninstall process.");
        }

        await process.WaitForExitAsync();

        StatusMessage = $"Uninstalled {app.DisplayName}";
        _infoBar.ShowSuccess("Uninstall started", $"{app.DisplayName} is being uninstalled.");

        // D-13: Reload the list after uninstall
        await RefreshAsync();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to uninstall {AppName}", app.DisplayName);
        ErrorMessage = ex.Message;
        StatusMessage = $"Could not uninstall: {ex.Message}";
        _infoBar.ShowError("Uninstall failed", ex.Message);
    }
}

private bool CanUninstall(InstalledApp? app) => app is not null && IsElevated;
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| WMI `Win32_Product` for installed programs | Direct registry reads on Uninstall keys | Windows 7+ | WMI is 10x slower and triggers consistency checks; registry is instant |
| PowerShell `Get-WindowsOptionalFeature` | DISM API P/Invoke | Windows 8+ | PowerShell is heavyweight; DISM API is the underlying COM interface |
| `dism.exe` CLI | DISM API COM interop | Windows 8+ | CLI spawns a process; COM interop is in-process and faster |
| `winget` tabular output parsing | `winget --format JSON` | winget 1.4+ (2022) | JSON is machine-parseable; tabular output is fragile |
| `PackageManager.FindPackages()` | `PackageManager.FindPackagesForUser()` | Windows 10 | FindPackagesForUser is more efficient for per-user queries |

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | WinRT `PackageManager` is accessible from .NET 10 WinUI 3 app | Code Examples | Low — WinRT projection is built into .NET |
| A2 | `DismApi.dll` is available on all Windows 10+ systems | Code Examples | Low — ships with Windows |
| A3 | `winget.exe` is available on Windows 10 1809+ with App Installer | Code Examples | Medium — may not be installed on older systems or enterprise images |
| A4 | `IRegistryTweakService.IsElevated` correctly detects admin privileges | Architecture | Low — already used in Phase 3/4/5 |
| A5 | `IDialogService.ConfirmAsync` returns `bool` (true = confirmed) | Code Examples | Low — already used in Phase 5 |
| A6 | `IProcessRunner.RunAsync` is the correct abstraction for winget CLI | Code Examples | Low — already exists from Phase 4 |
| A7 | Registry uninstall keys contain `UninstallString` and `QuietUninstallString` values | Code Examples | Low — standard Windows installer behavior |
| A8 | `Process.Start` with `UseShellExecute = true` can handle uninstall strings with arguments | Code Examples | Low — Windows shell parses the command line |

## Open Questions

1. **DISM API P/Invoke signatures**
   - What we know: DISM API is a COM-based API in `DismApi.dll`. The exact P/Invoke signatures for `DismGetFeatures`, `DismEnableFeature`, and `DismDisableFeature` are not fully documented in the research.
   - What's unclear: The exact parameter types and marshaling for the DISM API functions.
   - Recommendation: Use the `Microsoft.Dism` NuGet package if available, or use `Process.Start("dism.exe", ...)` as a fallback. The DISM API is also accessible via COM interop with `DismSession` and `DismFeature` interfaces.

2. **winget CLI availability**
   - What we know: winget ships with App Installer on Windows 10 1809+ and Windows 11.
   - What's unclear: Whether winget is available on all target systems (enterprise images may not have it).
   - Recommendation: Check for winget availability at startup and show a clear error message if not found. Provide a fallback message directing the user to install App Installer.

3. **Store page listing mechanism**
   - What we know: D-09 says "listing mechanism at researcher's discretion."
   - What's unclear: Whether to use `winget search` with a default query, a curated list, or a category-based approach.
   - Recommendation: Use `winget search` with an empty or default query to list all available apps. This is the most flexible approach and matches user expectations.

4. **Test strategy for WinRT PackageManager**
   - What we know: WinRT APIs cannot be easily mocked in unit tests.
   - What's unclear: How to test the Appx service without actually calling WinRT APIs.
   - Recommendation: Extract the WinRT calls behind an interface and use Moq to mock the interface in tests. Test the ViewModel logic (confirmation, elevation checks, error handling) without testing the actual WinRT calls.

5. **Test strategy for DISM P/Invoke**
   - What we know: P/Invoke calls cannot be mocked.
   - What's unclear: How to test the OptionalFeatures service without actually calling DISM.
   - Recommendation: Same as WinRT — extract behind an interface and mock. Test the ViewModel logic separately.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| winget | Store page | Likely (Windows 10 1809+ with App Installer) | 1.4+ | Show error message; direct user to install App Installer |
| DISM | Optional Features | Yes (ships with Windows 8+) | DismApi.dll | Use `dism.exe` CLI as fallback |
| WinRT PackageManager | Appx Manager | Yes (Windows 10+) | Windows.Management.Deployment | No fallback — required for Appx management |
| Registry Uninstall Keys | Installed Apps | Yes (all Windows) | N/A | No fallback — required for installed programs |

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit (existing project test framework) |
| Config file | `src/VainTools.Tests/VainTools.Tests.csproj` |
| Quick run command | `dotnet test --filter "FullyQualifiedName~AppxServiceTests"` |
| Full suite command | `dotnet test` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| APPX-01 | Enumerate installed and provisioned packages | unit (mock) | `dotnet test --filter "FullyQualifiedName~AppxServiceTests"` | ❌ Wave 0 |
| APPX-02 | Remove package via RemovePackageAsync | unit (mock) | `dotnet test --filter "FullyQualifiedName~AppxServiceTests"` | ❌ Wave 0 |
| INST-01 | Enumerate installed programs from registry | unit (mock) | `dotnet test --filter "FullyQualifiedName~InstalledAppsServiceTests"` | ❌ Wave 0 |
| INST-02 | Execute uninstall via Process.Start | unit (mock) | `dotnet test --filter "FullyQualifiedName~InstalledAppsServiceTests"` | ❌ Wave 0 |
| INST-03 | Copy uninstall command to clipboard | unit | `dotnet test --filter "FullyQualifiedName~InstalledAppsServiceTests"` | ❌ Wave 0 |
| OPT-01 | List optional features via DISM | unit (mock) | `dotnet test --filter "FullyQualifiedName~OptionalFeaturesServiceTests"` | ❌ Wave 0 |
| OPT-02 | Enable/disable features via DISM | unit (mock) | `dotnet test --filter "FullyQualifiedName~OptionalFeaturesServiceTests"` | ❌ Wave 0 |
| STOR-01 | List installable apps via winget search | unit (mock) | `dotnet test --filter "FullyQualifiedName~StoreServiceTests"` | ❌ Wave 0 |
| STOR-02 | Install app via winget install | unit (mock) | `dotnet test --filter "FullyQualifiedName~StoreServiceTests"` | ❌ Wave 0 |

### Wave 0 Gaps

All test files need to be created:
- `src/VainTools.Tests/AppxServiceTests.cs`
- `src/VainTools.Tests/InstalledAppsServiceTests.cs`
- `src/VainTools.Tests/OptionalFeaturesServiceTests.cs`
- `src/VainTools.Tests/StoreServiceTests.cs`

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V5 Input Validation | yes | Validate all user input (search queries, package names, feature names) before passing to APIs |
| V7 Error Handling | yes | Log all exceptions; show user-friendly error messages; never expose stack traces in UI |
| V9 Logging | yes | Log all destructive operations (remove, uninstall, enable/disable) with user context |
| V10 Malicious Input | yes | Sanitize registry values and winget output before display in UI |

## Sources

### Primary (HIGH confidence)
- [PackageManager Class (Windows.Management.Deployment)](https://learn.microsoft.com/en-us/uwp/api/windows.management.deployment.packagemanager) — Microsoft Learn; verified API surface for FindPackages, FindProvisionedPackages, RemovePackageAsync
- [PackageManager.RemovePackageAsync Method](https://learn.microsoft.com/en-us/uwp/api/windows.management.deployment.packagemanager.removepackageasync) — Microsoft Learn; verified removal API and behavior
- [Enable-WindowsOptionalFeature (DISM)](https://learn.microsoft.com/en-us/powershell/module/dism/enable-windowsoptionalfeature) — Microsoft Learn; verified DISM feature enable/disable parameters
- [Disable-WindowsOptionalFeature (DISM)](https://learn.microsoft.com/en-us/powershell/module/dism/disable-windowsoptionalfeature) — Microsoft Learn; verified DISM feature disable parameters
- [winget search command](https://learn.microsoft.com/en-us/windows/package-manager/winget/search) — Microsoft Learn; verified search command syntax and options
- [winget install command](https://learn.microsoft.com/en-us/windows/package-manager/winget/install) — Microsoft Learn; verified install command syntax and options
- [Use WinGet to install and manage applications](https://learn.microsoft.com/en-us/windows/package-manager/winget/) — Microsoft Learn; verified winget availability and requirements

### Secondary (MEDIUM confidence)
- [DISM API (DismApi.dll)](https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/dism-api) — Microsoft Learn; DISM API reference for P/Invoke signatures
- [winrt-api PackageManager docs](https://github.com/MicrosoftDocs/winrt-api/blob/docs/windows.management.deployment/packagemanager.md) — GitHub; additional WinRT API documentation
- [DeepWiki winget-cli](https://deepwiki.com/microsoft/winget-cli) — DeepWiki; winget CLI architecture and output format

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all APIs are documented Microsoft APIs with stable surfaces
- Architecture: HIGH — follows established Phase 5 patterns exactly
- Pitfalls: HIGH — all pitfalls are well-documented Windows API behaviors

**Research date:** 2026-10-04
**Valid until:** 2027-10-04 (Windows API surface is stable; winget CLI may change)

---

## RESEARCH COMPLETE

**Phase:** 06 - Apps
**Confidence:** HIGH

### Key Findings
- WinRT `PackageManager` provides `FindPackages()` and `FindProvisionedPackages()` for enumeration, and `RemovePackageAsync()` for removal — all accessible from .NET 10 WinUI 3
- Registry uninstall keys must be read from 3 hives (HKLM 64-bit, HKLM WOW6432Node, HKCU) to avoid missing 32-bit programs — same pattern as `StartupService.RunKeyLocations`
- DISM API is a COM-based API in `DismApi.dll`; P/Invoke signatures are complex — recommend using `dism.exe` CLI as a simpler fallback or the `Microsoft.Dism` NuGet package
- winget CLI is the standard Windows package manager; `winget search` and `winget install` are the key commands; output parsing should use `--format JSON` if available
- All four pages follow the exact same Phase 5 pattern: service interface + implementation, ViewModel with source generators, custom XAML layout, DI registration

### File Created
C:/Users/isleap/Documents/GitHub/Reversed-app/.planning/phases/06-apps/06-RESEARCH.md

### Confidence Assessment
| Area | Level | Reason |
|------|-------|--------|
| Standard Stack | HIGH | All APIs are documented Microsoft APIs with stable surfaces; winget and DISM ship with Windows |
| Architecture | HIGH | Follows established Phase 5 patterns exactly; all interfaces and base classes already exist |
| Pitfalls | HIGH | All pitfalls are well-documented Windows API behaviors with clear mitigation strategies |

### Open Questions
1. Exact DISM P/Invoke signatures — recommend using `dism.exe` CLI or `Microsoft.Dism` NuGet package as fallback
2. winget CLI availability on target systems — recommend runtime check with clear error message
3. Store page listing mechanism — recommend `winget search` with default query
4. Test strategy for WinRT and DISM — recommend interface extraction + Moq mocking

### Ready for Planning
Research complete. Planner can now create PLAN.md files.
