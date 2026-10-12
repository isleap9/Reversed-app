# Akari Toolbox — General Group Design

**Date:** 2026-10-11
**Status:** Draft for approval
**Parent spec:** PROJECT.md (Vain Toolbox Rebuild)
**Prior slice:** `docs/superpowers/specs/2026-10-10-akari-homepage-design.md` (built, launches)

---

## 1. Intent

Turn the app from one screen into an app that navigates. This slice wires the
shell's `NavigationView` to real pages and builds the entire **General group**:
six pages of simple Windows tweaks (toggles backed by registry values, service
start modes, and `bcdedit` flags). These are the easiest real pages in the
original, so they prove the navigation + tweak patterns every later page will
reuse.

The user's job this serves: flip common Windows annoyances (file extensions
hidden, RGB-style transparency, dead context menu entries, wrong NTP servers)
from one place, without opening regedit.

---

## 2. Goals

- **Working navigation**: nav items for Home + the six General pages switch the
  content area; the selected item tracks the visible page; Home keeps working
  exactly as today.
- **Six General pages**, each reading live state on load and writing through on
  toggle (behavior first, like the homepage — no mock toggles):
  1. **General** (`GeneralPage`): Hyper-V (`bcdedit /set hypervisorlaunchtype`),
     Print Spooler service start mode, Remote Desktop, LPC controller — the
     four `*_Changed` handlers from the original.
  2. **Explorer** (`ExplorerPage`): `HKCU\...\Explorer\Advanced` values (file
     extensions, hidden/system files) + taskbar keys. Data-driven rows.
  3. **Context Menu** (`ContextMenuPage`): Take Ownership (`HKCR\Directory\shell\runas`),
     Send-To handler, `ShellNew` entries (`.ps1`/`.bat`/`.reg`), modern-menu
     CLSIDs. Data-driven rows. HKCR writes require the admin elevation the app
     already mandates.
  4. **Visual** (`VisualPage`): personalization + transparency + visual effects.
     Data-driven rows.
  5. **Date & Time** (`DateTimePage`): NTP server list with add/apply
     (`HKLM\...\Services\W32Time\Parameters`), from the original's
     "NTP Servers / Add NTP server / Apply NTP Servers" UI.
  6. **Settings Visibility** (`SettingsVisibilityPage`): dynamically built rows
     that hide Settings pages (`hide:` URIs via `SettingsPageVisibility`).
- **Shared tweak plumbing** in Framework so pages 2–4 (and later slices) don't
  reimplement registry toggle logic: one `ToggleSetting` model + one
  `IRegistrySettingService` (read/write DWORD + string, HKCU/HKLM/HKCR,
  non-throwing with fallback), consumed by a shared row control or
  `ItemsRepeater` template.
- **Registry writes need admin**: the app already mandates
  `requireAdministrator`, so no new elevation work.

## 3. Non-Goals

- Other page groups (System, Security, Performance, GPU, Apps, Sound, Network,
  Startup, Tools, Affinity, About, Vain Tools) — later slices.
- `.vain` import/export — explicitly future work per user.
- Pixel-perfect parity with the original's General pages — behavior first.
- Undo/history for tweaks; reboot-required prompts beyond a note.

## 4. Anti-Goals

- Toggles that only flip UI state without reading/writing the real setting.
- A navigation scheme that only this slice's pages can use (it must serve all
  29 pages eventually).
- Touching homepage behavior, theme system, or hardware detection except
  through existing interfaces.

---

## 5. Constraints

| Constraint | Value |
|------------|-------|
| Stack | Same as homepage: .NET 10, WinUI 3, App SDK 2.5.1, CommunityToolkit.Mvvm, Hosting DI |
| MVVM | Each page gets a ViewModel; no code-behind setting logic beyond navigation |
| Registry access | Via new `IRegistrySettingService`; failures fall back to last-known state, never crash |
| Drivers | `bcdedit` and service control reuse the CLI/service patterns from PROJECT.md §7/§15 |
| Tests | xUnit; registry-touching tests use throwaway keys like `ThemeServiceTests` |

---

## 6. Navigation Design

- `MainWindow` owns a simple navigator: `NavView_SelectionChanged` maps the
  selected item's `Tag` to a `Page` instance (constructed with DI services, same
  as `HomePage` today) and swaps `ContentGrid` children.
- The six General items join the menu (enabled, with icons); remaining future
  items (Vain Tools, GPU, …) stay visible-but-disabled as today.
- Page constructors keep the `(services...)` DI-friendly shape; the ViewModel
  is created in code-behind, never in XAML (homepage ruling, retained).
- Deep-linking, back-stack, and breadcrumb UX are out of scope — selection
  follows the visible page, nothing more.

## 7. Page Specifications

### 7.1 General

Four section toggles with immediate effect + status text:

| Toggle | Backend |
|--------|---------|
| Hyper-V | `bcdedit /set hypervisorlaunchtype auto` (off = `off`); read via `/enum` |
| Print Spooler | Service `Spooler` start mode (auto/disabled) |
| Remote Desktop | Service `TermService` start mode + `fDenyTSConnections` |
| LPC controller | Original's `LpcController_Changed`; exact backend is builder's call (see §9) |

### 7.2 Explorer

Data-driven toggle rows over well-known values under
`HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced`
(`HideFileExt`, `Hidden`, `ShowSuperHidden`) plus taskbar keys. Each row:
label, current-state switch, writes through on flip. Explorer restart is NOT
triggered automatically (note in UI that some changes need it).

### 7.3 Context Menu

Data-driven rows, each mapping to a registry presence check:

| Row | Key |
|-----|-----|
| Take Ownership | `HKCR\Directory\shell\runas` present/absent |
| Send To | `HKCR\AllFilesystemObjects\shellex\ContextMenuHandlers\SendTo` |
| New PS1 / BAT / REG files | `HKCR\.ps1\ShellNew` etc. |
| Classic vs modern menu | The two modern-menu CLSIDs under `HKCU\Software\Classes\CLSID` |

Applying a row creates/deletes the key (default values per original where the
evidence names them; standard Windows defaults otherwise — builder's call,
recorded in code comments).

### 7.4 Visual

Data-driven rows over personalization + effects keys
(`HKCU\...\Themes\Personalize`: transparency, `AppsUseLightTheme` stays
display-only here since the app theme owns it). Exact row list is builder's
call from PROJECT.md registry roots.

### 7.5 Date & Time

