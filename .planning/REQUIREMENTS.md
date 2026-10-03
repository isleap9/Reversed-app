# Requirements: Vain Toolbox (WinUI 3 Rebuild)

**Defined:** 2026-10-03
**Core Value:** The full Vain Toolbox feature surface, reimplemented in WinUI 3 with a faithful navigation structure.

## v1 Requirements

Requirements for initial release. Each maps to roadmap phases.

### Shell & Navigation

- [x] **NAV-01**: App shell presents the real Vain Toolbox navigation tree (30 pages)
- [x] **NAV-02**: Navigation groups match the real product (Home, Vain Tools, General, System, Security, Experimental, Performance, Apps, Sound, Affinity, Startup, Power Editor, GPU, Network, Tools, About)
- [x] **NAV-03**: Nested groups expand to sub-pages (General, Apps, GPU, Tools)
- [x] **NAV-04**: Every page is reachable and renders without error
- [x] **NAV-05**: Selected page is reflected in the nav pane on startup and after navigation
- [x] **NAV-06**: Pages resolve through DI and are unit-testable

### Home

- [ ] **HOME-01**: Home shows system summary (OS, CPU, RAM, GPU, driver version)
- [ ] **HOME-02**: Home shows quick actions for common operations
- [ ] **HOME-03**: Home shows app version and update/status info

### Vain Tools (own settings)

- [ ] **VAIN-01**: User can import a `.vain` profile file
- [ ] **VAIN-02**: User can drag-and-drop a `.vain` file to import
- [ ] **VAIN-03**: User can restore Vain defaults for the listed settings
- [ ] **VAIN-04**: Invalid/foreign `.vain` files are rejected with a clear message

### GPU — NVIDIA DRS

- [ ] **DRS-01**: User can view all NVIDIA driver settings for the global profile
- [ ] **DRS-02**: User can view and select per-application (per-game) profiles
- [ ] **DRS-03**: User can search across profiles and settings
- [ ] **DRS-04**: User can edit a setting by type (Dword, Binary, AnsiString)
- [ ] **DRS-05**: User can edit bitmask settings by combining flags
- [ ] **DRS-06**: User can enter a custom hex/decimal value
- [ ] **DRS-07**: Edits are staged and only applied on explicit Apply
- [ ] **DRS-08**: Applied changes are verified by driver readback
- [ ] **DRS-09**: User can discard staged changes
- [ ] **DRS-10**: User can reload current values from the driver
- [ ] **DRS-11**: User can export the global profile to a `.vain` file
- [ ] **DRS-12**: User can restore ALL driver settings to NVIDIA defaults (confirmed, irreversible)
- [ ] **DRS-13**: Missing `nvapi64.dll` / DRS session failure is reported, not silently ignored

### GPU — Display / EDID

- [ ] **DISP-01**: User can enumerate monitors and see which have a saved EDID override
- [ ] **DISP-02**: User can add a custom resolution (timing parameters)
- [ ] **DISP-03**: User can edit and delete a custom resolution
- [ ] **DISP-04**: User can mark a timing as the preferred (native) mode
- [ ] **DISP-05**: User can add a refresh-range descriptor for VRR / FreeSync
- [ ] **DISP-06**: User can save an EDID override to the registry
- [ ] **DISP-07**: User can restore default EDID (delete the override)
- [ ] **DISP-08**: User can restart the display driver in place instead of rebooting
- [ ] **DISP-09**: Invalid timings are rejected before write
- [ ] **DISP-10**: Elevation requirement is detected and explained

### General

- [ ] **GEN-01**: General page shows the current state of its sub-settings
- [ ] **GEN-02**: Explorer tweaks (file extensions, hidden files, etc.) can be applied and reverted
- [ ] **GEN-03**: Context menu entries can be toggled
- [ ] **GEN-04**: Visual effects can be configured
- [ ] **GEN-05**: Date & time / NTP settings can be configured
- [ ] **GEN-06**: Settings visibility page can hide/show Windows settings pages

### System

- [ ] **SYS-01**: System page shows current tweak state
- [ ] **SYS-02**: Autoplay, autorun and startup-sound tweaks apply and revert
- [ ] **SYS-03**: Transparency and notification tweaks apply and revert
- [ ] **SYS-04**: Changes are reported as applied / reboot-required

### Security

- [ ] **SEC-01**: Security page shows current state of security settings
- [ ] **SEC-02**: Defender / SmartScreen related toggles apply and revert
- [ ] **SEC-03**: VBS / Memory Integrity state is displayed
- [ ] **SEC-04**: Vulnerable Driver Blocklist can be toggled
- [ ] **SEC-05**: Spectre/Meltdown mitigation override is configurable

### Performance & Power

- [ ] **PERF-01**: Performance page shows current performance settings
- [ ] **PERF-02**: Timer resolution can be set
- [ ] **PERF-03**: MPO / GPU scheduling toggles apply and revert
- [ ] **PERF-04**: Working-set adjustment coupling is configurable
- [ ] **PWR-01**: Power Editor shows available power plans
- [ ] **PWR-02**: User can edit power-plan settings
- [ ] **PWR-03**: Power changes apply and revert cleanly

