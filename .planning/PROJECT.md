# Vain Toolbox (WinUI 3 Rebuild)

## What This Is

A faithful WinUI 3 / C# reimplementation of **Vain Toolbox** — a native C++/WinRT
Windows tuning suite. The real application is a 30-page toolbox covering NVIDIA
driver (DRS) settings, EDID/display overrides, Windows system/privacy/performance
tweaks, app and driver management, and disk/device cleanup. This project recreates
that structure and feature set in managed code with MVVM and DI.

## Core Value

**The full Vain Toolbox feature surface, reimplemented in WinUI 3 with a faithful
navigation structure.** If everything else fails, a user opening the app must
recognise it as Vain Toolbox — same pages, same grouping, same capabilities.

## Requirements

### Validated

<!-- Nothing is validated yet: the previous milestone's features did not match the
     real product and were retired (see Key Decisions). -->

- ✓ Navigation shell with the real 28-page Vain Toolbox tree — Phase 1
- ✓ Home page with live machine summary; `.vain` profile import with validation — Phase 2
- ✓ Registry tweak engine plus the General and System pages (7 pages, 20 tweaks) — Phase 3

### Active

See `.planning/REQUIREMENTS.md` for the full REQ-ID list. Summary:

- Navigation shell with the real 30-page tree
- NVIDIA DRS settings editor (read + staged write + `.vain` import/export)
- Display / EDID override and custom-resolution editor
- General, System, Security, Performance, Power, Network, Sound, Affinity, Startup tweaks
- Apps: Appx Manager, Installed Apps, Optional Features, Store
- Tools: Device Cleaner, Drive Scanner, Driver Manager
- Home overview, About, tray-helper integration

### Out of Scope

| Exclusion | Reason |
|-----------|--------|
| Bit-for-bit UI cloning (icons, exact copy, branding assets) | Reverse-engineered tool; reproduce structure and function, not the author's proprietary assets |
| Native C++/WinRT rewrite | Deliberately reimplementing in managed C# with MVVM |
| Writing to `nvapi64.dll` undocumented surfaces beyond DRS | Only the documented DRS entry points are in scope |
| Multi-GPU / SLI-specific handling | v1 targets single-GPU systems |
| Telemetry, auto-update, license checks | Not part of the feature surface we're rebuilding |

## Context

**Ground truth** was recovered by static analysis of the shipped binaries and is
recorded in `.planning/research/VAIN-TOOLBOX-GROUND-TRUTH.md`. Key findings:

- `Vain Toolbox.exe` is **native C++/WinRT** (no CLR data directory, no `BSJB`/`mscorlib`
  strings, 725 `Microsoft.UI.Xaml` references, C++ mangled `vaintoolboxc__` names).
  It cannot be decompiled to C#.
- There are **three executables**: `Vain Toolbox.exe` (main, 30 pages),
  `Vain Tools.exe` (tray helper: screenshots + taskbar + latency),
  `Vain Governor UI.exe` (separate governor window, own rules engine).
- They coordinate over **named events** (`Local\VainGpuProfilesChanged`,
  `Local\VainToolsLatencyChanged`, `Local\VainToolsScreenshotChanged`,
  `Local\VainToolsTaskbarChanged`, `Local\VainShellReady`, …) and registry roots
  (`HKCU\Software\VainTools\{Misc,Screenshots,Taskbar}`, `HKCU\Software\VainGovernor\Profiles`).
- The flagship feature is an **NVIDIA DRS editor** over `nvapi64.dll` with staged
  changes, per-game profiles, and `.vain` XML export/import.

The previous milestone built six pages (Dashboard, GpuGovernor, Profiles,
SystemTweaks, Screenshots, Taskbar) from a *guessed* feature list. None of those
pages exist in the real product. That work is retired.

## Constraints

- **Tech stack**: .NET 10 (`net10.0-windows10.0.26100.0`), WinUI 3 / Windows App SDK
  2.3.1, CommunityToolkit.Mvvm, `Microsoft.Extensions.Hosting` DI. Central package
  management via `Directory.Packages.props`.
- **Base framework**: `VainTools.Framework` (from the reusable WinUI-3-MVVM-Framework)
  supplies navigation, dialogs, services, converters, behaviours, collections.
- **Elevation**: several features (EDID overrides, driver delete, some registry
  writes) require administrator. The app must detect and explain, never silently fail.
- **Platform**: Windows 10 10.0.26100+ / Windows 11, x64.
- **Safety**: DRS "restore all defaults" and driver deletion are irreversible —
  they require explicit confirmation per GSD `safety.always_confirm_destructive`.

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Reimplement in C#/WinUI 3 rather than port the native binary | Binary is native C++/WinRT; no decompilation path to C#; managed MVVM gives testability and the existing framework base | — Pending |
| Build the real 30-page navigation shell first, features second | Makes progress visible and prevents further drift from the real structure | — Pending |
| Retire the six guessed pages (Dashboard/GpuGovernor/Profiles/SystemTweaks/Screenshots/Taskbar) | Static analysis proved none exist in Vain Toolbox | — Pending |
| Model the GPU area on NVIDIA DRS + EDID, not NVML monitoring | The real app edits driver settings and display overrides; it does not poll GPU sensors | — Pending |
| Keep `.vain` XML profile format compatible | It is a documented on-disk contract (`ArrayOfProfile` with `Executeables`/`Settings`) users may have existing files for | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-10-03 after ground-truth reverse engineering and milestone reset*
