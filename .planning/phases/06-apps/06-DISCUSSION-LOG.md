# Phase 6: Apps - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-10-04
**Phase:** 6-Apps
**Areas discussed:** Appx enumeration, Installed Apps data source, Optional Features mechanism, Store integration, Uninstall execution, Layout pattern, Elevation handling, Confirmation flows, Error/failure states, Store listing source, Refresh behavior

---

## Appx Package Enumeration Approach

| Option | Description | Selected |
|--------|-------------|----------|
| PowerShell via IProcessRunner | Consistent with Phase 5 NetworkService, easier to test | |
| WinRT PackageManager API | Native, no PowerShell dependency, matches real app | ✓ |
| You decide | Pick whichever is more faithful to the real app | |

**User's choice:** "since we are reverse engineering Vain Toolbox.exe lets just copy it 1:1"
**Notes:** User wants the WinRT `Windows.Management.Deployment.PackageManager` API — the same native API the real C++/WinRT app uses.

---

## Installed Apps Data Source & Enumeration

| Option | Description | Selected |
|--------|-------------|----------|
| Direct registry read (HKLM + WOW6432Node + HKCU) | Matches the real native app exactly | ✓ |
| PowerShell Get-ItemProperty | Same data, consistent with Phase 5 pattern | |
| You decide | Pick the most faithful approach | |

**User's choice:** "since we are reverse engineering Vain Toolbox.exe lets just copy it 1:1"
**Notes:** Direct registry read on all three uninstall key paths.

---

## Optional Features Mechanism

| Option | Description | Selected |
|--------|-------------|----------|
| PowerShell (Enable/Disable-WindowsOptionalFeature) | Consistent with Phase 5, easier to test | |
| DISM API via P/Invoke | Native, matches the real app's likely approach | ✓ |
| You decide | Pick the most faithful approach | |

**User's choice:** "since we are reverse engineering Vain Toolbox.exe lets just copy it 1:1"
**Notes:** DISM API via P/Invoke — the real native app likely uses DISM directly.

---

## Store Page Integration

| Option | Description | Selected |
|--------|-------------|----------|
| winget CLI | Matches the ground truth's 'winget-style' description | ✓ |
| MS Store API (Windows.Services.Store) | More native, but may not match 'winget-style' | |
| You decide | Pick the most faithful approach | |

**User's choice:** "since we are reverse engineering Vain Toolbox.exe lets just copy it 1:1"
**Notes:** winget CLI for listing and installing apps.

---

## Uninstall Execution Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Direct Process.Start on UninstallString | Matches the real native app, simplest | ✓ |
| IProcessRunner abstraction | Consistent with Phase 4/5, more testable | |
| You decide | Pick the most faithful approach | |

**User's choice:** "since we are reverse engineering Vain Toolbox.exe lets just copy it 1:1"
**Notes:** Direct `Process.Start` — the real native app just creates a process.

---

## Layout Pattern

| Option | Description | Selected |
|--------|-------------|----------|
| Follow Phase 5 custom layout pattern | Each page has its own XAML, consistent with established approach | ✓ |
| Create a shared list component | Reusable AppList control, less duplication but new abstraction | |
| You decide | Pick whichever is more faithful to the real app | |

**User's choice:** "since we are reverse engineering Vain Toolbox.exe lets just copy it 1:1"
**Notes:** Per-page custom XAML layouts following the Phase 5 pattern.

---

## Elevation Handling

| Option | Description | Selected |
|--------|-------------|----------|
| Follow Phase 3/4/5 pattern | IsElevated check, report "applied vs reboot-required" | ✓ |
| Different approach | Researcher identifies from ground truth | |

**User's choice:** "since we are reverse engineering Vain Toolbox.exe lets just copy it 1:1"
**Notes:** Follow the established elevation pattern.

---

## Confirmation Flows for Destructive Operations

| Option | Description | Selected |
|--------|-------------|----------|
| safety.always_confirm_destructive | Explicit confirmation before Appx removal and uninstall | ✓ |
| No confirmation | Match real app if it doesn't have confirmations | |

**User's choice:** "since we are reverse engineering Vain Toolbox.exe lets just copy it 1:1"
**Notes:** Project safety config requires confirmation for destructive operations.

---

## Error/Failure States

| Option | Description | Selected |
|--------|-------------|----------|
| Follow real app 1:1 | Researcher identifies from ground truth or binary analysis | ✓ |
| Standard error handling | Generic error messages | |

**User's choice:** "since we are reverse engineering Vain Toolbox.exe lets just copy it 1:1"
**Notes:** Researcher should identify exact failure state messages from ground truth.

---

## Store Listing Source

| Option | Description | Selected |
|--------|-------------|----------|
| winget search | Dynamic search results | ✓ |
| Curated/hardcoded list | Static list of apps | |

**User's choice:** "since we are reverse engineering Vain Toolbox.exe lets just copy it 1:1"
**Notes:** Researcher decides based on ground truth.

---

## Refresh Behavior After Mutations

| Option | Description | Selected |
|--------|-------------|----------|
| Automatic reload | List refreshes after removal/uninstall | ✓ |
| Manual refresh button | User triggers refresh | |
| Navigation-triggered reload | Reload on page navigation | |

**User's choice:** "since we are reverse engineering Vain Toolbox.exe lets just copy it 1:1"
**Notes:** Researcher confirms the real app's refresh behavior.

---

## Claude's Discretion

- Exact DISM API P/Invoke signatures and COM interop details
- winget CLI output parsing (search results, install progress)
- Error/failure state UI messages
- Store page listing mechanism
- Test strategy for WinRT PackageManager, DISM P/Invoke, and winget CLI mocking

## Deferred Ideas

None — discussion stayed within phase scope.