---
phase: "06"
slug: "apps"
status: verified
# threats_open = count of OPEN threats at or above workflow.security_block_on severity (the blocking gate)
threats_open: 0
asvs_level: 1
created: "2026-10-09"
---

# Phase 06 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| Registry → UI / Process.Start | Uninstall-registry values (written by any installer) are displayed and, on request, executed | UninstallString / QuietUninstallString — untrusted command lines |
| User input → winget CLI | Store search query and selected package id are passed to `winget.exe` | Free-text query, package id |
| winget / DISM stdout → UI | CLI output is parsed and rendered in lists and InfoBars | Package names, ids, versions, error text |
| App → OS (elevated) | Appx removal, DISM enable/disable, winget install, uninstallers modify system state | System configuration changes |
| winget → package sources | winget downloads manifests and installers | Network traffic (handled by winget over HTTPS) |

---

## Threat Register

| Threat ID | Category | Component | Severity | Disposition | Mitigation | Status |
|-----------|----------|-----------|----------|-------------|------------|--------|
| T-06-01 | Tampering | AppxManagerViewModel | medium | mitigate | `IsElevated` gate + `ConfirmAsync` before `RemovePackageAsync` (AppxManagerViewModel.cs:116–134); `CanRemovePackage` requires elevation | closed |
| T-06-02 | Tampering | InstalledAppsViewModel | medium | mitigate | Confirmation dialog shows the raw command; `SelectUninstallCommand` prefers QuietUninstallString (InstalledAppsViewModel.cs) | closed |
| T-06-03 | Elevation of Privilege | InstalledAppsViewModel | medium | mitigate | Uninstall gated on `IsElevated` then `ConfirmAsync` showing the command before `Process.Start` | closed |
| T-06-04 | Tampering | OptionalFeaturesViewModel | medium | mitigate | Elevation gate on Enable and Disable; `ConfirmAsync` on Disable (OptionalFeaturesViewModel.cs:108, 154–172) | closed |
| T-06-05 | Tampering | InstalledAppsService / UI | low | mitigate | Registry values rendered as plain-text TextBlocks; only executed via the explicit, confirmed uninstall path (T-06-02/03) | closed |
| T-06-06 | Tampering | StoreViewModel | medium | mitigate | Confirmation names app and package id before install (StoreViewModel.cs:139–142) | closed |
| T-06-07 | Information Disclosure | All Phase 6 ViewModels | low | mitigate | `_logger.LogError(ex, …)` for full detail; InfoBar shows short messages (local single-user desktop app) | closed |
| T-06-08 | Information Disclosure | InstalledAppsViewModel.CopyCommand | low | mitigate | Only the user-requested UninstallString is copied | closed |
| T-06-09 | Tampering | StoreService / ProcessRunner | high | mitigate | Query and id passed as discrete argv via `ProcessStartInfo.ArgumentList` (ProcessRunner.cs:35–48; StoreService.cs:66, 88–90); unit-tested | closed |
| T-06-10 | Tampering | StoreViewModel / StoreService | medium | mitigate | Confirmation with app + id; install by `--id … --exact` | closed |
| T-06-11 | Information Disclosure | Store page | low | mitigate | WinUI TextBlock renders plain text — no HTML/markup interpretation of winget output | closed |
| T-06-12 | Elevation of Privilege | StoreViewModel | medium | mitigate | `IsElevated` checked before confirm/install; `CanInstall` requires elevation | closed |
| T-06-13 | Tampering | winget transport | low | transfer | winget handles source transport over HTTPS; app passes only `--accept-source-agreements` / `--accept-package-agreements` | closed |
| T-06-14 | Information Disclosure | StoreViewModel | low | mitigate | Exceptions logged via ILogger; InfoBar shows a generic install-failed message; winget stderr surfaced for CLI failures | closed |

*Status: open · closed · open — below high threshold (non-blocking)*
*Severity: critical > high > medium > low — only open threats at or above workflow.security_block_on count toward threats_open*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

Severities were not recorded in the plan-time register; they were assigned at audit time.

---

## Accepted Risks Log

No accepted risks.

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-10-09 | 14 | 14 | 0 | orchestrator (ASVS L1 grep-depth; auditor skipped per short-circuit) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-10-09
