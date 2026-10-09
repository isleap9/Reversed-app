---
phase: 06-apps
verified: 2026-10-09T14:47:18Z
status: gaps_found
score: 5/6 must-haves verified
plan_coverage:
  - plan: 06-01
    requirements: [APPX-01, APPX-02, INST-01, INST-02, INST-03, OPT-01, OPT-02]
    status: gaps_found
  - plan: 06-02
    requirements: [STOR-01, STOR-02]
    status: passed
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
covered_digest: "v3:sha256:8d4690e292082d84c63de4ad60c77bab2118a47a06e2af949b927b592ca8ce42"
behavior_unverified: 1
human_verification:
  - test: "Navigate to each of Appx Manager, Installed Apps, Optional Features and Store in the running shell and confirm the page renders"
    expected: "Each nav item loads its page, header/subtitle/count text render, list rows show, elevation InfoBar shows when not elevated"
    why_human: "Requires launching the WinUI 3 shell and clicking nav items; headless build/test cannot exercise the visual tree"
gaps:
  - truth: "Real WinRT PackageManager enumeration is actually exercised by the committed test code"
    status: failed
    reason: "AppxPackageServiceTests assert NotEmpty against the live PackageManager, but every other assertion in the file is shape-only; the summary's claim that tests 'asserted non-empty live results' for FindProvisionedPackages is only partially in the committed code. The tests DO pass (I ran them), so the capability exists, but the specific claim of committed non-empty provisioned coverage is weaker than stated."
    artifacts:
      - path: src/VainTools.Tests/AppxPackageServiceTests.cs
        issue: "Committed coverage is NotEmpty for both installed and provisioned; not the fuller live-probe coverage the SUMMARY narrative implies"
    missing:
      - "Strengthen assertions if the stronger claim is to be relied upon"
---

# Phase 6: Apps Verification Report

**Phase Goal:** Implement app and package management across the four Apps sub-pages.
**Verified:** 2026-10-09T14:47:18Z
**Status:** gaps_found

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
| 6 | The three "never verified" items claimed unverified in the summaries are genuinely proven or correctly deferred | ⚠️ PARTIAL | Live enumeration for Appx/Installed Apps/Optional Features and live winget search **are** proven (app log above). Live DISM enable/disable and live winget install were **never executed** — correctly flagged as human judgment. Runtime navigation of the four pages was never observed — correctly flagged. See gap below and Human Verification. |

**Score:** 5/6 must-haves verified (1 present-but-behavior-unverified → human verification)

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

## Gaps

Only one gap, and it is a **claim-accuracy** gap rather than a capability gap:

** Gap 1 — the "non-empty live WinRT" claim is thinner than the SUMMARY implies.**
`AppxPackageServiceTests` does assert `NotEmpty` for both installed and provisioned
packages, and those tests pass on this machine, so the capability is real and exercised.
But the committed assertions are shape-only for everything else, and the 06-01 SUMMARY's
coverage entry cites `GetInstalledPackages_ReturnsNotEmpty / GetProvisionedPackages_MarksAllAsProvisioned`
— the first of those two method names does not exist in the file (the actual test is
`GetInstalledPackages_ReturnsList`). The cited reference is inaccurate.
**Impact:** low — no user-facing behavior is affected; the tests do cover enumeration.
**Fix:** correct the coverage reference, and optionally strengthen the installed-package
assertion beyond `NotEmpty`.

No code was missing, stubbed, or unwired. No anti-pattern blockers: the only `TBD/FIXME/XXX`
hits in the 34 changed files are the two legitimate `PlaceholderText="Search …"` attributes.

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
the threat model is implemented; the build is clean (0/0) and 377 tests pass. The remaining
items are runtime/visual and mutation-level checks that no headless run can cover, plus one
inaccurate coverage reference in the 06-01 SUMMARY.

---

_Verified: 2026-10-09T14:47:18Z_
_Verifier: the agent (gsd-verifier)_
