# Phase 4 — Security, Performance & Power — UI Design Contract

**Phase:** 04-security-performance-power  
**Date:** 2026-10-03  
**Status:** approved  
**Author:** gsd-ui-researcher  
**Reviewed:** 2026-10-03 by gsd-ui-checker (APPROVED with 5 non-blocking FLAGs)  

---

## 1. Scope

This contract covers the UI design for three Phase 4 pages:

| Page | Layout Pattern | Data Source |
|------|---------------|-------------|
| **Security** | TweakList (toggle list) | RegistryTweakService + TweakCatalog |
| **Performance** | TweakList (toggle list) | RegistryTweakService + TweakCatalog |
| **Power Editor** | Custom layout (plan selector + settings editor) | PowerService (powercfg.exe wrapper) |

All three pages live under `Features/Security/`, `Features/Performance/`, and `Features/Powereditor/` respectively, following the existing project convention.

---

## 2. Design System

**Tool:** None (no design system tokens, no components.json, no shadcn).  
**Framework:** WinUI 3 built-in controls only.  
**Component inventory:** Omitted — this project uses WinUI 3's built-in control set (NavigationView, ToggleSwitch, ComboBox, Button, InfoBar, etc.) with no custom design system layer.

### 2.1 Existing Patterns to Reuse

| Pattern | Source | Usage |
|---------|--------|-------|
| `TweakList` UserControl | `Controls/TweakList.xaml` | Shared presenter for registry-tweak pages — handles list, elevation notice, Explorer-restart action |
| `TweakPageViewModel` | `ViewModels/TweakPageViewModel.cs` | Shared VM for toggle pages — read state, toggle, revert, restart Explorer, report errors |
| `TweakToggleViewModel` | `ViewModels/TweakToggleViewModel.cs` | One toggle row — wraps RegistryTweak, keeps UI in step with registry |
| `TweakPageViewModelFactory` | `ViewModels/TweakPageViewModelFactory.cs` | DI-friendly factory for page VMs |
| `RegistryTweakService` | `Services/RegistryTweakService.cs` | Registry read/write, elevation check, Explorer restart |
| `InfoBar` notifications | Framework `IInfoBarService` | Success/error feedback |
| `DialogService` confirmations | Framework `IDialogService` | Destructive action confirmation |

### 2.2 Visual Language

- **Typography:** WinUI 3 default (Segoe UI Variable)
- **Spacing:** 8px grid (WinUI 3 default margins/padding)
- **Color:** WinUI 3 system theme (light/dark aware) — no custom palette
- **Icons:** WinUI 3 SymbolIcon / Fluent UI System Icons
- **Elevation:** InfoBar for status, ContentDialog for confirmations

---

## 3. Page Specifications

### 3.1 Security Page

**File:** `Features/Security/SecurityPage.xaml`  
**ViewModel:** `SecurityPageViewModel` (via TweakPageViewModelFactory)  
**Layout:** TweakList (reuse existing control)

#### 3.1.1 Structure

```
SecurityPage
└── TweakList
    ├── Title: "Security"
    ├── Subtitle: "Windows Defender, virtualization-based security, and exploit mitigations"
    ├── ElevationBar (InfoBar): "Some changes require administrator rights" (shown when !IsElevated && HasAdminTweaks)
    ├── TweaksHost (ItemsControl): list of TweakToggleViewModel rows
    │   └── [ToggleSwitch + Name + Description + StateText] × N
    ├── RestartExplorerButton: "Restart Explorer" (shown when HasExplorerTweaks)
    └── StatusMessage: "{on} of {total} enabled"
```

#### 3.1.2 Toggle List (TweakCatalog entries)

