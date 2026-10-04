---
phase: 5
plan: 04
subsystem: Phase 5 gap remediation
tags: [volume-mixer, wasapi, affinity-rules, startup-delete, schtasks, dns, ntp]
key-files:
  created:
    - src/VainTools.Tests/SoundPageViewModelTests.cs
    - src/VainTools.Tests/SoundServiceTests.cs
    - src/VainTools.Tests/NetworkPageViewModelTests.cs
  modified:
    - src/VainTools.App/Services/IAffinityService.cs
    - src/VainTools.App/Services/AffinityService.cs
    - src/VainTools.App/Services/IStartupService.cs
    - src/VainTools.App/Services/StartupService.cs
    - src/VainTools.App/ViewModels/AffinityViewModel.cs
    - src/VainTools.App/ViewModels/StartupViewModel.cs
    - src/VainTools.App/ViewModels/SoundPageViewModel.cs
    - src/VainTools.App/Features/Affinity/AffinityPage.xaml
    - src/VainTools.App/Features/Startup/StartupPage.xaml
    - src/VainTools.App/Features/Sound/SoundPage.xaml
    - src/VainTools.App/Features/Sound/SoundPage.xaml.cs
    - src/VainTools.Tests/AffinityServiceTests.cs
    - src/VainTools.Tests/AffinityViewModelTests.cs
    - src/VainTools.Tests/StartupServiceTests.cs
    - src/VainTools.Tests/StartupViewModelTests.cs
metrics:
  tests_added: 40
  tests_total: 265
  build_warnings: 0
  build_errors: 0
---

# Plan 05-04 Summary: Phase 5 Gap Remediation

## What Was Built

- **SND-02 — Volume mixer.** `SoundPage` gains a Volume Mixer card (device
  `ComboBox`, 0–100 `Slider`, percent readout, mute `ToggleSwitch`) wired to the
  previously dead `ISoundService` volume API. `SoundPageViewModel` gains
  `SelectedDevice` / `VolumeLevel` / `IsMuted` / `VolumeText` plus
  `SelectDeviceCommand`; code-behind guards every handler with
  `IsUserVolumeChange` / `IsUserMuteChange` (compare against the last
  system-read state — the house anti-echo pattern).
- **AFF-03 — Affinity rule persistence.** `AffinityService` stores rules as QWORD
  masks per process name under `HKCU\SOFTWARE\VainTools\AffinityRules`
  (direct-registry, following the `StartupService` precedent). The key path is
  an optional constructor parameter so tests use a throwaway key. New surface:
  `GetRules` / `SaveRule` (rejects empty name, zero mask) / `DeleteRule`
  (throws when missing) / `ApplyRules` (reapplies to same-named running
  processes, counts successes). The page gains a Saved Rules card (reapply all,
  per-rule delete) and a Save-as-Rule button in the editor; deletes are
  `ConfirmAsync`-gated.
- **STR-03 — Delete startup entry.** `DeleteRunKeyEntry` (resolves plain or `-`
  prefixed value, deletes, throws when missing) and `DeleteScheduledTaskAsync`
  (`schtasks.exe /Delete /TN "<folder><name>" /F`). Both VM commands are
  confirmation-gated with refresh-on-success. Delete buttons added per row in
  both sections, routed via `ElementName=StartupRoot` — no code-behind needed
  (buttons bind commands directly, unlike toggles).
- **NET-02/NET-03 — Write paths pinned.** `NetworkPageViewModelTests` assert the
  exact HKLM paths and value names for both load and save. Live exercise still
  needs elevation; the VM already surfaces that via try/catch + InfoBar.

## Verification

- `dotnet build --no-incremental` — **0 warnings / 0 errors** (full solution,
  includes XAML markup compile)
- `dotnet test` — **265 passed / 0 failed** (40 new)
- Launch smoke: `VainTools.App.exe` starts, window "Vain Tools", Responding=True;
  process killed after the check, exe unlocked for the next build
- Registry discipline: all new registry-touching tests use throwaway keys or
  self-cleaning guid values with `finally` cleanup; the suite leaves no residue

## Deviations

- None from 05-04-PLAN.md. `VolumeText` (`{VolumeLevel:F0} %` with an
  `OnVolumeLevelChanged` notifier) was added during implementation so the
  readout shows whole percent instead of a raw double — display-only, no plan
  change.

## Self-Check: PASSED

- All three FAILs closed in UI + service + VM; both PARTIALs pinned by tests
- Binding-echo guards on all four new control handlers (ComboBox/Slider/Mute +
  pre-existing toggle patterns untouched)
- Destructive actions (rule delete, entry/task delete) behind confirmation
