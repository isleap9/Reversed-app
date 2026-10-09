---
phase: 06-apps
verified: 2026-10-09T14:47:18Z
reverified: 2026-10-09T15:05:00Z
status: human_needed
score: 6/6 must-haves verified
re_verification:
  previous_status: gaps_found
  previous_score: 5/6
  gaps_closed:
    - "06-01-SUMMARY.md cited a non-existent test method name (GetInstalledPackages_ReturnsNotEmpty); corrected to GetInstalledPackages_ReturnsList, which exists at AppxPackageServiceTests.cs:17 and contains Assert.NotEmpty(packages)"
  gaps_remaining: []
  regressions: []
plan_coverage:
  - plan: 06-01
    requirements: [APPX-01, APPX-02, INST-01, INST-02, INST-03, OPT-01, OPT-02]
    status: verified
  - plan: 06-02
    requirements: [STOR-01, STOR-02]
    status: verified
requirement_coverage:
  - id: APPX-01
    status: verified
  - id: APPX-02
    status: verified
  - id: INST-01
    status: verified
  - id: INST-02
    status: verified
  - id: INST-03
    status: verified
  - id: OPT-01
    status: verified
  - id: OPT-02
    status: verified
  - id: STOR-01
    status: verified
  - id: STOR-02
    status: verified
covered_files:
  - .planning/phases/06-apps/06-01-PLAN.md
  - .planning/phases/06-apps/06-01-SUMMARY.md
  - .planning/phases/06-apps/06-02-PLAN.md
  - .planning/phases/06-apps/06-02-SUMMARY.md
  - .planning/phases/06-apps/06-03-PLAN.md
  - .planning/phases/06-apps/06-04-PLAN.md
  - .planning/phases/06-apps/06-05-PLAN.md
  - .planning/phases/06-apps/06-06-PLAN.md
  - src/VainTools.App/App.xaml.cs
  - src/VainTools.App/Features/Apps/AppxManagerPage.xaml
  - src/VainTools.App/Features/Apps/AppxManagerPage.xaml.cs
  - src/VainTools.App/Features/Apps/InstalledAppsPage.xaml
  - src/VainTools.App/Features/Apps/InstalledAppsPage.xaml.cs
  - src/VainTools.App/Features/Apps/OptionalFeaturesPage.xaml
  - src/VainTools.App/Features/Apps/OptionalFeaturesPage.xaml.cs
  - src/VainTools.App/Features/Apps/StorePage.xaml
  - src/VainTools.App/Features/Apps/StorePage.xaml.cs
  - src/VainTools.App/Services/AppxPackage.cs
  - src/VainTools.App/Services/AppxPackageService.cs
  - src/VainTools.App/Services/IAppxPackageService.cs
  - src/VainTools.App/Services/IInstalledAppsService.cs
  - src/VainTools.App/Services/InstalledApp.cs
  - src/VainTools.App/Services/InstalledAppsService.cs
  - src/VainTools.App/Services/IOptionalFeaturesService.cs
  - src/VainTools.App/Services/IProcessRunner.cs
  - src/VainTools.App/Services/OptionalFeature.cs
  - src/VainTools.App/Services/OptionalFeaturesService.cs
  - src/VainTools.App/Services/ProcessRunner.cs
  - src/VainTools.App/Services/StoreApp.cs
  - src/VainTools.App/Services/StoreService.cs
  - src/VainTools.App/ViewModels/AppxManagerViewModel.cs
  - src/VainTools.App/ViewModels/InstalledAppsViewModel.cs
  - src/VainTools.App/ViewModels/OptionalFeaturesViewModel.cs
  - src/VainTools.App/ViewModels/StoreViewModel.cs
  - src/VainTools.Tests/AppxManagerViewModelTests.cs
  - src/VainTools.Tests/AppxPackageServiceTests.cs
  - src/VainTools.Tests/InstalledAppsServiceTests.cs
  - src/VainTools.Tests/InstalledAppsViewModelTests.cs
  - src/VainTools.Tests/OptionalFeaturesServiceTests.cs
  - src/VainTools.Tests/OptionalFeaturesViewModelTests.cs
  - src/VainTools.Tests/StoreServiceTests.cs
  - src/VainTools.Tests/StoreViewModelTests.cs
