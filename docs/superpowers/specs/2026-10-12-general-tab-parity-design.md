# Akari Toolbox — General Tab Parity Design

**Date:** 2026-10-12
**Status:** Draft for approval
**Parent spec:** PROJECT.md (Vain Toolbox Rebuild)
**Prior slice:** `docs/superpowers/specs/2026-10-11-general-group-design.md` (behavioral backends for the six General pages; merged to main)
**Evidence:** `Vain-Toolbox-general-tab.json` (75 REA records, this session), XBF control names in `VainToolbox-xbf-controls.txt`, screenshots 2026-10-12

---

## 1. Intent

The merged General slice proved the tweak plumbing (registry/service/BCD, toggle rows, toggle commands) but looks nothing like Vain Toolbox: separate nav pages with `ToggleSwitch` rows versus the original's single System screen with a General tab of expander cards and segmented Disabled/Enabled pickers. Worse, our Date & Time page covers ~15% of the original (NTP only) while the original manages timezone, locale, date/time formats, first day of week, live preview, and one-click resync.

This slice rebuilds the General tab to match the original's UI and behavior, driven end-to-end by the REA evidence gathered for it — no guessed backends.

The user's job this serves: open one tab that looks and acts like Vain Toolbox's General tab — flip real Windows settings without regedit, see formats previewed live, resync time in one click.

## 2. Goals

- **One General tab** on the System screen: header buttons (Restart Explorer, Vain Defaults, refresh), five expander cards (Explorer, Context Menu, Visual, Date & Time, Settings Page Visibility), four segmented toggles below (Manual Windows Updates, Print Service, Game Bar, Push Notifications). Performance/Security/Sound tabs exist as disabled placeholders.
- **Date & Time expander, complete:** TimeZoneCombo, LocaleCombo, Short/Long date/time combos with live example preview, FirstDayCombo, NTP list with Add/Edit/Remove, Apply NTP Servers, Sync Time Now — all backends per §7.
- **Four segmented toggles, exact backends** per §8 (Manual Updates, Print Service, Game Bar, Push Notifications).
- **Shared segmented control** (`Disabled | Enabled`) used by all four toggles; ToggleSwitch rows remain for the expander contents.
- **Existing expander contents reused** where they already match evidence (Context Menu 7 KeyValues rows); Explorer/Visual/SettingsVisibility contents stay as-is unless §9 says otherwise.

## 3. Non-Goals

