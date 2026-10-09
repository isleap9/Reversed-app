# Vain Toolbox — Reverse Engineering Findings (GROUND TRUTH)

**Method:** static analysis of the shipped binaries with `pefile` + raw string extraction.
**Supersedes:** the earlier `VAIN-TOOL-REVERSE-ENGINEERING.md`, which described a
*guessed* feature set ("Features Inferred") that does not match the real application.

## Binary inventory

**Current location:** `C:\Users\isleap\Documents\GitHub\Reversed-app\Vain\`
(originally recovered from the shipped installer layout
`C:\Users\isleap\Desktop\Vain\Executables\Files\Program Files\vain\`;
the binaries are local reference material only — `*.exe` is gitignored)

| File | Size | Role |
|---|---|---|
| `Vain Toolbox.exe` | 11.7 MB | Main app — **native C++/WinRT WinUI 3, NOT .NET** |
| `Vain Tools.exe` | 483 KB | Tray/background helper (registry + named-event driven) |
| `Vain Governor UI.exe` | 2.1 MB | Separate governor window app (`ThreadWindow.xaml`) |
| `VainShell.dll` | 70 KB | Shell integration |
| `Vain Toolbox.pri` | 1.4 MB | Resource index — every UI string, all languages |

Evidence it is native, not managed: no CLR data directory, zero occurrences of
`BSJB` / `mscorlib` / `.NETCoreApp` / `System.Runtime`. 725 hits for
`Microsoft.UI.Xaml`. C++ mangled names present:
`.?AUGpuPage@factory_implementation@vaintoolboxc__@winrt@@`.
Source paths leak the author's machine: `C:\Users\flash\Desktop\vaintools\vaintoolbox\`.

## Real page structure (30 pages)

Extracted from `ms-appx:///` XAML references. **The nav order below is the order the
page list appears in the binary.**

```
Home                          Features/Home/HomePage.xaml
Vain Tools                    Features/VainTools/VainToolsPage.xaml
General                       Features/General/GeneralPage.xaml
  ├─ Explorer                 Features/General/ExplorerPage.xaml
  ├─ Context Menu             Features/General/ContextMenuPage.xaml
  ├─ Visual                   Features/General/VisualPage.xaml
  ├─ Date & Time              Features/General/DateTimePage.xaml
  └─ Settings Visibility      Features/General/SettingsVisibilityPage.xaml
System                        Features/System/SystemPage.xaml
Security                      Features/Security/SecurityPage.xaml
Experimental                  Features/Experimental/ExperimentalPage.xaml
Performance                   Features/Performance/PerformancePage.xaml
Apps
  ├─ Appx Manager             Features/AppxManager/AppxManagerPage.xaml
  ├─ Installed Apps           Features/InstalledApps/InstalledAppsPage.xaml
  ├─ Optional Features        Features/OptionalFeatures/OptionalFeaturesPage.xaml
  └─ Store                    Features/Store/StorePage.xaml
Sound                         Features/Sound/SoundPage.xaml
Affinity                      Features/Affinity/AffinityPage.xaml
Startup                       Features/Startup/StartupPage.xaml
Power Editor                  Features/Powereditor/PowereditorPage.xaml
GPU                           Features/Gpu/GpuPage.xaml
  ├─ Display                  Features/Gpu/Display/DisplayPage.xaml
  └─ NVIDIA / DRS             Features/Gpu/Nvidia/Drs/DrsPage.xaml
Network                       Features/Network/NetworkPage.xaml
Tools
  ├─ Device Cleaner           Features/DeviceCleaner/DeviceCleanerPage.xaml
  ├─ Drive Scanner            Features/DriveScanner/DriveScannerPage.xaml
  └─ Driver Manager           Features/DriverManager/DriverManagerPage.xaml
About                         Features/About/AboutPage.xaml
```

## What each area actually does

### GPU → NVIDIA / DRS (the flagship feature)
A full **NVIDIA Driver Settings (DRS) editor over `nvapi64.dll`**, not a
fan/clock/power tool. Capabilities recovered from strings:

- Loads **all** driver settings (`Loaded %zu settings`) and shows driver default vs current
- Per-application (per-game) profiles + a global profile
- **Staged changes** model: edit → `Apply %zu staged change(s)` → verify by readback
- Value editors by type: `Dword`, `Binary`, `AnsiString`, `Bitmask`, `Custom Value`
  ("type a hex/decimal value", "enable bitmask mode to combine several flags")
- Search across profiles ("Search profiles...")
- **Restore ALL driver settings to NVIDIA defaults** (explicitly irreversible)
- Export/import **`.vain`** profile files (XML `ArrayOfProfile` with
  `Executeables` / `Settings` / `ProfileSetting` / `SettingID` / `SettingValue`)
- Failure handling: "NVIDIA driver settings are unavailable (nvapi64.dll / DRS session failed)"

