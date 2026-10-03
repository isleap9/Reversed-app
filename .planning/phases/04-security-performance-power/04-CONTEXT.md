# Phase 4: Security, Performance & Power - Context

**Gathered:** 2026-10-03
**Status:** Ready for planning

<domain>
## Phase Boundary

Implement three pages: Security (Defender/VBS/blocklist/Spectre), Performance (timer/MPO/GPU scheduling/working-set), and Power Editor (power plans, edit settings, apply/revert). All settings are registry-based except Power Editor which uses powercfg.exe. Reuse the Phase 3 RegistryTweakService + TweakList pattern for toggle pages; create a new PowerService for power plan editing.

</domain>

<decisions>
## Implementation Decisions

### Security Service Architecture
- **D-01:** Use the existing `RegistryTweakService` + `TweakCatalog` pattern for all Security toggles. The ground truth confirms all Security settings are registry keys: `VulnerableDriverBlocklistEnable`, `EnableVirtualizationBasedSecurity`, `FeatureSettingsOverride`, `FeatureSettingsOverrideMask`, and Defender toggles. No new service needed.
- **D-02:** No WMI queries for Defender status — the real app uses registry keys only. Display toggle states from registry reads.

### Performance & Power Service Architecture
- **D-03:** Use `RegistryTweakService` for Performance page toggles (timer resolution `SkipTickOverride`, MPO, GPU scheduling, working-set adjustment). These are all registry keys per ground truth.
- **D-04:** Create a new `PowerService` wrapping `powercfg.exe` for the Power Editor page. The ground truth confirms "powercfg-level editing" — list plans, edit settings, apply/revert. This is the only new service in Phase 4.

### Elevation & Safety Flow
- **D-05:** Reuse the Phase 3 elevation pattern exactly: `IsElevated` check, report "applied vs reboot-required", `RestartExplorerAsync` for shell tweaks. The ground truth confirms the real app uses `taskkill.exe /f /im explorer.exe` + relaunch for shell changes and "Requires administrator" for some operations. No new elevation service.

### Page Layout Strategy
- **D-06:** Use `TweakList` control for Security and Performance pages (they are toggle lists, consistent with Phase 3's 7 General/System pages).
- **D-07:** Create a custom layout for Power Editor page — plan selector + settings editor. The ground truth shows "powercfg-level editing" which needs a different UI pattern than a simple toggle list.

### Claude's Discretion
- Specific registry key paths and value names for Performance tweaks (exact `SkipTickOverride` path, MPO key, GPU scheduling key, working-set key) — researcher should confirm from ground truth or binary analysis.
- Power plan setting editor UI details — planner has flexibility on exact layout as long as it supports listing plans, editing settings, and apply/revert.
- Test strategy for PowerService (mocking powercfg.exe output) — planner decides approach.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Ground Truth
- `.planning/research/VAIN-TOOLBOX-GROUND-TRUTH.md` — Reverse engineering findings: real page structure, registry keys, feature descriptions. Source of truth for all implementation decisions.

### Requirements
- `.planning/REQUIREMENTS.md` — SEC-01 through SEC-05, PERF-01 through PERF-04, PWR-01 through PWR-03

### Roadmap
- `.planning/ROADMAP.md` — Phase 4 details, success criteria, plan breakdown

### Prior Phase Patterns
- `.planning/phases/03-general-system/03-PLAN.md` — RegistryTweakService + TweakCatalog + TweakList pattern to replicate

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Services/RegistryTweakService.cs` — Read/apply/revert registry tweaks, elevation detection, 64-bit-then-32-bit registry view fallback, `RemoveKeyWhenDisabled` for key-presence switches, `RestartExplorerAsync`
- `Services/TweakCatalog.cs` — Tweak definition model; pages supply title + tweak set
- `Controls/TweakList.xaml` — Shared presenter control for toggle lists
- `Models/RegistryTweak.cs` — Tweak model (key path, value name, type, default)

### Established Patterns
- RegistryTweakService + TweakCatalog + TweakList: one engine serving every toggle, pages supply title + tweak set
- Elevation detection: `IsElevated` check, report "applied vs reboot-required"
- Shell tweaks: `RestartExplorerAsync` (confirmed, never automatic)
- Test project references app project for service testing

### Integration Points
- `Features/Security/SecurityPage.xaml.cs` — Existing scaffold page, needs implementation
- `Features/Performance/PerformancePage.xaml.cs` — Existing scaffold page, needs implementation
- `Features/Powereditor/PowereditorPage.xaml.cs` — Existing scaffold page, needs implementation
- DI registration in `App.xaml.cs` — New PowerService needs registration

</code_context>

<specifics>
## Specific Ideas

- User wants to "copy everything from Vain Toolbox.exe" — follow the ground truth exactly for registry keys, feature names, and behavior.
- Power Editor is the only page needing a custom layout (not TweakList).
- PowerService is the only new service needed.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 4-Security, Performance & Power*
*Context gathered: 2026-10-03*
