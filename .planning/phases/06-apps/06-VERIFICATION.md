---
phase: 06-apps
verified: 2026-10-09T00:00:00Z
status: gaps_found
score: 7/9 must-haves verified
covered_files:
  - ".planning/phases/06-apps/06-01-PLAN.md"
  - ".planning/phases/06-apps/06-01-SUMMARY.md"
  - ".planning/phases/06-apps/06-02-PLAN.md"
  - ".planning/phases/06-apps/06-02-SUMMARY.md"
  - "src/VainTools.App/Features/Apps/InstalledAppsPage.xaml"
  - "src/VainTools.App/Features/Apps/StorePage.xaml"
  - "src/VainTools.App/Services/AppxPackageService.cs"
  - "src/VainTools.App/Services/OptionalFeaturesService.cs"
  - "src/VainTools.App/Services/StoreService.cs"
  - "src/VainTools.App/ViewModels/InstalledAppsViewModel.cs"
covered_digest: "v3:sha256:eb2a303be4819f705304b3185028a39a3c3c941caa7f7a2b9d5af52c7eec3bb5"
behavior_unverified: 0
overrides_applied: 0
gaps:
  - truth: "Installed Apps lists uninstall-registry programs with uninstall + copy-command (SC2; INST-02 'User can uninstall a program'; plan truth 'Uninstall executes via direct Process.Start on UninstallString')"
    status: failed
    reason: "Uninstall button cannot launch real uninstallers. RunUninstallAsync does new ProcessStartInfo(command){UseShellExecute=true}, putting the WHOLE registry command line in FileName. ShellExecute does not split arguments out of the file name. Reproduced on this machine: 'cmd.exe /c exit 0' and '\"C:\\Windows\\System32\\cmd.exe\" /c exit 0' both throw 'The system cannot find the file specified'; only a bare path with no arguments starts. On this machine only 2 of 293 uninstall entries are bare paths, so ~99% of Uninstall clicks (every MsiExec.exe /X{GUID}, every 'unins000.exe /SILENT') would end in an 'Uninstall failed' InfoBar. Every unit test overrides RunUninstallAsync (InstalledAppsViewModelTests.cs:317), and UAT never exercised a real uninstall, so nothing caught it."
    artifacts:
      - path: "src/VainTools.App/ViewModels/InstalledAppsViewModel.cs"
        issue: "RunUninstallAsync (lines ~211-219) passes full command line as ProcessStartInfo.FileName with UseShellExecute=true; exit code also discarded (WR-06)"
    missing:
      - "Launch the command line correctly: either cmd.exe /c <command> via ArgumentList with UseShellExecute=false, or split with CommandLineToArgvW and start the first token with the remaining args"
      - "Check exit code and surface nonzero as an error; fix the 'started ... has finished' wording"
      - "Add a non-mocked test that runs a real argument-bearing command (e.g. cmd /c exit 0) through the real RunUninstallAsync"
  - truth: "Optional Features lists features and can enable/disable them (SC3; OPT-02)"
    status: partial
    reason: "Listing works and the DISM calls are issued, but DISM exit code 3010 (success, restart required, returned because /NoRestart is passed) is treated as failure. For the most common features (WSL, Hyper-V, Windows Sandbox, .NET 3.5) the change is applied yet the user sees 'Enable failed', the list is not refreshed, and no restart notice is given. Features that need no reboot work."
    artifacts:
      - path: "src/VainTools.App/Services/OptionalFeaturesService.cs"
        issue: "EnableFeatureAsync/DisableFeatureAsync throw on any ExitCode != 0 (lines ~73-77, ~90-94)"
    missing:
      - "Treat exit 0 and 3010 as success; surface 'restart required' to the user (e.g. return a result record with RestartRequired)"
      - "Pass the feature name as a discrete argument (WR-03) instead of string interpolation"
deferred: []
advisory: []
---

# Phase 06: Apps Verification Report

**Phase Goal:** Implement app and package management across the four Apps sub-pages.
**Verified:** 2026-10-09
**Status:** gaps_found
**Re-verification:** No, initial verification

## Goal Achievement

### Observable Truths