Settings exposed include (sample of hundreds): `Low Latency Mode`, `Ultra Low Latency -
Enabled`, `DWM Low Latency`, `Power Management - Mode`, `Threaded Optimization`,
`Texture Filtering - Trilinear Optimization`, `Anisotropic Filter - Optimization`,
`DLSS - Enable DLL Override`, `DLSS 3.1.11+ - Forced scaling ratio`,
`DLSS - Forced Model Preset Profile`, `CUDA - Stable Performance Limit`,
`D3D12 Tiled Resources Batch Update VA Fences`, `Refresh Rate - Override`,
`VRR Override Control`, `G-SYNC Enable`, `HDR` overrides.

### GPU (the GPU page itself)
Both monitoring **and** tuning are present — an earlier revision of this document
wrongly claimed the app does not poll sensors. It does:

- **NVIDIA**: `nvml.dll` (`C:\Program Files\NVIDIA Corporation\NVSMI\nvml.dll`),
  `nvmlDeviceGetMemoryInfo`, `nvmlDeviceGetGpuMaxPcieLinkGeneration`
- **AMD**: ADLX (`IADLXGPU1`, `IADLXGPUMetrics1`, `IADLXGPUTuningServices1`,
  `IADLXManualVRAMTuning1/2`), `RadeonSoftware.exe`
- Reads: `GPU Temperature`, `GPU Usage`, `GPU Voltage`, `Memory Clock (MHz)`,
  `VRAM Clock`, `VRAM Type / Bandwidth`, `VRAM Usage`, `Memory Temperature`
- Writes: `gpuOffset`, `gpuVoltage`, `nvFanRamp`, `gpuMinFreqMHz`/`gpuMaxFreqMHz`,
  `gpuScaling`, `smartAccessMemory`, `variableGraphicsMemoryIndex`, VRAM tuning
- Toggles: `Hardware Accelerated GPU Scheduling (HAGS)`, GPU scaling, Smart Access
  Memory, Variable Graphics Memory, per-app `HKCU\Software\Microsoft\DirectX\UserGpuPreferences`
- **GPU restart in place**: "Restart GPU", "Restarting GPU…", "Removing GPU device(s)
  and audio bus", "The GPU driver has been successfully restarted."
- Failure states: `No GPU Detected`, `Unknown GPU`, `Profile does not match this GPU`,
  `Invalid NVIDIA profile parameters`, "Temperature target is not supported by this
  GPU / driver", "Classic voltage boost is not supported by this GPU / driver"

### Home page
- WMI queries: `SELECT Name FROM Win32_Processor`,
  `SELECT Manufacturer FROM Win32_BaseBoard`
- Fields: `Manufacturer`, `Model`, `Driver Version`, `Unknown CPU/GPU/Motherboard/RAM`,
  `Select driver version...`
- `Windows Insider Program` toggle, `GPU Performance Counters for All Users`

### Vain Tools page
- `Restore Vain Defaults` — confirmation text: "The following settings will be changed
  to Vain defaults:", covering **Sound, Security, Performance** sections
- `Import .vain profile`, "Drop a .vain profile exported from Vain Toolbox."
- Uses `taskkill.exe /f /im explorer.exe` + relaunch `explorer.exe` to apply shell tweaks

### GPU → Display
**EDID override and custom resolution editor** — genuinely low-level:
- Enumerate monitors from the registry (`*` marks a saved EDID override)
- Custom resolutions: pixel clock, H/V active/blanking, front porch, sync width,
  sync polarity, interlaced, preferred/native flag, DisplayID extension blocks
- Refresh-range descriptor for VRR / FreeSync
- HDMI audio data block injection
- **Restart display driver** in-place (disable/re-enable adapters) instead of reboot,
  with a documented "if the screen stays black, use Restore Default / safe mode" escape
- Requires administrator; writes `EDID_OVERRIDE` to the registry

### Vain Tools (app's own settings)
- `Import .vain profile` / drag-and-drop `.vain` files
- **Restore Vain Defaults** — resets a listed set of settings to the author's presets
- Branding: `https://vain.zone`, "Visit vain.zone for more tools",
  "This application is designed for vain only."

### Tools
- **Device Cleaner** — "processes, services, devices, driver packages, registry keys
  and folders"
- **Drive Scanner**
- **Driver Manager** — `pnputil`-style: `/enum-devices /drivers`, `/add-driver`,
  `/delete-driver /force`, `/export-driver`, "N driver(s) loaded",
  "%d drivers failed to delete. Would you like to force delete them?",
  NVIDIA and AMD (`atiadlxy.dll` / `atiadlxx.dll`) handling, "Uninstall GPU Driver"

### Apps
- **Appx Manager** — enumerate/remove provisioned packages
- **Installed Apps** — reads `SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall`,
  uses `UninstallString` / `QuietUninstallString`, "Copy uninstall command"
- **Optional Features** — Windows optional feature enable/disable
- **Store** — winget-style app installs

