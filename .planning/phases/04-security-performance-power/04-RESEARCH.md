# Phase 4: Security, Performance & Power — Research

**Date:** 2026-10-03
**Author:** gsd-phase-researcher
**Status:** Complete

---

## 1. Security Registry Keys

All Security tweaks are registry-based. The existing `RegistryTweakService` + `TweakCatalog` pattern handles them directly — no new service needed.

### 1.1 Toggle Definitions

| ID | Name | Registry Hive | Key Path | Value Name | Enabled Value | Disabled Value | Default | Requires Admin | Explorer Restart |
|----|------|---------------|----------|------------|---------------|----------------|---------|----------------|-----------------|
| `security-defender-disable` | Disable Windows Defender | HKLM | `SOFTWARE\Policies\Microsoft\Windows Defender` | `DisableAntiSpyware` | 1 | 0 | 0 (off) | Yes | No |
| `security-defender-tamper` | Disable Tamper Protection | HKLM | `SOFTWARE\Microsoft\Windows Defender\Features` | `TamperProtection` | 0 | 5 | 5 (on) | Yes | No |
| `security-vbs` | Enable Virtualization-Based Security | HKLM | `SYSTEM\CurrentControlSet\Control\DeviceGuard` | `EnableVirtualizationBasedSecurity` | 1 | 0 | 0 (off) | Yes | No |
| `security-memory-integrity` | Enable Memory Integrity | HKLM | `SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity` | `Enabled` | 1 | 0 | 0 (off) | Yes | No |
| `security-vulnerable-driver-blocklist` | Enable Vulnerable Driver Blocklist | HKLM | `SYSTEM\CurrentControlSet\Control\CI\Config` | `VulnerableDriverBlocklistEnable` | 1 | 0 | 0 (off) | Yes | No |
| `security-spectre-meltdown` | Spectre & Meltdown Mitigations | HKLM | `SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management` | `FeatureSettingsOverride` | 0 | 3 | 0 (off) | Yes | No |
| `security-uac` | Disable User Account Control | HKLM | `SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System` | `EnableLUA` | 0 | 1 | 1 (on) | Yes | No |
| `security-smartscreen` | Disable SmartScreen | HKLM | `SOFTWARE\Policies\Microsoft\Windows\System` | `EnableSmartScreen` | 0 | 1 | 1 (on) | Yes | No |

### 1.2 Notes

- **Tamper Protection** is inverted: `5` = enabled (on), `0` = disabled (off). The "Disable Tamper Protection" toggle writes `0` to disable it. This is a DWORD value.
- **Spectre/Meltdown** uses `FeatureSettingsOverride` (DWORD). Value `3` = mitigations enabled (vulnerable), `0` = mitigations disabled (protected). The companion value `FeatureSettingsOverrideMask` (DWORD, default `3`) controls which bits are active. For a simple toggle, only `FeatureSettingsOverride` is needed.
- **UAC** (`EnableLUA`): `1` = UAC enabled (default), `0` = UAC disabled. The "Disable UAC" toggle writes `0`.
- **SmartScreen** (`EnableSmartScreen`): `1` = enabled (default), `0` = disabled. The "Disable SmartScreen" toggle writes `0`.
- All Security tweaks write to HKLM → all require admin elevation.
- None require Explorer restart (they are system-level, not shell-level).

### 1.3 Ground Truth Confirmation

From `VAIN-TOOLBOX-GROUND-TRUTH.md`:
- `VulnerableDriverBlocklistEnable` — confirmed in System tweaks list
- `EnableVirtualizationBasedSecurity` — confirmed in System tweaks list
- `FeatureSettingsOverride`, `FeatureSettingsOverrideMask` — confirmed in Security section
- Defender, UAC, VBS, Memory Integrity — confirmed in Security section

---

## 2. Performance Registry Keys

All Performance tweaks are registry-based. Same pattern as Security.

### 2.1 Toggle Definitions