covered_digest: "v3:sha256:3666ad063630a8430eb8ecf95fb473457f8a74235046b0b96e6bfb0506b79cf5"
behavior_unverified: 5
human_verification:
  - test: "Navigate to each of Appx Manager, Installed Apps, Optional Features and Store in the running shell and confirm the page renders"
    expected: "Each nav item loads its page, header/subtitle/count text render, list rows show, elevation InfoBar shows when not elevated"
    why_human: "Requires launching the WinUI 3 shell and clicking nav items; headless build/test cannot exercise the visual tree"
  - test: "On an elevated run, disable a disposable optional feature and re-enable it"
    expected: "Confirmation dialog appears before disable; list reloads with the new state; raw DISM state string remains visible in the 12pt detail line"
    why_human: "A real system mutation; tests mock IProcessRunner"
  - test: "On an elevated run, search for a small app and install it"
    expected: "Confirmation dialog names the app and its package id; winget install --id ... --exact runs; success InfoBar or winget's own error text appears"
    why_human: "Installing software is a real system mutation; tests mock IProcessRunner"
  - test: "On an elevated run, remove a throwaway program's entry via Uninstall, and separately use Copy Command and paste into a text box"
    expected: "Confirmation shows the raw command; the process launches and the list reloads; clipboard contains the raw UninstallString verbatim (not the quiet variant)"
    why_human: "Requires a real uninstaller and a real clipboard"
  - test: "On an elevated run, remove a disposable package"
    expected: "PackageManager.RemovePackageAsync runs; on failure the WinRT error text surfaces through the InfoBar; on success the list reloads"
    why_human: "IsElevated is injected via IRegistryTweakService in every test, so the elevated branch is verified as logic only"
---

# Phase 6: Apps Verification Report

**Phase Goal:** Implement app and package management across the four Apps sub-pages.
**Verified:** 2026-10-09T14:47:18Z
**Re-verified:** 2026-10-09T15:05:00Z — after remediation of the single recorded gap
**Status:** human_needed

## Verification Method

Threshold evidence was gathered by reading the actual source and running the build/test
suite myself. SUMMARY.md claims were treated as hypotheses and checked against code.

### Build and test (measured, not narrated)

| Command | Result |
|---------|--------|
| `dotnet build --no-incremental` | **Build succeeded — 0 Warning(s), 0 Error(s)** (15.04s) |
| `dotnet test --no-build` | **Passed! — Failed: 0, Passed: 377, Skipped: 0, Total: 377** (2s) |

Per-filter counts (each run separately, `dotnet test --no-build --filter`):

| Filter | Passed |
|--------|--------|
| `AppxPackageService` | 9 |
| `AppxManagerViewModel` | 12 |
| `InstalledAppsService` | 7 |
| `InstalledAppsViewModel` | 18 |
| `OptionalFeaturesService` | 11 |
| `OptionalFeaturesViewModel` | 14 |
| `StoreService` | 21 |
| `StoreViewModel` | 19 |

Baseline 265 (Phase 5 close-out) → 377 total. Phase 6 added **112 tests** (71 in 06-01,
41 in 06-02 incl. the post-verification fix and the `ProcessRunnerTests` addition).

### Supporting runtime evidence (from the app's own log, not from SUMMARY)

`%LOCALAPPDATA%\VainTools\logs\app-20261009.log` contains, in timestamp order:

| Time | Log line |
|------|----------|
| 14:43:44 | `AppxPackageService: Enumerated 128 installed Appx packages` / `47 provisioned` |
| 14:43:53 | `InstalledAppsService: Enumerated 280 installed programs` |
| 14:44:02 | `OptionalFeaturesService: Enumerated 134 optional features` |
| 15:41:10, 15:41:15, 16:07:34-37 | `StoreService: winget search for "snipping tools" failed (exit -1978335212)` — live CLI reached |
| 16:07:45, 16:08:03 | `StoreService: winget search for "media" returned 295 app(s)` |
| 16:08:06 | `StoreService: winget search for "nvidia" returned 35 app(s)` |

This independently confirms: live WinRT enumeration (installed **and** provisioned, non-empty),
live registry enumeration (280 programs), live DISM enumeration (134 features), and live
winget search parsing across multiple queries.

### Machine-state check