### System tweaks (real ones found)
- `DisableAutoplay`, `AutorunsDisabled`, `NoDriveTypeAutoRun`, `DisableStartupSound`,
  `ShowSyncProviderNotifications`, `EnableTransparency`, `VulnerableDriverBlocklistEnable`,
  `EnableVirtualizationBasedSecurity`, `AllowGameDVR`, `AutoGameModeEnabled`,
  `AllowAutoGameMode`, `UseNexusForGameBarEnabled`, `AppCaptureEnabled`,
  Explorer `Advanced` keys, `HKLM\SOFTWARE\Policies\Microsoft\Windows\Explorer`
- Privacy toggles keyed `privacy-*` (`privacy-activityhistory`,
  `privacy-appdiagnostics`, `privacy-backgroundapps`, `privacy-broadfilesystemaccess`,
  `privacy-calendar`, `privacy-accountinfo`, `privacy-automaticfiledownloads`)
- **Affinity** — CPU affinity / ideal processor sets ("Adaptive ideal processor sets")
- **Performance** — timer resolution ("Black Flag Timer Resolution", `SkipTickOverride`),
  MPO, GPU scheduling, "Use Platform Tick", "Timer Expiration", working-set adjustment
- **Power Editor** — power plan / powercfg-level editing
- **Network** — DNS + NTP servers, `Get-NetAdapterBinding`, NIC offloads
  (`*TCPChecksumOffloadIPv4`, `*QoSOffload`, `*PMWiFiRekeyOffload`), MSI/interrupt moderation
- **Security** — Defender, UAC, VBS, Memory Integrity, Vulnerable Driver Blocklist,
  Spectre/Meltdown (`FeatureSettingsOverride`, `FeatureSettingsOverrideMask`)
- **Sound** — volume mixer, spatial audio, audio enhancements

## Inter-process architecture

Registry roots:
```
HKCU\Software\VainTools\{Misc, Screenshots, Taskbar}
HKCU\Software\VainGovernor\Profiles
HKCU\Software\VainToolbox
HKCU\Software\VainToolbox\Drivers
HKCU\Software\VainTools\Gpu
```
Named events (the three EXEs coordinate through these):
```
Local\VainToolboxInstance      Local\VainToolsInstance
Local\VainGpuProfilesChanged   Local\VainToolsLatencyChanged
Local\VainToolsScreenshotChanged  Local\VainToolsTaskbarChanged
Local\VainShellReady           Local\VainGovernorForeground
Local\VainGovernorRulesChanged Local\VainGovernorUiInstance
```
So: **Screenshots and Taskbar do exist, but as small features of the tray helper
(`Vain Tools.exe`, `HKCU\Software\VainTools\{Screenshots,Taskbar}`), not as
top-level pages.** The governor is a separate windowed app with its own rules engine.

## What the current Reversed-app has wrong

The project's six pages — `Dashboard`, `GpuGovernor`, `Profiles`, `SystemTweaks`,
`Screenshots`, `Taskbar` — **do not exist in Vain Toolbox**. They came from the
guessed research doc. Roughly:

| Current page | Reality |
|---|---|
| Dashboard | Real equivalent is **Home** |
| GpuGovernor (temp/fan/clock/power via NVML) | Vain's GPU area is **NVIDIA DRS settings + EDID/display**, not NVML monitoring |
| Profiles | Real equivalent is **DRS profiles + `.vain` export/import** |
| SystemTweaks | Real equivalent is ~6 separate pages (System, Security, Performance, Power Editor, Network, Sound, Experimental) |
| Screenshots | Real feature lives in the tray helper, not a page |
| Taskbar | Real feature lives in the tray helper, not a page |

Missing entirely: General/Explorer, Context Menu, Visual, Date & Time, Settings
Visibility, Apps/Appx Manager, Installed Apps, Optional Features, Store, Affinity,
Startup, Tools/Device Cleaner, Drive Scanner, Driver Manager, About.

## Recommended approach

`Vain Toolbox.exe` is **native C++/WinRT**, so it cannot be decompiled to C# the way a
managed assembly could. Options, in order of fidelity:

1. **Rebuild the real page structure in WinUI 3 / C#** and implement each feature
   against the documented Windows APIs (registry, `nvapi64.dll` P/Invoke, `pnputil`,
   `Get-NetAdapterBinding`, EDID registry). This is what the existing project shape
   supports — the pages just need to be the *right* pages.
2. **Interop against the shipped pieces** — `VainShell.dll` and the named events are a
   documented contract; a replacement shell could drive them.
3. **Behavioural capture** — run `Vain Toolbox.exe` and diff registry/`nvapi` state
   before/after each toggle to recover exact semantics.

Recommended: **option 1, starting with Home + Vain Tools + GPU/NVIDIA DRS**, because
DRS is the flagship feature and its data model (`.vain` XML) is already fully known.