| ID | Name | Registry Hive | Key Path | Value Name | Enabled Value | Disabled Value | Default | Requires Admin | Explorer Restart |
|----|------|---------------|----------|------------|---------------|----------------|---------|----------------|-----------------|
| `perf-skiptick` | Skip Tick Override | HKLM | `SYSTEM\CurrentControlSet\Control\Session Manager\kernel` | `SkipTickOverride` | 1 | 0 | 0 (off) | Yes | No |
| `perf-platform-tick` | Use Platform Tick | HKLM | `SYSTEM\CurrentControlSet\Control\Session Manager\kernel` | `UsePlatformTick` | 1 | 0 | 0 (off) | Yes | No |
| `perf-timer-expiration` | Timer Expiration | HKLM | `SYSTEM\CurrentControlSet\Control\Session Manager\kernel` | `TimerExpiration` | 1 | 0 | 0 (default) | Yes | No |
| `perf-mpo` | Multiplane Overlay (MPO) | HKLM | `SOFTWARE\Microsoft\Windows\Dwm` | `OverlayTestMode` | 1 | 0 | 0 (off) | Yes | Yes |
| `perf-gpu-scheduling` | Hardware-Accelerated GPU Scheduling | HKLM | `SYSTEM\CurrentControlSet\Control\GraphicsDrivers` | `HwSchMode` | 2 | 1 | 2 (on) | Yes | No |
| `perf-working-set` | Working Set Adjustment | HKLM | `SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management` | `LargeSystemCache` | 1 | 0 | 0 (off) | Yes | No |
| `perf-game-mode` | Game Mode | HKCU | `Software\Microsoft\GameBar` | `AllowAutoGameMode` | 1 | 0 | 1 (on) | No | No |
| `perf-game-dvr` | Game DVR | HKCU | `System\GameConfigStore` | `GameDVR_Enabled` | 1 | 0 | 1 (on) | No | No |

### 2.2 Notes

- **GPU Scheduling** (`HwSchMode`): `2` = enabled (on), `1` = disabled (off). This is inverted from the typical 0/1 pattern — the "enabled" state is `2`, not `1`. The toggle must handle this correctly.
- **MPO** (`OverlayTestMode`): Requires Explorer restart because it changes DWM composition behavior. This is the only Performance tweak with `RequiresExplorerRestart = true`.
- **Game Mode** and **Game DVR** write to HKCU → no admin required. These are the only Performance tweaks that don't need elevation.
- **Skip Tick Override**, **Use Platform Tick**, **Timer Expiration** all live under the same `kernel` key — they are related timer resolution settings.
- **Working Set** (`LargeSystemCache`): `1` = large system cache (favors system performance over app responsiveness), `0` = default.

### 2.3 Ground Truth Confirmation

From `VAIN-TOOLBOX-GROUND-TRUTH.md`:
- `SkipTickOverride` — confirmed ("Black Flag Timer Resolution")
- MPO, GPU scheduling, "Use Platform Tick", "Timer Expiration", working-set adjustment — confirmed in Performance section
- `AllowAutoGameMode` — confirmed in System tweaks list
- `AllowGameDVR`, `AutoGameModeEnabled`, `UseNexusForGameBarEnabled`, `AppCaptureEnabled` — confirmed in System tweaks list

---

## 3. Power Plan Editing via powercfg.exe

### 3.1 Command Reference

| Operation | Command | Description |
|-----------|---------|-------------|
| List plans | `powercfg /list` | Enumerates all power plans with GUIDs |
| Query settings | `powercfg /query <plan_guid>` | Dumps all settings for a plan |
| Set AC value | `powercfg /setacvalueindex <plan_guid> <sub_guid> <setting_guid> <value>` | Sets a setting for AC power |
| Set DC value | `powercfg /setdcvalueindex <plan_guid> <sub_guid> <setting_guid> <value>` | Sets a setting for DC power |
| Apply plan | `powercfg /setactive <plan_guid>` | Activates a power plan |
| Restore defaults | `powercfg /restoredefaultschemes` | Restores all plans to factory defaults |
| Get active plan | `powercfg /getactivescheme` | Returns the currently active plan GUID |

### 3.2 Power Plan GUIDs

| Plan | GUID |
|------|------|
| Balanced | `381b4222-f694-41f0-9685-ff5bb260df2e` |
| High Performance | `8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c` |
| Power Saver | `a1841308-3541-4fab-bc81-f71556f20b4a` |
| Ultimate Performance | `e9a42b02-d5df-448d-aa00-03f14749eb61` |

### 3.3 Setting Category GUIDs