| #  | Truth | Status | Evidence |
| -- | ----- | ------ | -------- |
| 1  | SC1: Appx Manager lists installed and provisioned packages and can remove them | VERIFIED (warning WR-01) | `AppxPackageService` uses WinRT `PackageManager.FindPackages/FindProvisionedPackages/RemovePackageAsync`; VM gates on elevation, confirms, reloads. Caveat: removal always calls per-user `RemovePackageAsync` and never deprovisions (research Pitfall 6 flagged this; D-02 locked the API choice). Not proven to fail, so not a gap, but see Human Verification note below. |
| 2  | SC2: Installed Apps lists uninstall-registry programs with uninstall + copy-command | FAILED | Listing (3 hives) and Copy Command are real and wired. Uninstall launch path is broken for argument-bearing commands (see gap 1; reproduced). |
| 3  | SC3: Optional Features lists features and can enable/disable them | FAILED (partial) | Listing parser real; enable/disable invoke `dism.exe`, but exit 3010 is misreported as failure (see gap 2). |
| 4  | SC4: Store page lists installable apps and can install one | VERIFIED (warnings CR-03, WR-04, WR-05) | `StoreService` runs `winget search`/`install` with discrete argv; live winget install passed in UAT. Search button path works. Enter-key search uses a stale `SearchQuery` (LostFocus binding), a UX defect, not a missing capability. |
| 5  | Appx removal uses RemovePackageAsync with elevation check (D-10/D-11) | VERIFIED | `AppxManagerViewModel.RemovePackageAsync` checks `IsElevated`, `ConfirmAsync`, calls service, `RefreshAsync` (D-13). |
| 6  | Installed Apps reads HKLM, HKLM\WOW6432Node, HKCU (D-03) | VERIFIED | `InstalledAppsService.UninstallKeyLocations` has exactly those three; blank-name skip and dedupe present. |
| 7  | Copy uninstall command copies raw string (D-04) | VERIFIED | `CopyToClipboardCore(app.UninstallString)` via DataPackage; guarded by `CanCopyCommand`. |
| 8  | Optional Features uses DISM API via P/Invoke (D-06/D-07) | VERIFIED (deviation accepted) | Uses `dism.exe` through `IProcessRunner`; plan line 376 explicitly permits this fallback and the code documents the choice. |
| 9  | All pages routed, DI-registered, custom layouts, partial properties | VERIFIED | `NavigationCatalog.cs:110-113`, `MainWindow.xaml:107-116`, `App.xaml.cs:112-137` register services/VMs/runner; VMs use `public partial` properties. |

**Score:** 7/9 truths verified (gap rows 2 and 3). 0 behavior-unverified.

### Required Artifacts

| Artifact | Status | Details |
| -------- | ------ | ------- |
| IAppxPackageService / AppxPackageService / AppxPackage | VERIFIED | Present, substantive, DI-registered |
| IInstalledAppsService / InstalledAppsService / InstalledApp | VERIFIED | Present, substantive, DI-registered |
| IOptionalFeaturesService / OptionalFeaturesService / OptionalFeature | VERIFIED | Present; exit-code handling gap |
| IStoreService / StoreService / StoreApp | VERIFIED | Present; winget parser against real output |
| Four ViewModels and four pages | VERIFIED | Present, nav-routed, DataContext set from DI |
| Unit tests (services + VMs) | PRESENT | Orchestrator reports 377 passed, 0 failed. Real uninstall launch path is untested (overridden). |

### Key Link Verification

| From | To | Status | Details |
| ---- | -- | ------ | ------- |
| AppxManagerPage -> VM -> IAppxPackageService -> PackageManager | WIRED | |
| InstalledAppsPage -> VM -> IInstalledAppsService -> Registry | WIRED | |
| InstalledAppsViewModel.UninstallCommand -> Process.Start | WIRED but BROKEN | Wired, but the launch contract fails for command lines with arguments |
| OptionalFeaturesPage -> VM -> service -> dism.exe | WIRED | Exit 3010 mishandled |
| StorePage -> StoreViewModel -> IStoreService -> winget | WIRED | |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
| -------- | ------- | ------ | ------ |
| ShellExecute with whole command line as FileName (the exact pattern in RunUninstallAsync) | `ProcessStartInfo("cmd.exe /c exit 0"){UseShellExecute=true}` | "The system cannot find the file specified" | FAIL |
| Same with quoted path plus args | `ProcessStartInfo("\"C:\\Windows\\System32\\cmd.exe\" /c exit 0")` | same error | FAIL |
| Bare path, no args | `ProcessStartInfo("C:\\Windows\\System32\\cmd.exe")` | starts | PASS |
| Share of real uninstall entries that are bare paths on this machine | registry scan | 2 of 293 | indicates ~99% affected |

(Check run in Windows PowerShell 5.1 / .NET Framework; .NET (Core) `Process.Start` with `UseShellExecute=true` calls the same `ShellExecuteEx` with the whole string as `lpFile`, so the behavior is the same.)

### Probe Execution

