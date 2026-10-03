# Phase 5: Network, Sound, Affinity & Startup - Context

**Gathered:** 2026-10-03
**Status:** Ready for planning

<domain>
## Phase Boundary

Implement four pages: Network (adapters, DNS, NTP, offloads), Sound (devices, spatial audio, enhancements), Affinity (CPU affinity for processes), and Startup (Run-key entries, scheduled tasks). Network offload toggles use the existing TweakList pattern; Sound, Affinity, and Startup need custom layouts. New services required for each page.

</domain>

<decisions>
## Implementation Decisions

### Network Service Architecture
- **D-01:** Use PowerShell for `Get-NetAdapterBinding` (adapter list, offload toggles) — matches ground truth exactly. The real app uses `Get-NetAdapterBinding` for NIC offloads.
- **D-02:** Use registry for DNS/NTP server settings — these are registry keys under `HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters` and `HKLM\SYSTEM\CurrentControlSet\Services\W32Time\Parameters`.
- **D-03:** Network offload toggles (`*TCPChecksumOffloadIPv4`, `*QoSOffload`, `*PMWiFiRekeyOffload`) use the existing `RegistryTweakService` + `TweakCatalog` + `TweakList` pattern — they are registry-based toggles.
- **D-04:** Create a new `NetworkService` wrapping PowerShell for adapter enumeration and offload toggling. This is the only new service for the Network page.

### Sound Service Architecture
- **D-05:** Use WASAPI (COM) for audio device enumeration and volume control — matches ground truth for volume mixer and spatial audio.
- **D-06:** Use registry for spatial audio and audio enhancement toggles — these are registry-based settings.
- **D-07:** Create a new `SoundService` wrapping WASAPI for device enumeration and volume, registry for toggles. This is a new service for the Sound page.

### Affinity Service Architecture
- **D-08:** Use P/Invoke for `SetProcessAffinityMask` and `GetProcessAffinityMask` — matches ground truth for "Adaptive ideal processor sets."
- **D-09:** Create a new `AffinityService` wrapping P/Invoke for CPU affinity. This is a new service for the Affinity page.

### Startup Service Architecture
- **D-10:** Use registry for Run-key entries (`HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`, `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`) — matches ground truth.
- **D-11:** Use WMI for scheduled tasks enumeration and management — matches ground truth.
- **D-12:** Create a new `StartupService` wrapping registry (Run-key) and WMI (scheduled tasks). This is a new service for the Startup page.

### Page Layout Strategy
- **D-13:** Use `TweakList` for Network page (offload toggles are registry-based, consistent with Phases 3-4).
- **D-14:** Create custom layouts for Sound (device list + volume controls), Affinity (process list + affinity editor), and Startup (entry list grouped by source). These pages need different UI patterns than a simple toggle list.

### Claude's Discretion
- Exact registry paths and value names for DNS/NTP settings — researcher should confirm from ground truth or binary analysis.
- WASAPI COM interop details — researcher should provide concrete implementation approach.
- P/Invoke signatures for SetProcessAffinityMask — researcher should provide exact signatures.
- WMI query for scheduled tasks — researcher should provide exact query.
- Test strategy for new services (mocking PowerShell, WASAPI, P/Invoke, WMI) — planner decides approach.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Ground Truth
- `.planning/research/VAIN-TOOLBOX-GROUND-TRUTH.md` — Reverse engineering findings: real page structure, registry keys, feature descriptions. Source of truth for all implementation decisions.

### Requirements
- `.planning/REQUIREMENTS.md` — NET-01 through NET-04, SND-01 through SND-04, AFF-01 through AFF-03, STR-01 through STR-04

### Roadmap
- `.planning/ROADMAP.md` — Phase 5 details, success criteria, plan breakdown

### Prior Phase Patterns
- `.planning/phases/04-security-performance-power/04-CONTEXT.md` — Prior phase context (patterns to follow)
- `.planning/phases/04-security-performance-power/04-RESEARCH.md` — Prior phase research (patterns to follow)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Services/RegistryTweakService.cs` — Read/apply/revert registry tweaks, elevation detection, 64-bit-then-32-bit registry view fallback, `RemoveKeyWhenDisabled` for key-presence switches, `RestartExplorerAsync`
- `Services/TweakCatalog.cs` — Tweak definition model; pages supply title + tweak set
- `Controls/TweakList.xaml` — Shared presenter control for toggle lists
- `Models/RegistryTweak.cs` — Tweak model (key path, value name, type, default)
- `Services/IProcessRunner.cs` / `Services/ProcessRunner.cs` — Process execution abstraction (from Phase 4, reusable for PowerShell calls)
- `ViewModels/TweakPageViewModel.cs` — Shared VM for toggle pages
- `ViewModels/TweakPageViewModelFactory.cs` — DI-friendly factory for page VMs

### Established Patterns
- RegistryTweakService + TweakCatalog + TweakList: one engine serving every toggle, pages supply title + tweak set
- Elevation detection: `IsElevated` check, report "applied vs reboot-required"
- Shell tweaks: `RestartExplorerAsync` (confirmed, never automatic)
- IProcessRunner abstraction for testable process execution (Phase 4)
- Partial properties for all new ViewModels (MVVMTK0045 compliance)

### Integration Points
- `Features/Network/NetworkPage.xaml.cs` — Existing scaffold page, needs implementation
- `Features/Sound/SoundPage.xaml.cs` — Existing scaffold page, needs implementation
- `Features/Affinity/AffinityPage.xaml.cs` — Existing scaffold page, needs implementation
- `Features/Startup/StartupPage.xaml.cs` — Existing scaffold page, needs implementation
- DI registration in `App.xaml.cs` — New services need registration

</code_context>

<specifics>
## Specific Ideas

- User wants to "copy everything from Vain Toolbox.exe" — follow the ground truth exactly for registry keys, feature names, and behavior.
- Network offload toggles are the only registry-based tweaks in this phase — they use TweakList.
- Sound, Affinity, and Startup are all custom layouts with new services.
- IProcessRunner from Phase 4 is reusable for PowerShell calls in NetworkService.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 5-Network, Sound, Affinity & Startup*
*Context gathered: 2026-10-03*