| Category | Sub-GUID |
|----------|----------|
| Processor Power Management | `54533251-82be-4824-96c1-47b60b740d00` |
| Display | `7516b95f-f776-4464-8c53-06167f40cc99` |
| Sleep | `238c9fa8-0aad-41ed-83f4-97be242c8f20` |
| Power Buttons and Lid | `4f971e89-eebd-4455-a8de-9e59040e7347` |
| Hard Disk | `0012ee47-9041-4b5d-9b77-535fba8b1442` |
| Desktop Background Settings | `0d7dbae2-4294-402a-ba8e-26777e8488cd` |
| Wireless Adapter Settings | `19cbb8fa-5279-450e-9fac-8a3d5fedd0c1` |
| USB Settings | `2a737441-1930-4402-8d77-b2bebba308a3` |
| Intel Graphics Settings | `44f3beca-a7c0-460e-9df2-bb8b99e0cba6` |
| PCI Express | `501a4d13-42af-4429-9fd1-a8218c268c20` |

### 3.4 Common Setting GUIDs

#### Processor Power Management (`54533251-82be-4824-96c1-47b60b740d00`)

| Setting | GUID | Values |
|---------|------|--------|
| Minimum processor state | `893dee8e-2bef-41e0-89c6-b55d0929964c` | 0-100 (percentage) |
| Maximum processor state | `bc5038f7-23e0-4960-96da-33abaf5935ec` | 0-100 (percentage) |
| Processor performance boost mode | `be337238-0d82-4146-a960-4f3749d470c7` | 0=Disabled, 1=Enabled, 2=Aggressive, 3=Enabled (Efficient), 4=Aggressive (Efficient) |
| Processor performance core parking min cores | `0cc5b647-c1df-4637-891a-dec35c318583` | 0-100 (percentage) |

#### Display (`7516b95f-f776-4464-8c53-06167f40cc99`)

| Setting | GUID | Values |
|---------|------|--------|
| Turn off display after | `3c0bc021-c8a8-4e07-a973-6b14cbcb2b7e` | 0=Never, otherwise seconds |
| Display brightness | `aded5e82-b909-4619-9949-f5d71dac0bcb` | 0-100 (percentage) |
| Dimmed display brightness | `f1fbfde2-a960-4165-9f88-50667911ce96` | 0-100 (percentage) |

#### Hard Disk (`0012ee47-9041-4b5d-9b77-535fba8b1442`)

| Setting | GUID | Values |
|---------|------|--------|
| Turn off hard disk after | `6738e2c4-e8a5-4a42-b16a-e040e769756e` | 0=Never, otherwise seconds |

#### Sleep (`238c9fa8-0aad-41ed-83f4-97be242c8f20`)

| Setting | GUID | Values |
|---------|------|--------|
| Sleep after | `29f6c1db-86da-48c5-9fdb-f2b67b1f44da` | 0=Never, otherwise seconds |
| Hibernate after | `9d7815a6-7ee4-497e-8888-515a05f02364` | 0=Never, otherwise seconds |
| Allow hybrid sleep | `94ac6d29-73ce-41a6-809f-6363ba21b47e` | 0=Off, 1=On |
| Allow wake timers | `bd3b718a-0680-4d9d-8ab2-e1d2b4ac806d` | 0=Disabled, 1=Enabled, 2=Important Wake Timers Only |

#### Power Buttons and Lid (`4f971e89-eebd-4455-a8de-9e59040e7347`)

| Setting | GUID | Values |
|---------|------|--------|
| Power button action | `7648efa3-dd9c-4e3e-b566-50f929386280` | 0=Do nothing, 1=Sleep, 2=Hibernate, 3=Shut down |
| Sleep button action | `96996bc0-ad50-47ec-923b-6f51874dd9eb` | 0=Do nothing, 1=Sleep, 2=Hibernate, 3=Shut down |
| Lid close action | `5ca83367-6e45-459f-a27b-476b1d01c936` | 0=Do nothing, 1=Sleep, 2=Hibernate, 3=Shut down |
| Start menu power button | `a70643a2-9103-481b-9882-72768ff7b5b1` | 0=Do nothing, 1=Sleep, 2=Hibernate, 3=Shut down |

### 3.5 powercfg /query Output Format

```
Power Scheme GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (Balanced)
  Subgroup GUID: 54533251-82be-4824-96c1-47b60b740d00  (Processor power management)
    Power Setting GUID: 893dee8e-2bef-41e0-89c6-b55d0929964c  (Minimum processor state)
      Possible Setting Index: 0x00000000
      Possible Setting Index: 0x00000064
      Current AC Power Setting Index: 0x00000005
      Current DC Power Setting Index: 0x00000005
```

