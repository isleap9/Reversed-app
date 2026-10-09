---
status: complete
phase: 06-apps
source: [06-01-SUMMARY.md, 06-02-SUMMARY.md]
started: 2026-10-09T14:22:17Z
updated: 2026-10-09T14:24:54.759Z
---

## Current Test

[testing complete]

## Tests

### 1. Apps pages open and load (Appx Manager, Installed Apps, Optional Features)
expected: Launch the app and click each of the three nav items. Appx Manager shows a list of installed packages (with provisioned packages available too). Installed Apps shows uninstall-registry programs with a working search box and Uninstall / Copy Command actions. Optional Features shows Windows features with their states and Enable/Disable actions. No page crashes, clips its list, or shows a blank area.
result: pass

### 2. Store page renders and searches
expected: Open the Store page. It shows a header with a Refresh button, an elevation InfoBar if not running as admin, a search box, and a scrollable app list card. Nothing is searched until you submit a query; submitting one (e.g. "7zip") shows a ProgressRing, then rows with name / id / version / source and an Install button.
result: pass

### 3. Live winget install
expected: Running as administrator, click Install on a small app (e.g. 7-Zip). A confirmation dialog names the app and its package id. Confirming runs winget and the InfoBar reports success (or shows winget's error message if it fails); the app is actually installed afterwards.
result: pass

### 4. Appx Package Service enumerates installed and provisioned packages via WinRT PackageManager
expected: Appx Package Service enumerates installed and provisioned packages via WinRT PackageManager
result: pass
source: automated
coverage_id: 06-01/D1

### 5. Appx package removal runs through RemovePackageAsync behind an elevation check, a confirmation dialog, and a list reload
expected: Appx package removal runs through RemovePackageAsync behind an elevation check, a confirmation dialog, and a list reload
result: pass
source: automated
coverage_id: 06-01/D2

### 6. Installed Apps Service reads programs from HKLM, HKLM\WOW6432Node and HKCU with blank-name skipping and case-insensitive dedupe
expected: Installed Apps Service reads programs from HKLM, HKLM\WOW6432Node and HKCU with blank-name skipping and case-insensitive dedupe
result: pass
source: automated
coverage_id: 06-01/D3

### 7. Uninstall executes via direct Process.Start on QuietUninstallString (or UninstallString)
expected: Uninstall executes via direct Process.Start on QuietUninstallString (or UninstallString) with no IProcessRunner
result: pass
source: automated
coverage_id: 06-01/D4

### 8. Copy Command copies the raw UninstallString verbatim to the clipboard
expected: Copy Command copies the raw UninstallString verbatim to the clipboard
result: pass
source: automated
coverage_id: 06-01/D5

### 9. Optional Features Service lists Windows optional features with their states
expected: Optional Features Service lists Windows optional features with their states
result: pass
source: automated
coverage_id: 06-01/D6

### 10. Optional Features enable/disable run through DISM behind elevation, with confirmation on Disable
expected: Optional Features enable and disable run through DISM behind elevation, with a confirmation dialog on the destructive Disable path
result: pass
source: automated
coverage_id: 06-01/D7

### 11. Store service searches installable apps through the winget CLI and parses name / id / version / source
expected: Store service searches installable apps through the winget CLI and parses the result into name / id / version / source
result: pass
source: automated
coverage_id: 06-02/D1

### 12. winget query and package id passed as discrete argv elements
expected: winget search query and install package id are passed as discrete argv elements, never concatenated into a shell string (T-06-09)
result: pass
source: automated
coverage_id: 06-02/D2

### 13. winget output parsed defensively
expected: Missing version and source, varying column widths, progress-bar noise, footers, empty output and non-zero exits all yield a usable list instead of throwing
result: pass
source: automated
coverage_id: 06-02/D3

### 14. Install gated on elevation, then confirmation naming app and id, then install by id with --exact
expected: Install is gated on elevation (D-10), then an explicit confirmation naming the app and its package id (D-11), then installs by id with --exact (T-06-10)
result: pass
source: automated
coverage_id: 06-02/D5

### 15. Install success, non-zero exit and exceptions surface through the InfoBar with detail logged
expected: Install success, winget non-zero exit and service exception each surface through the InfoBar with full detail logged via ILogger (D-12 / T-06-14)
result: pass
source: automated
coverage_id: 06-02/D6

## Summary

total: 15
passed: 15
issues: 0
pending: 0
skipped: 0

## Gaps

[none yet]
