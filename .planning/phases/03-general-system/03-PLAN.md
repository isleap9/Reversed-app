# Phase 3: General & System — PLAN

**Phase:** 3 of 10
**Goal:** Implement the General sub-pages and the System tweak page, all driven by a
shared registry tweak engine with read/apply/revert.
**Requirements:** GEN-01..06, SYS-01..04
**Depends on:** Phase 2 — complete

## Research notes (recovered from the binary)

Registry roots the real app touches for these pages:
```
HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced
HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers
HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize
HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced   (32/64-bit view both used)
HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer
HKLM\SOFTWARE\Policies\Microsoft\Windows\Explorer
HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\LogonUI\BootAnimation
HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\KernelShadowStacks
HKLM\SYSTEM\CurrentControlSet\Services\W32Time\Parameters            (NtpServer)
SOFTWARE\Microsoft\Windows NT\CurrentVersion\Time Zones\
HKCR\AllFilesystemObjects\shellex\ContextMenuHandlers\SendTo
HKCR\UserLibraryFolder\shellex\ContextMenuHandlers\SendTo
HKLM\SOFTWARE\Microsoft\Windows\Dwm
```

Value names confirmed present: `Hidden`, `UseCompactMode`, `NoDriveTypeAutoRun`,
`NoAutorun`, `DisableStartupSound`, `EnableLUA`, `EnableTransparency`,
`SettingsPageVisibility`, `NtpServer`, `ConsentPromptBehaviorAdmin`,
`VulnerableDriverBlocklistEnable`, `Kernel Shadow Stacks`.

Context-menu CLSIDs found (Windows 11 classic-menu restore and the Send-To handler):
```
{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}   classic (Windows 10) context menu
{7BA4C740-9E81-11CF-99D3-00AA004AE837}   Send To
{00000122-0000-0000-c000-000000000046}   IContextMenu (interface IID)
```

Shell-restart behaviour: the real app runs
`taskkill.exe /f /im explorer.exe` then relaunches `explorer.exe` to apply shell
tweaks. Phase 3 implements this behind an explicit, confirmed action.

Safety wording recovered verbatim (used by the delete-confirmation path):
"it does NOT go to the Recycle Bin. Deleting system files can break Windows."

## Design

### The tweak engine (the core of this phase)

Everything on these pages is the same shape: a named registry value with an
"enabled" and "disabled" state, optionally needing admin or a shell restart. One
engine serves all six pages rather than six bespoke implementations.

- `Models/RegistryTweak.cs` — a tweak definition:
  - `Id`, `Name`, `Description`
  - `Hive`, `KeyPath`, `ValueName`, `ValueKind` (`Dword`/`String`)
  - `EnabledValue`, `DisabledValue`
  - `RequiresAdmin`, `RequiresExplorerRestart`
  - `DefaultState` (so "unset" reads as a known default rather than "unknown")
- `Services/IRegistryTweakService.cs`
  - `TweakState Read(RegistryTweak)` → `Enabled` / `Disabled` / `Unset` / `Unavailable`
  - `Task ApplyAsync(RegistryTweak)` / `Task RevertAsync(RegistryTweak)`
  - `bool IsElevated`
- `Services/RegistryTweakService.cs` — Microsoft.Win32.Registry implementation.
  Reads must be side-effect free and never throw; writes surface failures.
- `Services/TweakCatalog.cs` — the real tweak set, grouped per page.

`Unavailable` matters: some keys are absent by design, and a 32-bit process sees a
different `Explorer\Advanced` view. Reads check both views and report honestly.

### Tasks

**3-01 — Tweak engine**
- `RegistryTweak` model, `IRegistryTweakService`, `RegistryTweakService`
- Elevation detection (reads current token; no prompt)
- `TweakCatalog` with the real paths/values above
- Unit tests: read/apply/revert round-trip against `HKCU\Software\VainTools\Test\...`
  (never touching real system keys in tests)

**3-02 — Explorer, Context Menu, Visual, General**
- `ExplorerPage`: file extensions, hidden files, super-hidden, compact mode,
  Quick Access target, recent/frequent files, autoplay/autorun
- `ContextMenuPage`: classic (Win10) context menu restore; Send To handler entries;
  "Restart Explorer" action
- `VisualPage`: window animations, taskbar animations, listview shadows, drag full
  windows, font smoothing, menu show delay
- `GeneralPage`: an overview listing every General tweak with its live state
- Shared `TweakToggleViewModel` so all four pages reuse one toggle row implementation

**3-03 — Date & Time, Settings Visibility, System**
- `DateTimePage`: NTP server list (read/write `W32Time\Parameters\NtpServer`),
  time-zone list from `Time Zones\`, apply/reset
- `SettingsVisibilityPage`: `SettingsPageVisibility` string with a picker of known
  Settings pages and show/hide-all actions
- `SystemPage`: startup sound, transparency, autoplay/autorun, plus a state summary
- Every page reports `applied` vs `reboot/explorer-restart required` accurately

## Success criteria (from ROADMAP)

1. General, Explorer, Context Menu, Visual, Date & Time and Settings Visibility pages
   all render their real controls
2. Explorer tweaks (extensions, hidden files) apply and revert
3. Context menu entries toggle correctly
4. System tweaks (autoplay, autorun, startup sound, transparency) apply and revert
5. Reboot-required changes are reported as such

## Risks

| Risk | Mitigation |
|---|---|
| Writing real registry keys could damage the machine | Tests only ever touch `HKCU\Software\VainTools\Test`; the UI reverts via the same engine and shows current state before changing it |
| 32/64-bit registry view differs for `Explorer\Advanced` | Read both views; report `Unavailable` rather than guessing |
| Elevation absent | Detect and explain; never silently no-op |
| Explorer restart is disruptive | Explicit confirmed action only, never automatic; warn that windows will close |
| HKLM writes need admin | Mark tweaks `RequiresAdmin` and surface it before the user tries |

## Out of scope

- Security/Performance/Power tweaks (Phase 4)
- Anything requiring the NVIDIA driver