The parser must extract:
- Power Scheme GUID (from the top-level line)
- Subgroup GUID and name
- Setting GUID and name
- Current AC Power Setting Index (hex value)
- Current DC Power Setting Index (hex value)
- Possible Setting Index values (for valid range)

### 3.6 PowerService Design

```csharp
public interface IPowerService
{
    bool IsElevated { get; }
    Task<IReadOnlyList<PowerPlan>> GetPlansAsync();
    Task<IReadOnlyList<PowerSetting>> GetSettingsAsync(Guid planGuid);
    Task ApplySettingAsync(Guid planGuid, string settingGuid, string value);
    Task RevertPlanAsync(Guid planGuid);
}

public record PowerPlan(Guid Guid, string Name, bool IsActive);

public record PowerSetting(
    string Guid,
    string Name,
    string CategoryGuid,
    string CategoryName,
    string CurrentAcValue,
    string CurrentDcValue,
    IReadOnlyList<string> PossibleValues);
```

### 3.7 PowerService Implementation Notes

- Use `Process.Start` with `ProcessStartInfo` to run `powercfg.exe`
- Redirect stdout/stderr, capture exit code
- Parse `/list` output: lines matching `Power Scheme GUID: ([a-f0-9-]+)\s*\((.+)\)`
- Parse `/query` output: multi-line format with subgroup/setting hierarchy
- `ApplySettingAsync` runs both `/setacvalueindex` and `/setdcvalueindex` for the given value
- `RevertPlanAsync` runs `/restoredefaultschemes` (restores ALL plans — this is a system-wide operation)
- All powercfg operations require admin elevation
- `IsElevated` check: same pattern as `RegistryTweakService` — `WindowsIdentity.GetCurrent().Owner` is in `BuiltInAdministrators` group

### 3.8 Risks & Gotchas

- **`/restoredefaultschemes` is system-wide**: It resets ALL power plans to factory defaults, not just the selected one. The UI must warn the user clearly.
- **powercfg.exe output is locale-dependent**: The parser must handle English output. On non-English systems, the output format may differ. Consider using `powercfg /query` with the `/XML` flag if available, or document the limitation.
- **GUIDs are stable**: The setting GUIDs above are fixed by Microsoft and do not change between Windows versions.
- **Some settings are hidden**: `powercfg /query` only shows visible settings. To show all settings, use `powercfg /query <plan_guid> <sub_guid>` with the specific subgroup, or use `powercfg /qh` (query hidden).
- **Value types vary**: Some settings are percentages (0-100), some are seconds (0=Never), some are enum indices (0, 1, 2, 3). The UI must handle each type appropriately.
- **AC vs DC**: Most settings have separate AC (plugged in) and DC (battery) values. The UI should show both or let the user choose which to edit.

---

## 4. Existing Code Patterns to Reuse

### 4.1 RegistryTweakService

**File:** `src/VainTools.App/Services/RegistryTweakService.cs`

Key methods:
- `Read(RegistryTweak tweak)` → `TweakState` — reads current state from registry
- `ApplyAsync(RegistryTweak tweak)` — writes enabled value
- `RevertAsync(RegistryTweak tweak)` — writes disabled value
- `SetAsync(RegistryTweak tweak, TweakState state)` — sets to specific state
- `IsElevated` — checks if running as administrator
- `RestartExplorerAsync()` — taskkill + relaunch explorer.exe

Key patterns:
- 64-bit registry view first, then 32-bit fallback
- `RemoveKeyWhenDisabled` for key-presence switches
- `RequiresAdmin` check before HKLM writes
- `RegistryValueKind` support for DWORD, String, QWord, Binary

### 4.2 TweakCatalog

**File:** `src/VainTools.App/Services/TweakCatalog.cs`

Key pattern:
- Static class with `All` property returning `IReadOnlyList<RegistryTweak>`
- `Find(string id)` method for lookup by ID
- Each tweak is a `RegistryTweak` record with: Id, Name, Description, Hive, KeyPath, ValueName, EnabledValue, DisabledValue, DefaultWhenUnset, ValueKind, RequiresAdmin, RequiresExplorerRestart, RemoveKeyWhenDisabled

### 4.3 TweakList Control