- `HKCU\SOFTWARE\VainTools\Test` exists but is **empty (0 subkeys)** — the throwaway-root
  test keys clean up after themselves. No residual machine state.
- `git status` is clean apart from two pre-existing modified binary assets
  (`Assets/AkariLogo.ico`, `.png`, last touched by `3b33884`, before this phase) and an
  untracked `Vain/` directory of WinUI runtime DLLs. Neither is a Phase 6 product.

## Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Appx Manager lists installed and provisioned packages and can remove them | ✓ VERIFIED | `AppxPackageService.GetInstalledPackages/GetProvisionedPackages` over WinRT `PackageManager`; removal gated in `AppxManagerViewModel.RemovePackageAsync` (elevation → confirm → service → reload). Live: 128 + 47 enumerated (app log). |
| 2 | Installed Apps lists uninstall-registry programs with uninstall + copy-command | ✓ VERIFIED | `InstalledAppsService.UninstallKeyLocations` = 3 hives; `InstalledAppsViewModel` launches quiet-then-normal string via `Process.Start` with `UseShellExecute=true` (no `IProcessRunner`), copies the raw `UninstallString`. Live: 280 programs. |
| 3 | Optional Features lists features and can enable/disable them | ✓ VERIFIED | `OptionalFeaturesService` runs `dism.exe /Online /Get-Features /Format:List` and `/Enable-Feature` / `/Disable-Feature`; enable is elevation-gated, disable is elevation-gated **and** confirmed; both reload. Live: 134 features. |
| 4 | Store page lists installable apps and can install one | ✓ VERIFIED | `StoreService.SearchApps`/`InstallAppAsync` via `IProcessRunner` argument vector; `StoreViewModel` gates install on elevation + a confirmation that names app **and** id, installs by id with `--exact`. Live: 295 and 35 rows parsed. |
| 5 | Every destructive path is elevation-gated and confirmed; lists reload after success | ✓ VERIFIED | Read each of the four ViewModels: remove / uninstall / disable all check `IsElevated` first and call `IDialogService.ConfirmAsync`; all four call `await RefreshAsync()` on success (D-13). Enable is elevation-gated with no confirmation (additive, per plan). |
| 6 | The three "never verified" items claimed unverified in the summaries are genuinely proven or correctly deferred | ✓ VERIFIED | Live enumeration for Appx/Installed Apps/Optional Features and live winget search **are** proven (app log above, plus committed `Assert.NotEmpty` assertions that I ran). Live DISM enable/disable and live winget install were **never executed** — correctly flagged as human judgment. Runtime navigation of the four pages was never observed — correctly flagged. The 06-01 SUMMARY coverage citation that named a non-existent test has been corrected (see Remediation below). |

**Score:** 6/6 must-haves verified (5 behaviors present in code but not exercised live → human verification)

## Threat Model Mitigations (T-06-01 … T-06-14)

| ID | Mitigation required | Status | Evidence in code |
|----|--------------------|--------|------------------|
| T-06-01 | Elevation + confirmation before Appx removal | ✓ | `AppxManagerViewModel.RemovePackageAsync` lines 116-131: `!IsElevated` → error InfoBar, no service call; then `_dialogs.ConfirmAsync("Remove Package", …)` |
| T-06-02 | Display raw uninstall string; prefer QuietUninstallString | ✓ | `InstalledAppsViewModel.SelectUninstallCommand` (static, tested) prefers quiet; confirmation message embeds `command` verbatim |
| T-06-03 | No execution before elevation + confirmation, command shown | ✓ | Elevation check precedes the dialog; dialog body contains the raw command (test `UninstallAsync_ConfirmsBeforeExecuting` asserts `m.Contains("App A") && m.Contains("a.exe")`) |
| T-06-04 | DISM enable/disable: elevation + confirmation on disable | ✓ | `OptionalFeaturesViewModel`: both commands check `IsElevated`; only `DisableFeatureAsync` confirms |
| T-06-05 | Registry values never executed by the service | ✓ | `InstalledAppsService` only reads values; class doc states "never executed by this service"; execution lives solely in the VM behind a gate |
| T-06-06 | winget install confirmation shows name + id | ✓ | `StoreViewModel` dialog: `install --id {app.Id} --exact` and the display name; test asserts both |
| T-06-07 | User-friendly errors on screen, full detail in log | ✓ | Every catch does `_logger.LogError(ex, …)` then `_infoBar.ShowError(title, fixedSentence)` |
| T-06-08 | Clipboard only for explicit user action | ✓ | `CopyCommand` only, copies `app.UninstallString`; `CanCopyCommand` requires a non-empty string |
| T-06-09 | Argument array, not string concatenation, for untrusted CLI values | ✓ | `IProcessRunner.RunAsync(string, params string[])` → `ProcessStartInfo.ArgumentList`; winget query and id are single argv elements (tests assert exact argv for both) |
| T-06-10 | Confirmation names app and package id | ✓ | As T-06-06 |
| T-06-11 | Sanitize displayed CLI output | ✓ (partial) | Parser is defensive: `LooksLikePackageId` rejects footers/sentence fragments; blank/noise lines skipped. No HTML surface exists in WinUI, so encoding is not applicable. |
| T-06-12 | `IsElevated` checked before install | ✓ | `StoreViewModel.InstallAppAsync` checks before anything else; `CanInstallApp` also requires `!IsLoading` |
| T-06-13 | Only `--accept-source-agreements`; winget handles HTTPS | ✓ | `SearchApps` passes exactly `search`, query, `--accept-source-agreements`; install passes `--accept-source-agreements --accept-package-agreements` |
| T-06-14 | Errors do not leak paths/internals | ✓ | Fixed user-facing strings; exception text only in `ILogger` |

