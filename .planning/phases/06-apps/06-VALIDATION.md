---
phase: "06"
slug: "apps"
status: draft
nyquist_compliant: false
wave_0_complete: false
created: "2026-10-04"
---

# Phase 06 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.x |
| **Config file** | `tests/VainTools.Tests/VainTools.Tests.csproj` |
| **Quick run command** | `dotnet test tests/VainTools.Tests --filter "FullyQualifiedName~Phase6" --no-build` |
| **Full suite command** | `dotnet test tests/VainTools.Tests --no-build` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test tests/VainTools.Tests --filter "FullyQualifiedName~Phase6" --no-build`
- **After every plan wave:** Run `dotnet test tests/VainTools.Tests --no-build`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 06-01-01 | 01 | 1 | APPX-01 | T-06-01 | N/A | unit | `dotnet test --filter "FullyQualifiedName~AppxPackageService" --no-build` | ❌ W0 | ⬜ pending |
| 06-01-02 | 01 | 1 | APPX-02 | T-06-01 | N/A | unit | `dotnet test --filter "FullyQualifiedName~AppxPackageService" --no-build` | ❌ W0 | ⬜ pending |
| 06-01-03 | 01 | 1 | INST-01 | T-06-02 | N/A | unit | `dotnet test --filter "FullyQualifiedName~InstalledAppsService" --no-build` | ❌ W0 | ⬜ pending |
| 06-01-04 | 01 | 1 | INST-02 | T-06-02 | N/A | unit | `dotnet test --filter "FullyQualifiedName~InstalledAppsService" --no-build` | ❌ W0 | ⬜ pending |
| 06-01-05 | 01 | 1 | INST-03 | T-06-02 | N/A | unit | `dotnet test --filter "FullyQualifiedName~InstalledAppsService" --no-build` | ❌ W0 | ⬜ pending |
| 06-01-06 | 01 | 1 | OPT-01 | T-06-03 | N/A | unit | `dotnet test --filter "FullyQualifiedName~OptionalFeaturesService" --no-build` | ❌ W0 | ⬜ pending |
| 06-01-07 | 01 | 1 | OPT-02 | T-06-03 | N/A | unit | `dotnet test --filter "FullyQualifiedName~OptionalFeaturesService" --no-build` | ❌ W0 | ⬜ pending |
| 06-02-01 | 02 | 2 | STOR-01 | T-06-04 | N/A | unit | `dotnet test --filter "FullyQualifiedName~StoreService" --no-build` | ❌ W0 | ⬜ pending |
| 06-02-02 | 02 | 2 | STOR-02 | T-06-04 | N/A | unit | `dotnet test --filter "FullyQualifiedName~StoreService" --no-build` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/VainTools.Tests/Services/AppxPackageServiceTests.cs` — stubs for APPX-01, APPX-02
- [ ] `tests/VainTools.Tests/Services/InstalledAppsServiceTests.cs` — stubs for INST-01, INST-02, INST-03
- [ ] `tests/VainTools.Tests/Services/OptionalFeaturesServiceTests.cs` — stubs for OPT-01, OPT-02
- [ ] `tests/VainTools.Tests/Services/StoreServiceTests.cs` — stubs for STOR-01, STOR-02
- [ ] `tests/VainTools.Tests/ViewModels/AppxManagerViewModelTests.cs` — ViewModel tests
- [ ] `tests/VainTools.Tests/ViewModels/InstalledAppsViewModelTests.cs` — ViewModel tests
- [ ] `tests/VainTools.Tests/ViewModels/OptionalFeaturesViewModelTests.cs` — ViewModel tests
- [ ] `tests/VainTools.Tests/ViewModels/StoreViewModelTests.cs` — ViewModel tests

*If none: "Existing infrastructure covers all phase requirements."*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Appx package removal | APPX-02 | Requires admin + real packages | Run as admin, remove a provisioned package, verify it's gone |
| Program uninstall | INST-02 | Requires real program + uninstaller | Uninstall a test program, verify it launches uninstaller |
| Optional feature enable/disable | OPT-02 | Requires admin + DISM | Enable/disable a feature, verify system change |
| Store app install | STOR-02 | Requires winget + network | Install an app via winget, verify it appears in Start menu |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