**File:** `src/VainTools.App/Controls/TweakList.xaml`

- UserControl with `ViewModel` property of type `TweakPageViewModel`
- Renders: Title, Subtitle, ElevationBar (InfoBar), TweaksHost (ItemsControl), RestartExplorerButton, StatusMessage
- Binds to `TweakPageViewModel` for all data

### 4.4 TweakPageViewModel

**File:** `src/VainTools.App/ViewModels/TweakPageViewModel.cs`

- Takes `title`, `subtitle`, and `IEnumerable<RegistryTweak>` in constructor
- Creates `TweakToggleViewModel` for each tweak
- Handles: Load, Toggle, Revert, Refresh, RestartExplorer
- Reports: StatusMessage, IsElevated, HasAdminTweaks, HasExplorerTweaks

### 4.5 TweakPageViewModelFactory

**File:** `src/VainTools.App/ViewModels/TweakPageViewModelFactory.cs`

- DI-friendly factory: `Create(string title, string subtitle, IEnumerable<RegistryTweak> tweaks)`
- Registered in `App.xaml.cs` DI container

### 4.6 Page Implementation Pattern

Each page (e.g., `GeneralPage.xaml.cs`):
```csharp
public GeneralPage()
{
    InitializeComponent();
    List.ViewModel = App.Services.GetRequiredService<TweakPageViewModelFactory>()
        .Create("General", TweakCatalog.All);
}
```

For Security and Performance, the pattern is identical — just pass the appropriate tweak subset:
```csharp
// SecurityPage.xaml.cs
List.ViewModel = App.Services.GetRequiredService<TweakPageViewModelFactory>()
    .Create("Security", TweakCatalog.All.Where(t => t.Id.StartsWith("security-")));

// PerformancePage.xaml.cs
List.ViewModel = App.Services.GetRequiredService<TweakPageViewModelFactory>()
    .Create("Performance", TweakCatalog.All.Where(t => t.Id.StartsWith("perf-")));
```

### 4.7 DI Registration

**File:** `src/VainTools.App/App.xaml.cs`

Services are registered in `ConfigureServices()`:
- `IRegistryTweakService` → `RegistryTweakService` (singleton)
- `TweakPageViewModelFactory` (singleton)
- `IDialogService`, `IInfoBarService` (singleton)

New registrations needed:
- `IPowerService` → `PowerService` (singleton)
- `PowerEditorViewModel` (transient or scoped)

---

## 5. Test Strategy

### 5.1 RegistryTweakService Tests (Existing)

**File:** `src/VainTools.Tests/RegistryTweakServiceTests.cs`

Already covers:
- Read when value absent → Unset
- Apply then read → Enabled
- Revert then read → Disabled
- Apply writes expected raw value
- Apply creates key when missing
- SetAsync with Unset/Unknown → throws
- Apply without admin for admin tweak → throws
- String value kind stored as string
- RemoveKeyWhenDisabled deletes key
- Key presence switch reads correctly
- Value present but unexpected → Disabled
- Catalog IDs are unique
- Catalog every tweak has key path and name
- Catalog classic context menu removes key when disabled
- Catalog only tweaks that need admin are marked so
- Catalog reads every tweak without throwing

### 5.2 New Tests Needed for Phase 4

#### TweakCatalog Tests

```csharp
[Fact] Catalog_SecurityTweaks_HaveCorrectIds()
[Fact] Catalog_PerformanceTweaks_HaveCorrectIds()
[Fact] Catalog_SecurityTweaks_AllRequireAdmin()
[Fact] Catalog_PerformanceTweaks_HkcuOnesDoNotRequireAdmin()
[Fact] Catalog_PerformanceTweaks_MpoRequiresExplorerRestart()
[Fact] Catalog_GpuScheduling_EnabledValueIsTwo()
[Fact] Catalog_TamperProtection_InvertedValues()
```

#### PowerService Tests (Mocking powercfg.exe)

Since `PowerService` wraps `powercfg.exe` via `Process.Start`, testing requires either:
1. **Interface mocking**: Mock `IPowerService` in `PowerEditorViewModel` tests
2. **Process abstraction**: Extract `IProcessRunner` interface for testability

Recommended approach: Extract `IProcessRunner` interface:

```csharp
public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(string fileName, string arguments);
}

public record ProcessResult(int ExitCode, string StdOut, string StdErr);
```