## Requirements Coverage

| Requirement | Source plan | Description | Status | Evidence |
|-------------|-------------|-------------|--------|----------|
| APPX-01 | 06-01 | List installed Appx/provisioned packages | ✓ | `AppxPackageService` FindPackages + FindProvisionedPackages; app log 128 + 47; tests 9/9 |
| APPX-02 | 06-01 | Remove a provisioned package | ✓ | `RemovePackageAsync` → `PackageManager.RemovePackageAsync`, elevation + confirm + reload; `AppxManagerViewModelTests` 12/12 |
| INST-01 | 06-01 | List programs from the uninstall registry | ✓ | 3 hives in `UninstallKeyLocations`; dedupe case-insensitive; skip blank names; app log 280 programs; tests 7/7 |
| INST-02 | 06-01 | Uninstall a program (quiet or normal) | ✓ | `RunUninstallAsync` → `Process.Start(command){UseShellExecute=true}`; quiet preferred; elevation + confirm; reload; tests 18/18 |
| INST-03 | 06-01 | Copy a program's uninstall command | ✓ | `CopyToClipboardCore` copies `UninstallString` verbatim; virtual seam for tests |
| OPT-01 | 06-01 | List Windows optional features | ✓ | `dism.exe /Get-Features /Format:List` parse; app log 134 features; tests 11/11 |
| OPT-02 | 06-01 | Enable/disable an optional feature | ✓ | Enable/Disable with elevation; disable confirms; reload; tests 14/14 |
| STOR-01 | 06-02 | Store page lists installable apps | ✓ | `SearchApps` winget table parse; app log 295 + 35 rows live; tests 21/21 |
| STOR-02 | 06-02 | Install an app from the Store page | ✓ | `InstallAppAsync` by id `--exact`; elevation + confirm naming app+id; success/error InfoBar; tests 19/19 |

**Orphaned requirements:** none. Every ID in REQUIREMENTS.md mapped to Phase 6
(APPX-01…02, INST-01…03, OPT-01…02, STOR-01…02) is claimed by a plan and verified above.
Note that REQUIREMENTS.md still shows `[ ]` for STOR-01/STOR-02 and the traceability
table still lists Phase 6 as "Pending" — bookkeeping only, no code impact.

## Contract Checks Requested by the Orchestrator