| ID | Name | Description | Registry Path | Default | Requires Admin | Requires Explorer Restart |
|----|------|-------------|---------------|---------|----------------|---------------------------|
| `security-defender-disable` | Disable Windows Defender | Turns off real-time protection (requires Tamper Protection off) | `HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\DisableAntiSpyware` | 0 (off) | Yes | No |
| `security-defender-tamper` | Disable Tamper Protection | Prevents Defender from being re-enabled by other tools | `HKLM\SOFTWARE\Microsoft\Windows Defender\Features\TamperProtection` | 5 (on) | Yes | No |
| `security-vbs` | Enable Virtualization-Based Security | Isolates critical system processes in a secure container | `HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\EnableVirtualizationBasedSecurity` | 0 (off) | Yes | No |
| `security-memory-integrity` | Enable Memory Integrity | Protects core processes from code injection attacks | `HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity\Enabled` | 0 (off) | Yes | No |
| `security-vulnerable-driver-blocklist` | Enable Vulnerable Driver Blocklist | Blocks known vulnerable drivers from loading | `HKLM\SYSTEM\CurrentControlSet\Control\CI\Config\VulnerableDriverBlocklistEnable` | 0 (off) | Yes | No |
| `security-spectre-meltdown` | Spectre & Meltdown Mitigations | CPU speculative execution mitigations (FeatureSettingsOverride) | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\FeatureSettingsOverride` | 0 (off) | Yes | No |
| `security-uac` | Disable User Account Control | Suppresses UAC prompts (not recommended) | `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System\EnableLUA` | 1 (on) | Yes | No |
| `security-smartscreen` | Disable SmartScreen | Turns off Windows SmartScreen app reputation checks | `HKLM\SOFTWARE\Policies\Microsoft\Windows\System\EnableSmartScreen` | 1 (on) | Yes | No |

#### 3.1.3 Copywriting

- **Page title:** "Security"
- **Page subtitle:** "Windows Defender, virtualization-based security, and exploit mitigations"
- **Elevation notice:** "Some changes require administrator rights. Restart the app as administrator to apply them."
- **Restart Explorer button:** "Restart Explorer"
- **Restart confirmation title:** "Restart Explorer"
- **Restart confirmation body:** "Explorer will be closed and restarted so the changes take effect. Any open File Explorer windows will close."
- **Status message:** "{on} of {total} enabled"

---

### 3.2 Performance Page

**File:** `Features/Performance/PerformancePage.xaml`  
**ViewModel:** `PerformancePageViewModel` (via TweakPageViewModelFactory)  
**Layout:** TweakList (reuse existing control)

#### 3.2.1 Structure

```
PerformancePage
└── TweakList
    ├── Title: "Performance"
    ├── Subtitle: "Timer resolution, GPU scheduling, and memory management tweaks"
    ├── ElevationBar (InfoBar): "Some changes require administrator rights" (shown when !IsElevated && HasAdminTweaks)
    ├── TweaksHost (ItemsControl): list of TweakToggleViewModel rows
    │   └── [ToggleSwitch + Name + Description + StateText] × N
    ├── RestartExplorerButton: "Restart Explorer" (shown when HasExplorerTweaks)
    └── StatusMessage: "{on} of {total} enabled"