Then `PowerService` takes `IProcessRunner` in constructor. Tests mock `IProcessRunner` to return canned powercfg output.

```csharp
[Fact] GetPlansAsync_ParsesPowerCfgListOutput()
[Fact] GetPlansAsync_EmptyOutput_ReturnsEmptyList()
[Fact] GetSettingsAsync_ParsesPowerCfgQueryOutput()
[Fact] GetSettingsAsync_EmptyOutput_ReturnsEmptyList()
[Fact] ApplySettingAsync_CallsSetAcAndDcValueIndex()
[Fact] RevertPlanAsync_CallsRestoreDefaultSchemes()
[Fact] IsElevated_WhenNotAdmin_ReturnsFalse()
[Fact] GetPlansAsync_ProcessFails_Throws()
```

#### PowerEditorViewModel Tests

```csharp
[Fact] LoadPlansAsync_PopulatesPlansCollection()
[Fact] LoadPlansAsync_SetsActivePlan()
[Fact] LoadSettingsAsync_PopulatesSettingsCollection()
[Fact] ApplyChangesAsync_CallsPowerServiceForAllSettings()
[Fact] RevertChangesAsync_CallsRevertPlan()
[Fact] RefreshAsync_ReloadsPlansAndSettings()
[Fact] NotElevated_DisablesApplyButton()
[Fact] IsBusy_DuringApply_DisablesButtons()
```

### 5.3 Test Project Setup

**File:** `src/VainTools.Tests/VainTools.Tests.csproj`

Already references:
- `VainTools.Framework` project
- `VainTools.App` project
- `Moq` package
- `xunit` package
- `Microsoft.Extensions.DependencyInjection`
- `Microsoft.Extensions.Logging.Abstractions`

No new packages needed for Phase 4 tests.

---

## 6. Risks & Gotchas

### 6.1 Security Tweaks

- **Tamper Protection**: Windows 10/11 may prevent writing to `TamperProtection` registry key even with admin rights. The key is protected by Defender itself. The UI should handle this gracefully and inform the user.
- **VBS/Memory Integrity**: These require a reboot to take effect. The UI should indicate "reboot required" after applying.
- **Vulnerable Driver Blocklist**: Requires reboot. Also, on some systems this key may not exist until VBS is enabled.
- **Spectre/Meltdown**: The `FeatureSettingsOverride` value `3` means "mitigations enabled" (system is vulnerable). The toggle semantics are inverted — "enabling" the mitigation means writing `0` to `FeatureSettingsOverride`. Careful with naming.
- **UAC**: Disabling UAC is a significant security risk. The UI should show a warning.
- **SmartScreen**: Disabling SmartScreen reduces protection against malicious apps. Warning recommended.

### 6.2 Performance Tweaks

- **GPU Scheduling** (`HwSchMode`): The enabled value is `2`, not `1`. This is unusual and must be handled correctly in the `RegistryTweak` definition.
- **MPO**: Requires Explorer restart. The `TweakList` control already handles this via `HasExplorerTweaks` and `RestartExplorerButton`.
- **Timer resolution tweaks**: `SkipTickOverride`, `UsePlatformTick`, `TimerExpiration` are under the same `kernel` key. They may interact with each other. The ground truth mentions "Black Flag Timer Resolution" as a single feature — consider whether these should be separate toggles or a single combined toggle.
- **Game Mode/DVR**: These are HKCU tweaks and don't require admin. They will work without elevation.

### 6.3 Power Editor

- **`/restoredefaultschemes` is system-wide**: This is the biggest risk. It resets ALL power plans, not just the selected one. The UI must have a clear confirmation dialog warning about this.
- **powercfg.exe output parsing**: The output format is well-documented but locale-dependent. The parser should be robust and handle variations.
- **Hidden settings**: Some settings are hidden by default. The UI may need to use `powercfg /qh` to show all settings, or just show the common ones.
- **AC vs DC values**: Most settings have separate AC and DC values. The UI should either show both or let the user choose which to edit.
- **Value type diversity**: Settings can be percentages, seconds, or enum indices. The UI needs different control types for each.
- **Admin required**: All powercfg operations require elevation. The UI should check `IsElevated` and disable the Apply button if not elevated.

### 6.4 General