- Performance, Security, Sound tabs (later slices).
- Vain Defaults bundle contents (separate REA investigation; button may confirm + apply only General-tab defaults if evidenced, else disabled with tooltip — builder's call, see §9).
- Pixel-perfect styling (fonts, exact spacing, starfield already exists); layout parity and control parity, not screenshot tracing.
- Pixel-exact segmented animation; a clean two-segment picker is enough.
- LPC controller (still no evidenced backend).

## 4. Anti-Goals

- Toggles that flip UI state without the evidenced backend behind them.
- Replacing working backends with prettier fakes to match the screenshot faster.
- Touching the homepage, theme system, or hardware detection except through existing interfaces.

## 5. Evidence ledger (what REA proved)

All from `Vain-Toolbox-general-tab.json` unless noted:

| UI | Backend (observed) |
|----|--------------------|
| TimeZoneCombo | Enumerate via `EnumDynamicTimeZoneInformation` family (imports observed); apply via `tzutil.exe <args>` through the `FUN_140066b60` CLI runner (`ev_ac2db90…`). Exact arg template unresolved — builder verifies `tzutil /s "<key>"` mapping on dev machine (residual unknown R-1). |
| LocaleCombo | `HKCU\Control Panel\International\LocaleName` string (`FUN_1402cac00`, `ev_b9f5242…`) |
| ShortDateCombo | `…\International\sShortDate`, 7 presets: `M/d/yyyy`, `M/d/yy`, `MM/dd/yy`, `MM/dd/yyyy`, `yy/MM/dd`, `yyyy-MM-dd`, `dd-MMM-yy` (`FUN_1402cad60`, `ev_7b8c3bb…` + preset reads) |
| LongDateCombo | `…\International\sLongDate`, 4 presets: `dddd, MMMM d, yyyy`, `MMMM d, yyyy`, `dddd, d MMMM yyyy`, `d MMMM yyyy` (`FUN_1402cae90`, `ev_12124bd…`) |
| ShortTimeCombo | `…\International\sShortTime`, 4 presets: `h:mm tt`, `hh:mm tt`, `H:mm`, `HH:mm` (`FUN_1402cafc0`, `ev_b4a5c11…`) |
| LongTimeCombo | `…\International\sTimeFormat` (NOT `sLongTime`), 4 presets: `h:mm:ss tt`, `hh:mm:ss tt`, `H:mm:ss`, `HH:mm:ss` (`FUN_1402cb0f0`, `ev_bce9920…`) |
| FirstDayCombo | `…\International\iFirstDayOfWeek` = combo index via `_itow_s` (`FUN_1402cb220`, `ev_8791d91…`) |
| Example texts | Computed preview via `GetLocaleInfoEx` formatting (import observed `ev_992baf9…`); values derived live, not stored |
| NTP list | `HKLM\…\W32Time\Parameters\NtpServer` (prior slice, kept) |
| Apply NTP | `w32tm /config /update` + W32Time restart (prior slice, kept) |
| Sync Time Now | `w32tm.exe /resync /force` via CLI runner (`FUN_1402cb690`, `ev_b1ae998…`) |
| Manual Updates (index 1 = Enabled) | `HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\SetDisableUXWUAccess` DWORD = (index != 1) (`FUN_1402cce60`, `ev_cae887c…`) |
| Print Service | Spooler start mode (prior slice, kept) |
| Game Bar (index 0 = Disabled) | Disable WRITES 8 values / enable DELETES them: `HKCU\…\GameBar\{GamePanelStartupTipIndex=3, ShowStartupPanel=0, UseNexusForGameBarEnabled=0}`, `HKCU\…\GameDVR\AppCaptureEnabled=0`, `HKLM\…\Policies\…\GameDVR\AllowGameDVR=0`, `HKLM\…\PolicyManager\…\AllowGameDVR\value=0`, `HKLM\…\PresenceWriter\ActivationType=0`, `HKCR\ms-gamebar` default=`URL:ms-gamebar` + `URL Protocol`="" + `NoOpenWith`="" (strings; DWORDs elsewhere) (`FUN_1400606b0`, `ev_c44a2bd…`) |
| Push Notifications (index 0 = Disabled) | Disable: `WpnService\Start=4` (if service present), ConsentStore `Value="Deny"`, `ToastEnabled=0`, `NoCloudApplicationNotification=1`, `ShowSyncProviderNotifications=0`, 3× `NOC_*=0`. Enable: `Start=2`, `Value="Allow"`, `ToastEnabled=1`, `NoCloud=0`, `ShowSync=1`, 3× `NOC_*` value-deleted (`FUN_1400609a0`, `ev_bb40955…`) |
| Restart Explorer | `taskkill /f /im explorer.exe` + relaunch (PROJECT.md `0x1402c8950`, kept) |
| XBF control names | `TimeZoneCombo`, `LocaleCombo`, `ShortDateCombo`, `LongDateCombo`, `ShortTimeCombo`, `LongTimeCombo`, `FirstDayCombo`, `*ExampleText`, `NtpListView`, `NtpInputBox`, `Add/Edit/RemoveNtpBtn`, `ApplyNtpBtn`, `DateTimeExpander`, `vaintoolboxc__.DateTimePage` (`VainToolbox-xbf-controls.txt`) |

Helper semantics (inferred from symmetric use, marked as inference): `FUN_140046e30(key, name, dword)` writes DWORD; `FUN_140046bb0(key, name, flag, data)` writes string (flag 1) or DWORD (flag 4); `FUN_14005aaf0(key, name)` deletes a value; `FUN_140066b60(exe, args)` runs a CLI command.

### 5.1 Exact paths (builder-check resolution; `HKCU`/`HKLM`/`HKCR` roots as shown)

Game Bar disable-writes (enable deletes each value):
- `HKCU\SOFTWARE\Microsoft\GameBar` : `GamePanelStartupTipIndex`=3, `ShowStartupPanel`=0, `UseNexusForGameBarEnabled`=0 (DWORD)
- `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR` : `AppCaptureEnabled`=0 (DWORD)
- `HKLM\SOFTWARE\Policies\Microsoft\Windows\GameDVR` : `AllowGameDVR`=0 (DWORD)
- `HKLM\SOFTWARE\Microsoft\PolicyManager\default\ApplicationManagement\AllowGameDVR` : `value`=0 (DWORD)
- `HKLM\SOFTWARE\Microsoft\WindowsRuntime\ActivatableClassId\Windows.Gaming.GameBar.PresenceServer.Internal.PresenceWriter` : `ActivationType`=0 (DWORD)
- `HKCR\ms-gamebar` : default=`URL:ms-gamebar`, `URL Protocol`="", `NoOpenWith`="" (REG_SZ)

Push Notifications disable-writes (enable values in §5 table):
- `HKLM\SYSTEM\CurrentControlSet\Services\WpnService` : `Start`=4 (DWORD; only if the service query returns nonzero; enable writes 2)
- `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userNotificationListener` : `Value`=`Deny` (enable: `Allow`)
- `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\PushNotifications` : `ToastEnabled`=0 (DWORD)
- `HKLM\SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\PushNotifications` : `NoCloudApplicationNotification`=1 (DWORD)
- `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced` : `ShowSyncProviderNotifications`=0 (DWORD)
- `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings` : `NOC_GLOBAL_SETTING_ALLOW_NOTIFICATION_SOUND`, `NOC_GLOBAL_SETTING_ALLOW_CRITICAL_TOASTS_ABOVE_LOCK`, `NOC_GLOBAL_SETTING_ALLOW_TOASTS_ABOVE_LOCK` =0 (DWORD; enable DELETES all three)

Multi-value atomicity (my call): best-effort sequential writes in table order, no cross-value rollback — this matches the original's straight-line code. First failure reverts the segmented index and surfaces the error (same revert contract as toggle rows).

## 6. UI design (from screenshots)

- System screen hosts a tab strip: General (active) + Performance / Security / Sound (disabled placeholders for later slices).
- General tab content: action row (Restart Explorer, Vain Defaults, refresh) then expander cards in order: Explorer, Context Menu, Visual, Date & Time, Settings Page Visibility; then the four segmented rows (Manual Windows Updates, Print Service, Game Bar, Push Notifications).
- Segmented row: label left, two-segment `Disabled | Enabled` picker right (index 0 = Disabled everywhere the evidence indexes it).
- Date & Time expander content order: Time zone combo; Format (locale) combo; Short date / Long date / Short time / Long time combos; First day of week combo; live preview block (short date, long date, short time, long time rendered in current locale); NTP Servers list; input + Add / Edit / Remove; Apply NTP Servers + Sync Time Now buttons.
- New `SegmentedSettingRow` control in `AkariTools.App/Controls`; expander cards via WinUI `Expander`.

## 7. Page specifications

### 7.1 System screen + General tab

- New `Views/System/SystemPage` with tab strip; General tab content as §6. Other tabs navigate nowhere yet (disabled).
- The six `Views/General/*Page.xaml(.cs)` hosts are retired (deleted); their ViewModels are kept and hosted directly as expander content (VMs are UI-agnostic; only the thin Page wrappers go). `GeneralPage`'s Hyper-V/Spooler/RDP content does NOT appear on the General tab (the original shows Manual Updates/Print Service/Game Bar/Push Notifications there instead); its ViewModel stays for a later slice.
- MainWindow navigator replaces the six General items with one System entry; DI and page cache follow the existing pattern.
- The header refresh button re-reads all General tab state (all expanders + segmented indices).
- Elevation: no new work — the app manifest already mandates `requireAdministrator` (PROJECT.md §20); all HKLM/service writes rely on it.

### 7.2 Date & Time (full)

- Commit model is immediate-on-select per combo (evidenced: the original has one handler function per combo, each writing its value on selection change — `FUN_1402cac00`, `FUN_1402cad60`, `FUN_1402cae90`, `FUN_1402cafc0`, `FUN_1402cb0f0`, `FUN_1402cb220`, `FUN_1402a0700`). No Apply button for combos; each failed write reverts its combo + surfaces error.
- After Intl registry writes, broadcast `WM_SETTINGCHANGE` so running apps pick up the new formats (my call — standard practice, not observed in the slices read).
- ViewModel owns: `TimeZones` (display + key), `SelectedTimeZone`, `Locales`, `SelectedLocale`, four format collections (exact presets from §5) + selections, `FirstDays` (0–6 with day names) + selection, preview strings (recomputed from selections via `CultureInfo`), NTP list (existing behavior: add/validate/remove/apply), `SyncTimeNowCommand` (`w32tm /resync /force` + result message).
- Timezone apply writes through `tzutil`; R-1 verified on dev machine during build.
- Edit NTP: selecting a list entry loads it into the input (XBF `EditNtpBtn`); saving replaces it (re-validated, duplicate check exempts the entry being edited).

### 7.3 Segmented toggles

- One shared pattern: `SelectedIndex` (0 Disabled / 1 Enabled) + apply command per §5 backends; failures revert the index + surface error (same revert contract as toggle rows).
- Manual Updates and Print Service reuse service/registry plumbing; Game Bar and Push Notifications are multi-value sets modeled like Context Menu KeyValues rows (new `ToggleKind` unaffected — segmented VMs hold their own write/delete lists).

### 7.4 Buttons

- Restart Explorer: existing taskkill + relaunch path (`taskkill /f /im explorer.exe`, then `ShellExecuteW("explorer.exe", "open")` per PROJECT.md `0x1402c8950`).
- Vain Defaults: confirm dialog with the original's literal header `The following settings will be changed to Vain defaults:` plus a blank line plus the runtime-built list of General-tab settings (`ev_d1c96a0…`); applies defaults for the General tab only in this slice (exact per-toggle defaults recorded in code from §5 enable-values); full per-tab bundle is a later REA item.

## 8. Shared plumbing (Framework)

- `ITimeZoneService` (enumerate `(DisplayName, KeyName)` + `SetTimeZone(key)` via `tzutil`) and `IDateFormatService` (locale list, format presets as data from §5, `CultureInfo` preview, Intl registry read/write) — UI-free, xUnit-testable with throwaway keys and tzutil arg-capture seam.
- `ISegmentedSetting` pattern or reuse of `SettingRowViewModel` with an index-based VM — builder's call (see §9); must keep the OneWay + explicit-value contract from the prior slice's review fixes.

## 9. Decisions made

| Decision | Answer |
|----------|--------|
| Scope | Whole General tab in one slice (shell + segmented toggles + full DateTime); expander contents for Explorer/Visual/SettingsVisibility stay as built |
| Expander contents | Reuse: Context Menu 7 rows, Explorer 4 rows, Visual rows, SettingsVisibility rows from prior slice; only DateTime is rebuilt |
| Segmented vs ToggleSwitch | New segmented control for the 4 direct toggles (matches original); expander internals keep ToggleSwitch rows |
| Nav items | Six standalone General pages collapse into the System screen; MainWindow nav shows System instead |
| Timezone template R-1 | Builder verifies `tzutil /s` mapping live; failure falls back to documenting the gap, not guessing |
| Vain Defaults | General-tab-only defaults in this slice; full bundle later |
| Shell tabs | Performance/Security/Sound render as disabled placeholders |
| Page hosts | Six `Views/General/*Page` wrappers retired; existing ViewModels hosted directly as expander content (my call — VMs are UI-agnostic) |
| Combo apply | Immediate-on-select, one handler per combo (evidenced); `WM_SETTINGCHANGE` broadcast after Intl writes (my call) |
| Multi-value writes | Best-effort sequential, index revert + error on first failure (matches original; my call) |
| Refresh button | Re-reads all General tab state (my call) |
| Elevation | No new work; `requireAdministrator` manifest already covers HKLM/service writes |

## 10. Builder calls

- Whether segmented VMs reuse `SettingRowViewModel` or get their own index-based base (must preserve OneWay + same-value guard + forced revert).
- Exact XAML layout within §6 (expander styling, tab strip control choice).
- Timezone display-name format in the combo (`(UTC+01:00) Name` mirroring the original's display strings).
- Locale list source (installed locales via `CultureInfo.GetCultures` vs curated list; original shows names like `italiano (Italia) (it-IT)`).
- The `Vain Defaults` per-toggle default values within the General tab.

## 11. Acceptance

- System screen shows the General tab matching §6 order; each expander opens to live (non-placeholder) state.
- Flipping any segmented toggle changes the real setting (spot-check against regedit/services), persists across restart, reverts with error on failure.
- Date & Time: changing timezone/format/first-day applies to Windows (spot-check `intl.cpl` + registry); preview block matches selections; Sync Time Now reports success/failure.
- `dotnet build` 0 errors; `dotnet test` all green (new tests included).

## 12. Residual unknowns (bounded)

- R-1: `tzutil` exact arg template (bytes at `DAT_140399aec` inconclusive); verify live, do not ship a guess.
- R-2: TimeZoneCombo population function (import observed, caller unresolved); standard `EnumDynamicTimeZoneInformation` enumeration is the documented path.
- R-3: Full Vain Defaults bundle (per-tab applier `FUN_1402dc490` identified, contents not extracted).