### Network

- [ ] **NET-01**: Network page lists adapters
- [ ] **NET-02**: User can set DNS servers
- [ ] **NET-03**: User can manage NTP servers
- [ ] **NET-04**: Adapter offload settings can be viewed and toggled

### Sound

- [ ] **SND-01**: Sound page shows audio devices
- [ ] **SND-02**: Volume mixer is accessible
- [ ] **SND-03**: Spatial audio can be toggled
- [ ] **SND-04**: Audio enhancements can be toggled

### Affinity & Startup

- [ ] **AFF-01**: User can view running processes
- [ ] **AFF-02**: User can set CPU affinity for a process
- [ ] **AFF-03**: Affinity rules can be saved and reapplied
- [ ] **STR-01**: Startup page lists startup entries grouped by source
- [ ] **STR-02**: User can enable/disable a startup entry
- [ ] **STR-03**: User can delete a startup entry
- [ ] **STR-04**: Scheduled tasks are listed alongside Run-key entries

### Apps

- [ ] **APPX-01**: User can list installed Appx/provisioned packages
- [ ] **APPX-02**: User can remove a provisioned package
- [ ] **INST-01**: User can list installed programs from the uninstall registry
- [ ] **INST-02**: User can uninstall a program (quiet or normal)
- [ ] **INST-03**: User can copy a program's uninstall command
- [ ] **OPT-01**: User can list Windows optional features
- [ ] **OPT-02**: User can enable/disable an optional feature
- [ ] **STOR-01**: Store page lists installable apps
- [ ] **STOR-02**: User can install an app from the Store page

### Tools

- [ ] **TOOL-01**: Device Cleaner detects orphaned processes, services, devices, driver packages, registry keys and folders
- [ ] **TOOL-02**: Device Cleaner can remove selected orphans
- [ ] **TOOL-03**: Drive Scanner analyzes drive space usage
- [ ] **TOOL-04**: Drive Scanner shows a breakdown by folder/file size
- [ ] **DRV-01**: Driver Manager enumerates installed driver packages
- [ ] **DRV-02**: User can export a driver package
- [ ] **DRV-03**: User can delete a driver package (with force option)
- [ ] **DRV-04**: User can add/install a driver package
- [ ] **DRV-05**: GPU driver uninstall is supported for NVIDIA and AMD

### Experimental & About

- [ ] **EXP-01**: Experimental page surfaces unfinished features behind a clear warning
- [ ] **ABT-01**: About page shows app version, links and credits

### Tray Helper Features

- [ ] **TRAY-01**: Screenshot capture (region + full screen) is available
- [ ] **TRAY-02**: Screenshot save location and format are configurable
- [ ] **TRAY-03**: Taskbar hide/restore works and reverts cleanly
- [ ] **TRAY-04**: Latency-related toggles apply and revert

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Advanced

- **ADV-01**: HDMI audio data-block injection in EDID
- **ADV-02**: Multi-monitor EDID profile management
- **ADV-03**: Per-application DRS profile auto-switching
- **ADV-04**: Governor rules engine (process-triggered profiles)
- **ADV-05**: Scheduled/automatic tweak application

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Bit-for-bit UI clone / author's branding assets | Reproduce structure and function, not proprietary assets |
| Native C++/WinRT rewrite | Deliberate managed C# MVVM reimplementation |
| Undocumented `nvapi64.dll` surfaces beyond DRS | Only documented DRS entry points are targeted |
| Multi-GPU / SLI handling | v1 targets single-GPU systems |
| Telemetry, auto-update, license validation | Not part of the feature surface being rebuilt |
| Cross-platform (Linux/macOS) port | Windows-specific tool |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| NAV-01 … NAV-06 | Phase 1 | Complete |
| HOME-01 … HOME-03 | Phase 2 | Pending |
| VAIN-01 … VAIN-04 | Phase 2 | Pending |
| GEN-01 … GEN-06 | Phase 3 | Pending |
| SYS-01 … SYS-04 | Phase 3 | Pending |
| SEC-01 … SEC-05 | Phase 4 | Pending |
| PERF-01 … PERF-04, PWR-01 … PWR-03 | Phase 4 | Pending |
| NET-01 … NET-04, SND-01 … SND-04 | Phase 5 | Pending |
| AFF-01 … AFF-03, STR-01 … STR-04 | Phase 5 | Pending |
| APPX-01 … APPX-02, INST-01 … INST-03 | Phase 6 | Pending |
| OPT-01 … OPT-02, STOR-01 … STOR-02 | Phase 6 | Pending |
| TOOL-01 … TOOL-04, DRV-01 … DRV-05 | Phase 7 | Pending |
| DRS-01 … DRS-13 | Phase 8 | Pending |
| DISP-01 … DISP-10 | Phase 9 | Pending |
| EXP-01, ABT-01 | Phase 10 | Pending |
| TRAY-01 … TRAY-04 | Phase 10 | Pending |

**Coverage:**
- v1 requirements: 76 total
- Mapped to phases: 76
- Unmapped: 0

---
*Requirements defined: 2026-10-03*
*Last updated: 2026-10-03 after ground-truth reverse engineering*