- **RegistryTweakService already handles HKLM writes**: The service checks `RequiresAdmin` and throws `UnauthorizedAccessException` if not elevated. The UI should catch this and show an appropriate message.
- **TweakCatalog.All is read-only**: Adding new tweaks to the catalog is safe — the existing tests verify that all tweaks can be read without throwing.
- **DI registration**: New services must be registered in `App.xaml.cs` `ConfigureServices()`. Missing registration will cause runtime errors.
- **Navigation already wired**: `NavigationCatalog.cs` already has entries for Security, Performance, and Power Editor pages. No navigation changes needed.

---

## 7. Implementation Checklist

### 7.1 Security Page

- [ ] Add 8 Security tweak definitions to `TweakCatalog.cs`
- [ ] Update `SecurityPage.xaml.cs` to wire up `TweakPageViewModel` via factory
- [ ] Update `SecurityPage.xaml` to use `TweakList` control with Title/Subtitle
- [ ] Add catalog tests for Security tweaks

### 7.2 Performance Page

- [ ] Add 8 Performance tweak definitions to `TweakCatalog.cs`
- [ ] Update `PerformancePage.xaml.cs` to wire up `TweakPageViewModel` via factory
- [ ] Update `PerformancePage.xaml` to use `TweakList` control with Title/Subtitle
- [ ] Add catalog tests for Performance tweaks

### 7.3 Power Editor Page

- [ ] Create `IPowerService` interface in `Services/IPowerService.cs`
- [ ] Create `PowerService` implementation in `Services/PowerService.cs`
- [ ] Create `IProcessRunner` interface for testability
- [ ] Create `PowerPlan` and `PowerSetting` records
- [ ] Create `PowerEditorViewModel` in `ViewModels/PowerEditorViewModel.cs`
- [ ] Update `PowereditorPage.xaml` with custom layout (plan selector + settings editor + action bar)
- [ ] Update `PowereditorPage.xaml.cs` to wire up `PowerEditorViewModel`
- [ ] Register `IPowerService` and `PowerEditorViewModel` in `App.xaml.cs` DI
- [ ] Add PowerService tests (mocking IProcessRunner)
- [ ] Add PowerEditorViewModel tests (mocking IPowerService)

### 7.4 Shared

- [ ] Ensure all new tweaks pass existing `Catalog_ReadsEveryTweakWithoutThrowing` test
- [ ] Ensure all new tweaks pass existing `Catalog_OnlyTweaksThatNeedAdminAreMarkedSo` test
- [ ] Run full test suite — target: 130+ tests passing, 0 warnings, 0 errors

---

## 8. File Manifest

| File | Action | Description |
|------|--------|-------------|
| `Services/TweakCatalog.cs` | Modify | Add 8 Security + 8 Performance tweak definitions |
| `Features/Security/SecurityPage.xaml.cs` | Modify | Wire up TweakPageViewModel via factory |
| `Features/Security/SecurityPage.xaml` | Modify | Add TweakList control with Title/Subtitle |
| `Features/Performance/PerformancePage.xaml.cs` | Modify | Wire up TweakPageViewModel via factory |
| `Features/Performance/PerformancePage.xaml` | Modify | Add TweakList control with Title/Subtitle |
| `Services/IPowerService.cs` | Create | Interface for powercfg.exe wrapper |
| `Services/PowerService.cs` | Create | Implementation wrapping powercfg.exe |
| `Services/IProcessRunner.cs` | Create | Process abstraction for testability |
| `Services/ProcessRunner.cs` | Create | Default ProcessRunner implementation |
| `Models/PowerPlan.cs` | Create | Power plan record |
| `Models/PowerSetting.cs` | Create | Power setting record |
| `ViewModels/PowerEditorViewModel.cs` | Create | VM for Power Editor page |
| `Features/Powereditor/PowereditorPage.xaml` | Modify | Custom layout: plan selector + settings editor + action bar |
| `Features/Powereditor/PowereditorPage.xaml.cs` | Modify | Wire up PowerEditorViewModel |
| `App.xaml.cs` | Modify | Register IPowerService, IProcessRunner, PowerEditorViewModel |
| `Tests/PowerServiceTests.cs` | Create | Tests for PowerService with mocked IProcessRunner |
| `Tests/PowerEditorViewModelTests.cs` | Create | Tests for PowerEditorViewModel with mocked IPowerService |
| `Tests/TweakCatalogTests.cs` | Modify | Add tests for Security and Performance tweaks |

---

*Research completed: 2026-10-03*