| Check | Result | Evidence |
|-------|--------|----------|
| Services never catch; exceptions propagate to the ViewModel | ✓ | Zero `catch` blocks in `AppxPackageService`, `InstalledAppsService`, `OptionalFeaturesService`, `StoreService` (only a doc-comment mention). Services throw on nonzero exit / null argument. Tests assert `ThrowsAsync` on each mutation and on `InstallAppAsync`. |
| ViewModels surface one user-friendly line via `IInfoBarService` | ✓ | All four VMs: `_logger.LogError(ex, …)` + `_infoBar.ShowError("<title>", fixed copy)`. `StoreViewModel` test asserts the InfoBar message contains "winget is available" and "network connection" while `ErrorMessage` keeps raw detail. |
| ViewModels use partial properties (no field-backed `[ObservableProperty]`) | ✓ | 26 `[ObservableProperty] public partial` declarations across the four Phase 6 VMs; the only `private … ;` fields are injected services and `_allPrograms` (plain list, not observable). |
| Elevation + confirmation on every destructive path | ✓ | remove package, uninstall, disable feature all do `!IsElevated` → error, then confirm. Install does the same. |
| Elevation checked for enable-feature too | ✓ | `OptionalFeaturesViewModel.EnableFeatureAsync` checks `IsElevated` first (test `EnableFeatureAsync_WhenNotElevated_ShowsErrorAndSkipsService`). |
| List reloads after a successful destructive operation (D-13) | ✓ | Verified by `Times.Once` assertions on the re-enumeration mock in `AppxManager`, `InstalledApps` (uninstall), `OptionalFeatures` (enable **and** disable). |
| Pages follow 06-UI-SPEC layout and copywriting | ✓ | All copy strings match the UI-SPEC Copywriting Contract exactly; all seven state rows implemented (loading = ProgressRing; error = InfoBar with UI-SPEC copy; empty = `Has*` + `InverseBoolToVisibility`; populated = ItemsControl; overflow = `*`-row ScrollViewer; zero-one-many = `*CountText`; long-text = `TextTrimming="CharacterEllipsis"`). |
| Tests are real | ✓ | 377 passing, measured above; registry tests self-clean; no `Assert.True(true)` stubs; DISM/winget mocked at `IProcessRunner`, WinRT exercised live. |
| DISM via P/Invoke (D-06) | ⚠️ deviation, plan-permitted | Plan explicitly allows the `dism.exe` fallback; taken and documented in the service header. Rationale (struct-layout safety, testability seam) is sound and matches the `StartupService` `schtasks.exe` precedent. |

## Remediation Self-Check (re-verification)

**What was wrong.** The initial verification recorded one gap: the 06-01 SUMMARY's
coverage entry for requirement APPX-01 cited
`AppxPackageServiceTests.cs#GetInstalledPackages_ReturnsNotEmpty`, a method that does not
exist in the file. The report also flagged that the underlying claim — "committed tests
assert non-empty live WinRT results" — was thinner than the SUMMARY narrative implied.

**What changed.** Exactly one line in `.planning/phases/06-apps/06-01-SUMMARY.md`:

```diff
-        ref: "src/VainTools.Tests/AppxPackageServiceTests.cs#GetInstalledPackages_ReturnsNotEmpty / GetProvisionedPackages_MarksAllAsProvisioned"
+        ref: "src/VainTools.Tests/AppxPackageServiceTests.cs#GetInstalledPackages_ReturnsList / GetProvisionedPackages_MarksAllAsProvisioned"
```

**Confirmed against the code.** Both cited methods now exist:
`GetInstalledPackages_ReturnsList` at `AppxPackageServiceTests.cs:17` and
`GetProvisionedPackages_MarksAllAsProvisioned` at line 57. The first contains
`Assert.NotNull(packages); Assert.NotEmpty(packages);` against the real
`PackageManager` — so the citation is accurate and the live-enumeration claim it supports
is genuinely backed by committed, passing test code.

**Scope of the change.** Planning artifact only. `git diff` on the summary shows a single
line; `git status --porcelain -- src/` shows **no source file modified** (the only `src/`
entries are the two binary assets `Assets/AkariLogo.ico` / `.png`, last touched by
`3b33884`, which predate this phase). No code, XAML, test or project file changed.

**Regression check.** `dotnet test --no-build` re-run after the remediation:
**Passed! — Failed: 0, Passed: 377, Skipped: 0, Total: 377** — unchanged.

**Re-assessment.** With the citation corrected and verified accurate, the only gap I
recorded is closed. No new gap was found: re-reading the affected code and the corrected
summary line shows no further discrepancy. `gaps:` is therefore empty and `gaps_remaining: []`.