SKIPPED: no probes declared by the phase.

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
| ----------- | ----------- | ----------- | ------ | -------- |
| APPX-01 | 06-01 | List installed Appx/provisioned packages | SATISFIED | AppxPackageService enumeration, UAT pass |
| APPX-02 | 06-01 | Remove a provisioned package | SATISFIED (with warning) | RemovePackageAsync per D-02; no deprovisioning (WR-01); needs human confirmation on a provisioned-only package |
| INST-01 | 06-01 | List installed programs from uninstall registry | SATISFIED | 3-hive reader |
| INST-02 | 06-01 | Uninstall a program (quiet or normal) | BLOCKED | Launch path fails for argument-bearing commands (reproduced) |
| INST-03 | 06-01 | Copy a program's uninstall command | SATISFIED | Raw string to clipboard |
| OPT-01 | 06-01 | List Windows optional features | SATISFIED | DISM list parser (English-only labels, WR-08) |
| OPT-02 | 06-01 | Enable/disable an optional feature | PARTIAL | Works for no-reboot features; 3010 reported as failure |
| STOR-01 | 06-02 | Store page lists installable apps | SATISFIED | winget search + parser, UAT pass |
| STOR-02 | 06-02 | Install an app from Store page | SATISFIED | winget install, live UAT install |

All 9 phase requirement IDs appear in a PLAN frontmatter (06-01: APPX-01/02, INST-01/02/03, OPT-01/02; 06-02: STOR-01/02) and in REQUIREMENTS.md. No orphaned requirements.

### Bookkeeping Inconsistencies (non-blocking)

- ROADMAP.md still shows "1/2 plans executed" and 06-02 unchecked, though 06-02-SUMMARY.md exists and UAT covered the Store page.
- REQUIREMENTS.md leaves STOR-01/STOR-02 unchecked and the traceability table shows Phase 6 as "Pending" for all nine.
- 06-REVIEW-DISPOSITION.md lists all 18 review findings as `open`.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
| ---- | ---- | ------- | -------- | ------ |
| InstalledAppsViewModel.cs | ~211-219 | Whole command line as ShellExecute FileName; exit code discarded | BLOCKER | INST-02 broken |
| OptionalFeaturesService.cs | ~73-94 | Exit 3010 treated as failure; feature name interpolated into arg string | WARNING (gap) | OPT-02 misreports success |
| StorePage.xaml / InstalledAppsPage.xaml | 46-49 / 42-44 | `x:Bind TwoWay` on TextBox.Text defaults to LostFocus | WARNING | Enter-key Store search uses stale query; filter not live |
| StoreService.cs | 70-76 | Any nonzero winget exit returns empty list ("No apps found") | WARNING | Real failures hidden |
| AppxPackageService.cs | 49-57 | Per-user remove only; `IsProvisioned` ignored | WARNING | Provisioned removal semantics |
| TBD/FIXME/XXX debt markers | n/a | Not searched beyond reviewed files | n/a | none found in files read |

CR-03 assessment: does not defeat STOR-01 or INST-01; the Search button path works (confirmed by UAT) and the Installed Apps filter still works on focus loss. Fix is a one-attribute change but it is a quality defect, not a goal failure.

### Human Verification Required

Not blocking the verdict (gaps_found takes precedence), but once gaps are closed:

1. **Remove a provisioned-only package (APPX-02).** Test: as admin, pick a row marked Provisioned that is not installed for the current user and click Remove. Expected: package is deprovisioned or a clear error is shown. Why human: depends on live OS state; research Pitfall 6 predicts failure.
2. **Real uninstall (INST-02).** Test: as admin, uninstall a throwaway MSI or Inno Setup program through the UI. Expected: uninstaller launches and the list reloads without the entry. Why human: destructive and UAT never exercised it.
3. **Enable a reboot-requiring feature (OPT-02).** Test: enable e.g. Windows Sandbox. Expected: success plus restart notice.

### Gaps Summary

Two gaps block the phase goal.

1. **INST-02 uninstall is functionally broken** (BLOCKER). The goal for the Installed Apps page, "uninstall + copy-command", is not met for the Uninstall half. The code was verified wired and passing 377 tests only because every test overrides the one method that launches the process. I reproduced the failure: passing a command line with arguments as the ShellExecute file name fails with "file not found", and about 99% of uninstall registry entries on this machine contain arguments. UAT's pass on this feature was against a mocked launch.
2. **OPT-02 enable/disable misreports success** for reboot-required features because DISM exit 3010 is treated as an error.

Everything else (APPX-01, APPX-02 per locked decision D-02, INST-01, INST-03, OPT-01, STOR-01, STOR-02, navigation, DI, elevation + confirmation gating, reload after mutation) is verified in code. The remaining review findings (CR-03 and WR-01 to WR-10, IN-01 to IN-05) are quality or robustness concerns and are recommended for the closure plan or later triage but do not by themselves defeat a requirement. Suggested closure plan scope: fix CR-01 (with a real, unmocked launch test and exit-code handling), fix CR-02 (3010 plus restart notice, argv-based feature name), and take CR-03 (one attribute on two TextBoxes) at the same time.

---

_Verified: 2026-10-09_
_Verifier: Claude (gsd-verifier)_
