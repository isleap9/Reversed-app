---
phase: 06
review: 06-REVIEW.md
titles: json
findings:
  - id: CR-01
    severity: critical
    disposition: open
    title: "Uninstall passes the entire registry command line to ShellExecute as a file name"
  - id: CR-02
    severity: critical
    disposition: open
    title: "DISM exit code 3010 (success, restart required) is reported as a failure"
  - id: CR-03
    severity: critical
    disposition: open
    title: "Store Enter-key search runs with a stale query; Installed Apps filter is not live"
  - id: WR-01
    severity: warning
    disposition: open
    title: "Provisioned packages are \"removed\" with the per-user remove API; `IsProvisioned` is ignored"
  - id: WR-02
    severity: warning
    disposition: open
    title: "Package enumeration is admin-only and one bad package aborts the whole list; tests depend on the machine"
  - id: WR-03
    severity: warning
    disposition: open
    title: "DISM feature name is spliced into a shell-style argument string"
  - id: WR-04
    severity: warning
    disposition: open
    title: "Genuine winget failures are reported as \"No apps found\""
  - id: WR-05
    severity: warning
    disposition: open
    title: "Query beginning with `-` is still parsed by winget as a switch; the T-06-09 claim is overstated"
  - id: WR-06
    severity: warning
    disposition: open
    title: "Uninstall result is never checked; success is reported unconditionally"
  - id: WR-07
    severity: warning
    disposition: open
    title: "No re-entrancy guard on Remove / Uninstall / Enable / Disable"
  - id: WR-08
    severity: warning
    disposition: open
    title: "`dism /Format:List` parser depends on English field labels"
  - id: WR-09
    severity: warning
    disposition: open
    title: "Install confirmation hides flags that auto-accept license agreements"
  - id: WR-10
    severity: warning
    disposition: open
    title: "Process output is decoded with the OEM code page; there is no timeout or cancellation"
  - id: IN-01
    severity: info
    disposition: open
    title: "Elevated execution of a command read from a user-writable hive"
  - id: IN-02
    severity: info
    disposition: open
    title: "`dism.exe` / `winget` resolved through the search path while elevated"
  - id: IN-03
    severity: info
    disposition: open
    title: "Uninstall list de-duplicates and lists by display name only"
  - id: IN-04
    severity: info
    disposition: open
    title: "Unused `_registry` field in all four view models"
  - id: IN-05
    severity: info
    disposition: open
    title: "Test organisation and wording"
open: 18
total: 18
---

# Phase 06: Code Review Disposition

| Finding | Severity | Disposition | Source |
|---------|----------|-------------|--------|
| CR-01 | critical | open | - |
| CR-02 | critical | open | - |
| CR-03 | critical | open | - |
| WR-01 | warning | open | - |
| WR-02 | warning | open | - |
| WR-03 | warning | open | - |
| WR-04 | warning | open | - |
| WR-05 | warning | open | - |
| WR-06 | warning | open | - |
| WR-07 | warning | open | - |
| WR-08 | warning | open | - |
| WR-09 | warning | open | - |
| WR-10 | warning | open | - |
| IN-01 | info | open | - |
| IN-02 | info | open | - |
| IN-03 | info | open | - |
| IN-04 | info | open | - |
| IN-05 | info | open | - |

Dispositions: `open` (recorded, not yet triaged), `fixed`, `skipped`, `deferred`.