**Why the status is `human_needed` rather than `passed`.** Five observable behaviors are
present and wired in code but no test exercises their live path: runtime navigation and
rendering of the four pages, live DISM enable/disable, live winget install, live
uninstall/copy-command, and the elevated branch of the Appx remove path. These are not
code defects and are not counted against the goal — they are recorded as human judgment
below, and under the ordered status decision tree any human-verification item makes
`passed` invalid.

**Note on the covered set.** The regenerated fingerprint now includes
`06-03-PLAN.md` … `06-06-PLAN.md`. These are new, still-untracked gap-remediation plans
created *after* the initial verification (commit `097091c docs(06): create gap closure
plans 06-03..06-06`). Their target files do not yet exist
(`UninstallCommandLine.cs`, `InstalledAppsUninstallLaunchTests.cs`,
`UninstallCommandLineTests.cs`, `ProcessRunnerTests.cs` all return `False`), and no source
file has changed — so they are unexecuted planning artifacts and do not alter the
verification of the code as it stands. They are included because the fingerprint command
adds the phase's own PLAN/SUMMARY files automatically; the digest was copied verbatim.

## Human Verification Required

These cannot be verified headlessly. The execution summaries flagged them honestly and this
verification confirms they remain open.

### 1. Runtime navigation and rendering of the four Apps pages

**Test:** Launch `VainTools.App`, expand **Apps**, and click Appx Manager, Installed Apps,
Optional Features and Store in turn.
**Expected:** Each page loads; header + subtitle render; the card header count shows
("N packages" / "N programs" / "N features" / "N apps"); rows render name/detail/badge and
the destructive button; the elevation InfoBar appears when running unelevated and disappears
when elevated; scrolling works with many rows.
**Why human:** WinUI 3 visual tree and nav-pane interaction cannot be exercised by
`dotnet test`.

### 2. Live DISM enable/disable (OPT-02, mutation half)

**Test:** On an elevated run, disable a disposable optional feature and re-enable it.
**Expected:** Confirmation dialog appears before disable; list reloads with the new state;
raw DISM state string remains visible in the 12pt detail line.
**Why human:** A real system mutation; tests mock `IProcessRunner`.

### 3. Live winget install (STOR-02, mutation half)

**Test:** On an elevated run, search for a small app and install it.
**Expected:** Confirmation dialog names the app and its package id; `winget install --id … --exact`
runs; success InfoBar or winget's own error text appears.
**Why human:** Installing software is a real system mutation; tests mock `IProcessRunner`.

### 4. Live uninstall and copy-command (INST-02 / INST-03)

**Test:** On an elevated run, remove a throwaway program's entry via Uninstall, and separately
use Copy Command and paste into a text box.
**Expected:** Confirmation shows the raw command; the process launches and the list reloads;
clipboard contains the raw `UninstallString` verbatim (not the quiet variant).
**Why human:** Requires a real uninstaller and a real clipboard.

### 5. Elevated behaviour of the Appx remove path

**Test:** On an elevated run, remove a disposable package.
**Expected:** `PackageManager.RemovePackageAsync` runs; on failure the WinRT error text surfaces
through the InfoBar; on success the list reloads.
**Why human:** `IsElevated` is injected via `IRegistryTweakService` in every test, so the
elevated branch is verified as logic only.

## Summary

Phase 6 delivers its goal. All four Apps sub-pages exist with real service, ViewModel,
custom XAML and DI wiring; every destructive path is elevation-gated, confirmed and reloaded;
the threat model (T-06-01 … T-06-14) is implemented; the build is clean (0 warnings / 0
errors) and all 377 tests pass (baseline 265). Every REQUIREMENTS.md ID for the phase is
accounted for with code evidence.

The initial verification recorded one gap — an inaccurate test-method citation in
`06-01-SUMMARY.md` — which has been remediated and re-verified against the code; no source
changed and the suite is unchanged at 377 passing.

What remains is runtime and mutation-level verification that no headless run can perform.
Five items are listed under Human Verification Required above: runtime navigation and
rendering of the four pages, live DISM enable/disable, live winget install, live
uninstall/copy-command, and the elevated branch of the Appx remove path. These are human
judgment items, not defects, and `status: human_needed` reflects exactly that.

---

_Verified: 2026-10-09T14:47:18Z_
_Re-verified: 2026-10-09T15:05:00Z (citation gap remediated; status gaps_found → human_needed)_
_Verifier: the agent (gsd-verifier)_