```

#### 3.2.2 Toggle List (TweakCatalog entries)

| ID | Name | Description | Registry Path | Default | Requires Admin | Requires Explorer Restart |
|----|------|-------------|---------------|---------|----------------|---------------------------|
| `perf-skiptick` | Skip Tick Override | Reduces timer resolution for lower latency (Black Flag Timer Resolution) | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\SkipTickOverride` | 0 (off) | Yes | No |
| `perf-platform-tick` | Use Platform Tick | Forces use of the platform timer instead of HPET | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\UsePlatformTick` | 0 (off) | Yes | No |
| `perf-timer-expiration` | Timer Expiration | Controls timer expiration behavior for power/performance tradeoff | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\TimerExpiration` | 0 (default) | Yes | No |
| `perf-mpo` | Multiplane Overlay (MPO) | Enables hardware overlay planes for reduced GPU load | `HKLM\SOFTWARE\Microsoft\Windows\Dwm\OverlayTestMode` | 0 (off) | Yes | Yes |
| `perf-gpu-scheduling` | Hardware-Accelerated GPU Scheduling | Lets the GPU manage its own memory scheduling | `HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\HwSchMode` | 2 (on) | Yes | No |
| `perf-working-set` | Working Set Adjustment | Adjusts system working set size for better memory management | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\LargeSystemCache` | 0 (off) | Yes | No |
| `perf-game-mode` | Game Mode | Prioritizes game processes for better frame times | `HKCU\Software\Microsoft\GameBar\AllowAutoGameMode` | 1 (on) | No | No |
| `perf-game-dvr` | Game DVR | Background recording for game clips | `HKCU\System\GameConfigStore\GameDVR_Enabled` | 1 (on) | No | No |

#### 3.2.3 Copywriting

- **Page title:** "Performance"
- **Page subtitle:** "Timer resolution, GPU scheduling, and memory management tweaks"
- **Elevation notice:** "Some changes require administrator rights. Restart the app as administrator to apply them."
- **Restart Explorer button:** "Restart Explorer"
- **Restart confirmation title:** "Restart Explorer"
- **Restart confirmation body:** "Explorer will be closed and restarted so the changes take effect. Any open File Explorer windows will close."
- **Status message:** "{on} of {total} enabled"

---

### 3.3 Power Editor Page

**File:** `Features/Powereditor/PowereditorPage.xaml`  
**ViewModel:** `PowerEditorViewModel` (new — does NOT use TweakPageViewModel)  
**Layout:** Custom (plan selector + settings editor)

#### 3.3.1 Structure

```
PowerEditorPage
├── Title: "Power Editor"
├── Subtitle: "Edit power plan settings at the powercfg level"
├── PlanSelector (ComboBox)
│   ├── Label: "Power Plan"
│   ├── Items: [Balanced, High Performance, Power Saver, Custom...]
│   └── SelectedItem: binds to current plan
├── SettingsEditor (Grid/StackPanel)
│   ├── Category: "Processor Power Management"
│   │   ├── Minimum processor state (Slider/ComboBox: 0-100%)
│   │   ├── Maximum processor state (Slider/ComboBox: 0-100%)
│   │   └── Processor performance boost mode (ComboBox: Enabled/Disabled/Aggressive)
│   ├── Category: "Display"
│   │   ├── Turn off display after (ComboBox: 1min/5min/10min/15min/30min/1hr/Never)
│   │   └── Turn off hard disk after (ComboBox: 1min/5min/10min/15min/30min/1hr/Never)
│   ├── Category: "Sleep"
│   │   ├── Sleep after (ComboBox: 1min/5min/10min/15min/30min/1hr/Never)
│   │   ├── Hibernate after (ComboBox: 1min/5min/10min/15min/30min/1hr/Never)
│   │   └── Allow hybrid sleep (ToggleSwitch)
│   └── Category: "Power Buttons and Lid"
│       ├── Power button action (ComboBox: Do nothing/Sleep/Hibernate/Shut down)
│       ├── Sleep button action (ComboBox: Do nothing/Sleep/Hibernate/Shut down)
│       └── Lid close action (ComboBox: Do nothing/Sleep/Hibernate/Shut down)
├── ActionBar (CommandBar)
│   ├── ApplyButton: "Apply Changes" (primary)
│   ├── RevertButton: "Revert" (secondary)
│   └── RefreshButton: "Refresh" (secondary)
├── ElevationBar (InfoBar): "Power settings require administrator rights" (shown when !IsElevated)
└── StatusMessage: "Ready" / "Applying..." / "Changes applied" / "Reverted to defaults"
```

#### 3.3.2 ViewModel Design

```csharp
public partial class PowerEditorViewModel : ViewModelBase
{
    private readonly IPowerService _power;
    private readonly IDialogService _dialogs;
    private readonly IInfoBarService _infoBar;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    [ObservableProperty]
    public partial bool IsElevated { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial PowerPlan? SelectedPlan { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<PowerPlan> Plans { get; } = [];

    [ObservableProperty]
    public partial ObservableCollection<PowerSetting> Settings { get; } = [];

    [RelayCommand]
    private async Task LoadPlansAsync();

    [RelayCommand]
    private async Task LoadSettingsAsync();

    [RelayCommand]
    private async Task ApplyChangesAsync();

    [RelayCommand]
    private async Task RevertChangesAsync();

    [RelayCommand]
    private async Task RefreshAsync();
}
```

#### 3.3.3 PowerService Interface

```csharp
public interface IPowerService
{
    bool IsElevated { get; }
    Task<IReadOnlyList<PowerPlan>> GetPlansAsync();
    Task<IReadOnlyList<PowerSetting>> GetSettingsAsync(Guid planGuid);
    Task ApplySettingAsync(Guid planGuid, string settingGuid, string value);
    Task RevertPlanAsync(Guid planGuid);
}
```

#### 3.3.4 Copywriting

- **Page title:** "Power Editor"
- **Page subtitle:** "Edit power plan settings at the powercfg level"
- **Plan selector label:** "Power Plan"
- **Apply button:** "Apply Changes"
- **Revert button:** "Revert"
- **Refresh button:** "Refresh"
- **Elevation notice:** "Power settings require administrator rights. Restart the app as administrator to apply them."
- **Apply confirmation title:** "Apply Power Changes"
- **Apply confirmation body:** "The selected power plan settings will be applied. This may affect system performance and battery life."
- **Revert confirmation title:** "Revert Power Plan"
- **Revert confirmation body:** "All settings for this plan will be restored to their defaults. This cannot be undone."
- **Status messages:**
  - Idle: "Ready"
  - Loading: "Loading power plans..."
  - Applying: "Applying changes..."
  - Applied: "Changes applied successfully"
  - Reverted: "Plan reverted to defaults"
  - Error: "Could not apply changes: {error}"

---

## 4. Interaction Patterns

### 4.1 Toggle Pages (Security, Performance)

1. **Initial load:** TweakPageViewModel reads all tweak states from registry, populates toggle list
2. **Toggle change:** User flips ToggleSwitch → TweakToggleViewModel writes to registry → switch reflects actual state (rolls back on failure)
3. **Refresh:** Re-reads all states from registry
4. **Restart Explorer:** Confirmation dialog → taskkill/relaunch explorer.exe → InfoBar success
5. **Elevation:** InfoBar shown when admin tweaks exist and app is not elevated

### 4.2 Power Editor

1. **Initial load:** LoadPlansAsync populates plan selector → LoadSettingsAsync populates settings editor
2. **Plan selection:** User selects plan → LoadSettingsAsync refreshes settings for that plan
3. **Edit setting:** User changes value in ComboBox/Slider/ToggleSwitch → staged in memory (not yet applied)
4. **Apply:** Confirmation dialog → ApplyChangesAsync writes all staged values via powercfg → InfoBar success
5. **Revert:** Confirmation dialog → RevertPlanAsync restores plan defaults → settings editor refreshes
6. **Refresh:** Re-reads plan and settings from powercfg

---

## 5. Error Handling

| Scenario | Behavior |
|----------|----------|
| Registry write fails | ToggleSwitch rolls back to previous state, InfoBar shows error |
| powercfg.exe fails | StatusMessage shows error, InfoBar shows error |
| Not elevated (Security/Performance) | InfoBar shows elevation notice, toggles still visible but writes may fail |
| Not elevated (Power Editor) | InfoBar shows elevation notice, Apply button disabled |
| Explorer restart fails | InfoBar shows error with exception message |
| Plan load fails | StatusMessage shows error, settings editor empty |

---

## 6. Accessibility

- All ToggleSwitch controls have `AutomationProperties.Name` set to the tweak name
- ComboBox controls have `AutomationProperties.Name` set to the setting category
- Buttons have `AutomationProperties.Name` set to the action
- InfoBar controls have `AutomationProperties.Name` set to the message
- Keyboard navigation follows WinUI 3 default tab order
- Focus visual style follows WinUI 3 system theme

---

## 7. File Manifest

| File | Action | Description |
|------|--------|-------------|
| `Features/Security/SecurityPage.xaml` | Modify | Add TweakList binding, set Title/Subtitle |
| `Features/Security/SecurityPage.xaml.cs` | Modify | Wire up TweakPageViewModel via factory |
| `Features/Performance/PerformancePage.xaml` | Modify | Add TweakList binding, set Title/Subtitle |
| `Features/Performance/PerformancePage.xaml.cs` | Modify | Wire up TweakPageViewModel via factory |
| `Features/Powereditor/PowereditorPage.xaml` | Modify | Custom layout: plan selector + settings editor + action bar |
| `Features/Powereditor/PowereditorPage.xaml.cs` | Modify | Wire up PowerEditorViewModel |
| `ViewModels/PowerEditorViewModel.cs` | Create | New VM for Power Editor page |
| `Services/IPowerService.cs` | Create | Interface for powercfg.exe wrapper |
| `Services/PowerService.cs` | Create | Implementation wrapping powercfg.exe |
| `Services/TweakCatalog.cs` | Modify | Add Security and Performance tweak definitions |
| `App.xaml.cs` (or DI registration) | Modify | Register IPowerService and PowerEditorViewModel |

---

## 8. Out of Scope

- WMI queries (D-02: registry only)
- Custom design system components (use WinUI 3 built-ins)
- Power plan creation/deletion (only editing existing plans)
- Per-application power profiles (not in scope for Phase 4)
- Battery health monitoring (not in scope for Phase 4)
- Windows Update power settings (not in scope for Phase 4)

---

## 10. UI Considerations

> Populated by the ui-phase UI-consideration probe (Step 9.5) and lifted by plan-phase's
> `## UI Considerations` lift rule via the identical rule as SPEC `## Edge Coverage`. Shape-rooted UI *state*
> coverage (empty / loading / error / populated / partial / overflow / zero-one-many / long-text).
> Empty-state and error-state COPY live in `## Copywriting Contract` above — this section covers
> state coverage and REFERENCES those rows rather than restating the copy (de-dup).

Applicable state considerations resolved: 28 covered, 0 backstop, 0 unresolved

| Category | Element(s) | Status | Resolution / Reason |
|----------|------------|--------|---------------------|
| empty | E1, E2 (toggle lists) | ✅ covered | InfoBar shows "No tweaks available" with retry button when registry returns zero tweaks |
| loading | E1, E2 (toggle lists) | ✅ covered | ProgressRing with "Loading tweaks..." text while registry read is in progress |
| error | E1, E2 (toggle lists) | ✅ covered | InfoBar shows "Failed to load tweaks: {error}" with retry button on registry failure |
| populated | E1, E2 (toggle lists) | ✅ covered | Toggle list renders all 8 toggles with ToggleSwitch + Name + Description + StateText |
| partial | E1, E2 (toggle lists) | ✅ covered | Tweaks shown are those successfully read; failed reads are omitted from list |
| overflow | E1, E2 (toggle lists) | ✅ covered | TweakList ItemsControl is scrollable when toggle count exceeds visible area |
| zero-one-many | E1, E2 (toggle lists) | ✅ covered | Status message shows "{on} of {total} enabled" — handles 0, 1, and N toggles |
| long-text | E1, E2 (toggle lists) | ✅ covered | Toggle name wraps to 2 lines; description wraps to 3 lines with ellipsis |
| empty | E3 (plan selector) | ✅ covered | ComboBox shows "No power plans found" when powercfg returns zero plans |
| loading | E3 (plan selector) | ✅ covered | ComboBox shows "Loading plans..." while powercfg /list is in progress |
| error | E3 (plan selector) | ✅ covered | InfoBar shows "Failed to load plans: {error}" on powercfg failure |
| populated | E3 (plan selector) | ✅ covered | ComboBox displays all available power plans (Balanced, High Performance, Power Saver, Custom) |
| partial | E3 (plan selector) | ✅ covered | Plans successfully enumerated are shown; failed enumeration shows error InfoBar |
| overflow | E3 (plan selector) | ✅ covered | ComboBox dropdown scrolls when plan count exceeds visible area |
| zero-one-many | E3 (plan selector) | ✅ covered | "No plans" / "1 plan" / "N plans" — ComboBox handles all counts |
| long-text | E3 (plan selector) | ✅ covered | Plan name truncates with ellipsis in ComboBox |
| empty | E4 (settings editor) | ✅ covered | Settings editor shows "Select a power plan to edit its settings" when no plan selected |
| loading | E4 (settings editor) | ✅ covered | ProgressRing with "Loading settings..." while powercfg /query is in progress |
| error | E4 (settings editor) | ✅ covered | InfoBar shows "Failed to load settings: {error}" on powercfg failure |
| populated | E4 (settings editor) | ✅ covered | Settings grid renders all categories (Processor, Display, Sleep, Power Buttons) with controls |
| partial | E4 (settings editor) | ✅ covered | Categories successfully queried are shown; failed queries show error InfoBar |
| overflow | E4 (settings editor) | ✅ covered | Settings grid is scrollable when settings count exceeds visible area |
| zero-one-many | E4 (settings editor) | ✅ covered | "No settings" / "1 setting" / "N settings" — grid handles all counts |
| long-text | E4 (settings editor) | ✅ covered | Setting name wraps to 2 lines; category header is single line |
| loading | E5 (action bar) | ✅ covered | Apply/Revert/Refresh buttons disabled; status shows "Applying..." or "Reverting..." |
| error | E5 (action bar) | ✅ covered | InfoBar shows "Failed to apply changes: {error}" on powercfg failure |
| long-text | E5 (action bar) | ✅ covered | Button labels are short ("Apply Changes", "Revert", "Refresh") — no truncation needed |
| unclassified | E6 (status message) | ✅ covered | Status message shows: "Ready", "Loading...", "Applying...", "Applied", "Reverted", "Error: {error}" |
| unclassified | E7 (elevation notice) | ✅ covered | InfoBar shows "Some changes require administrator rights. Restart the app as administrator to apply them." |
| loading | E8 (restart explorer) | ✅ covered | Button disabled; status shows "Restarting Explorer..." |
| error | E8 (restart explorer) | ✅ covered | InfoBar shows "Failed to restart Explorer: {error}" |
| long-text | E8 (restart explorer) | ✅ covered | Button label "Restart Explorer" is short — no truncation needed |

<!-- Status vocabulary (locked by probe-core projectTruths):
     ✅ covered   → a plain truth string lifted into must_haves.truths
     🧪 backstop  → a flat scalar { statement, verification: backstop }; at verify time, no explicit
                    evidence → insufficient_spec → human_needed (never a silent pass, #1154)
     ⚠ unresolved → an explicit planner assumption (surfaced, never silently dropped)
     Rows are REPLACED (not appended) on a probe re-run — idempotent. -->

---

## 9. Acceptance Criteria

- [ ] Security page renders toggle list with all 8 Security tweaks from TweakCatalog
- [ ] Performance page renders toggle list with all 8 Performance tweaks from TweakCatalog
- [ ] Both toggle pages show elevation notice when not elevated
- [ ] Both toggle pages show "Restart Explorer" button when applicable
- [ ] Power Editor page loads and displays available power plans
- [ ] Power Editor page loads and displays settings for selected plan
- [ ] Power Editor Apply button writes changes via powercfg.exe
- [ ] Power Editor Revert button restores plan defaults
- [ ] All error states show InfoBar with descriptive message
- [ ] All confirmation dialogs use IDialogService
- [ ] No custom design system components introduced
- [ ] All copywriting matches the specifications above