- NTP server list (read from `W32Time\Parameters`), Add box ("Add NTP server
  (e.g. time.windows.com)"), Apply button ("Apply NTP Servers") that writes
  the list and restarts the time service.
- Current-time display is out of scope (clock already lives in the taskbar).

### 7.6 Settings Visibility

- Rows built at runtime (`BuildUI` in the original): one per known Settings
  page with a visibility switch.
- Writes the `SettingsPageVisibility` policy value (`hide:` URI list).
- The known-page list ships as data in the ViewModel (builder's call on the
  exact list; must include at least the pages the original names in XBF
  strings).

---

## 8. Shared Plumbing (Framework)

- `Models/ToggleSetting.cs`: `{ Key, Label, Description?, Current, IsApplying }`
  record-style observable model.
- `Services/IRegistrySettingService.cs` + `RegistrySettingService.cs`:
  `bool? ReadDword(hive, path, name)`, `WriteDword(...)`, `string? ReadString`,
  `WriteString`, `bool Exists(path)`, `DeleteKey(...)` — all non-throwing
  (null/false on failure), HKCU/HKLM/HKCR selectable.
- `Services/IServiceControl.cs` (new, small): service start-mode
  read/set via `ServiceController` + registry `Start` value; non-throwing.
- Tests: throwaway-key round-trip tests for the registry service (mirroring
  `ThemeServiceTests` isolation); service-control tests assert graceful
  failure on bogus service names (no real services touched in tests).

---

## 9. Decisions Made

| Decision | Answer |
|----------|--------|
| **Scope** | Navigation + all six General pages in one slice (they share the plumbing; splitting would duplicate it) |
| **Row pattern** | Data-driven (`ToggleSetting` + shared template), matching the original's `Connect` approach |
| **Explorer restart** | Never automatic; UI notes when a change needs it |
| **LPC controller backend** | Builder's call — evidence names the handler but not the key; builder picks the closest documented control and notes it |
| **Settings page list** | Builder's call, data in ViewModel, minimum = original's XBF-named pages |
| **Admin** | Reuse existing elevation; no per-action UAC work |

---

## 10. Builder Calls

- Exact XAML layout per page (within the patterns above).
- Icon choices for the six nav items.
- Which Explorer/Visual rows beyond the named keys (must all be real, tested keys).
- The `SettingsPageVisibility` value format detail (`hide:` join semantics).
- Whether service toggles apply immediately or stage behind Apply (recommend immediate, note it).

---

## 11. Acceptance

- App launches as today; clicking each of the 7 nav items shows its page with
  live (non-placeholder) state; Home still shows hardware + theme menu.
- Flipping any toggle changes the real Windows setting (spot-check 2 per
  page against regedit/services), persists across app restart, and survives
  a missing-key first run (sensible default, no crash).
- `dotnet build` 0 errors; `dotnet test` all green (new tests included).

---

## 12. Builder-Check Resolutions (my calls — check while reading)

A builder review of this spec asked 11 questions; all resolved here:

1. **RDP mapping**: display reads `fDenyTSConnections` as source of truth
   (0 = On). Toggle On writes service `Automatic` + `fDenyTSConnections=0`;
   Off writes `Disabled` + `1`. Disagreement shows whatever the flag says.
2. **Registry service contract**: every method takes an explicit hive
   parameter (`RegistryHive` enum) — no HKCR-vs-HKCU ambiguity.
3. **Spooler Off = Disabled**; On = Automatic. A pre-existing `Manual` start
   mode displays as On with subtitle "Manual".
4. **NTP**: standard `NtpServer` format (space-separated `host,0xflag`
   entries); Add validates `host[,0xflag]` then stages in memory; Apply writes
   the value, runs `w32tm /config /update`, and restarts `W32Time` via
   `IServiceControl`.
5. **Classic menu**: the two modern-menu CLSIDs from PROJECT.md §3.6 under
   `HKCU\Software\Classes\CLSID` (`{86ca1aa0-...}` and `{6480400b-...}`
   `InprocServer32`); key present = classic menu restored. Toggle creates/
   deletes both together.
6. **SettingsPageVisibility**: `REG_SZ` at
   `HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer`.
7. **Service toggles** set the start mode AND start/stop the running service
   to match (immediate effect; status text reflects live state).
8. **Navigation**: one cached page instance per nav item (preserves toggle
   state); six new items with fresh `Tag`s added to `MainWindow.xaml`
   (current Tags are Home/VainTools/General/GPU only).
9. **Hyper-V read**: parse the `hypervisorlaunchtype` line of
   `bcdedit /enum {current}`; absent line = Off. Off writes `off`
   (explicit, not `/deletevalue`).
10. **Row UI location**: shared row template/control lives in
    `AkariTools.App/Controls`; Framework holds only model + services.
11. **HKLM/HKCR tests**: registry service takes an injectable base path like
    `ThemeService` (unit tests redirect under HKCU throwaway keys);
    ViewModel tests use mocked services; no test touches real HKLM/HKCR.

---

## 13. Context Menu Binary Evidence (SUPERSEDES §7.3 row table + §12.5)

Decompiled `Vain\Vain Toolbox.exe` via REA/Ghidra 2026-10-11. The original
Context Menu page has SEVEN toggles (not the two-CLSID sketch in §7.3), each
writing a multi-key value set. Framework models this with
`ToggleKind.KeyValues` + `ToggleSetting.Probe/Writes/Deletes` +
`IRegistrySettingService.WriteExpandString`. PROJECT.md §3.6 GUIDs corrected
accordingly (the adapter CLSID is `{6480100b-…}`, not `{6480400b-…}`).

| # | Id / Label | Probe (string read non-null ⇒ On) | Writes on enable | Deletes on disable |
|---|------------|-----------------------------------|------------------|--------------------|
| 1 | `ModernMenu` / "Modern Context Menu" | `HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32` default | same value, REG_SZ empty | `{86ca1aa0-…}` tree |
| 2 | `TakeOwnership` / "Take Ownership" | `HKCR\Directory\shell\runas` default | default=`Take Ownership`, `HasLUAShield`="", `NoWorkingDirectory`="", `command` default + `IsolatedCommand`=`cmd.exe /c takeown /f "%1" /r /d y && icacls "%1" /grant *S-1-5-32-544:F /t` | `Directory\shell\runas` tree |
| 3 | `SendTo` / "Send To" | `HKCR\AllFilesystemObjects\shellex\ContextMenuHandlers\SendTo` default | default=`{7BA4C740-9E81-11CF-99D3-00AA004AE837}` at that path AND `HKCR\UserLibraryFolder\shellex\ContextMenuHandlers\SendTo` | both trees |
| 4 | `NewPs1` / "New Windows PowerShell Script" | `HKCR\.ps1\ShellNew` `NullFile` | `HKCR\.ps1` default=`Microsoft.PowerShellScript.1`, `HKCR\.ps1\ShellNew` `NullFile`="", `HKCR\Microsoft.PowerShellScript.1` default + `FriendlyTypeName`=`Windows PowerShell Script` | `HKCR\.ps1\ShellNew` ONLY |
| 5 | `NewBat` / "New Batch File" | `HKCR\.bat\ShellNew` `NullFile` | `NullFile`="" (REG_SZ), `ItemName`=`%systemroot%\System32\acppage.dll,-6002` (REG_EXPAND_SZ) | `HKCR\.bat\ShellNew` |
| 6 | `NewReg` / "New Registry File" | `HKCR\.reg\ShellNew` `NullFile` | `NullFile`="" (REG_SZ), `ItemName`=`%systemroot%\regedit.exe,-309` (REG_EXPAND_SZ) | `HKCR\.reg\ShellNew` |
| 7 | `ModernViews` / "Modern File Explorer Views" | `HKCU\Software\Classes\CLSID\{2aa9162e-c906-4dd9-ad0b-3d24a8eef5a0}` default | per-CLSID triple (default + `InprocServer32` default + `ThreadingModel`=`Apartment`) for `{2aa9162e-…}` and `{6480100b-…}` | both trees |
