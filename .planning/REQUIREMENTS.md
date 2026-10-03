# Requirements: Vain Tools

**Defined:** 2026-10-03
**Core Value:** Users can fully control GPU performance and system settings through a unified WinUI 3 interface with programmatic access and cross-platform potential.

## v1 Requirements

Requirements for initial release (MVP). Each maps to roadmap phases.

### Architecture

- [ ] **ARCH-01**: WinUI 3 MVVM architecture with proper separation of concerns
- [ ] **ARCH-02**: Dependency injection container for services
- [ ] **ARCH-03**: Settings/service layer abstraction for future portability
- [ ] **ARCH-04**: Unit testable view models

### Authentication/Profile System

- [ ] **PROF-01**: User can create named GPU profiles (performance, balanced, silent modes)
- [ ] **PROF-02**: Profiles persist across application restarts
- [ ] **PROF-03**: User can switch between saved profiles instantly
- [ ] **PROF-04**: Default factory/gaming/silent profile presets available

### GPU Governor

- [ ] **GPU-01**: User can view current GPU clock (core/memory)
- [ ] **GPU-02**: User can view current GPU temperature
- [ ] **GPU-03**: User can view current GPU power consumption
- [ ] **GPU-04**: User can view current fan speed percentage
- [ ] **GPU-05**: User can manually adjust GPU fan curve
- [ ] **GPU-06**: User can set custom fan speed percentage
- [ ] **GPU-07**: User can adjust GPU clock offsets (core/memory)
- [ ] **GPU-08**: User can set power limit targets

### System Tweaks

- [ ] **SYS-01**: User can view current system tweak settings
- [ ] **SYS-02**: User can enable/disable performance mode optimizations
- [ ] **SYS-03**: User can configure game mode settings
- [ ] **SYS-04**: Settings apply cleanly and can be reverted

### Screenshots

- [ ] **SCR-01**: User can capture region of screen (Win+Shift+S style)
- [ ] **SCR-02**: User can capture full screen
- [ ] **SCR-03**: Screenshots save to configurable location
- [ ] **SCR-04**: User can choose screenshot format (PNG/JPG)

### Taskbar

- [ ] **TBR-01**: User can hide/decorate taskbar
- [ ] **TBR-02**: Taskbar settings persist
- [ ] **TBR-03**: Apply/revert taskbar modifications

### Dashboard

- [ ] **DSH-01**: Show GPU at-a-glance status (temp, clocks, utilization)
- [ ] **DSH-02**: Show system metrics summary
- [ ] **DSH-03**: Quick actions for common operations

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Advanced GPU Features

- [ ] **ADV-01**: GPU voltage adjustment
- [ ] **ADV-02**: Memory timing adjustments
- [ ] **ADV-03**: GPU undervolting profiles

### System Tweaks Advanced

- [ ] **SYS-02**: Registry modifications
- [ ] **SYS-03**: Service control interface
- [ ] **SYS-04**: Startup optimization tools

### Multi-GPU Support

- [ ] **MGPU-01**: Support multiple GPUs
- [ ] **MGPU-02**: Per-GPU profile management

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Game/library management | Core is hardware/tools, not content management |
| Monitor color calibration | Out of scope for this hardware tuning focus |
| Hardware overclocks (CPU/memory) | Focused on GPU governor only for v1 |
| Mobile platform version | Windows-specific tool, desktop first |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| ARCH-01 | Phase 1 | Pending |
| ARCH-02 | Phase 1 | Pending |
| ARCH-03 | Phase 1 | Pending |
| ARCH-04 | Phase 1 | Pending |
| PROF-01 | Phase 2 | Pending |
| PROF-02 | Phase 2 | Pending |
| PROF-03 | Phase 2 | Pending |
| PROF-04 | Phase 2 | Pending |
| GPU-01 | Phase 3 | Pending |
| GPU-02 | Phase 3 | Pending |
| GPU-03 | Phase 3 | Pending |
| GPU-04 | Phase 3 | Pending |
| GPU-05 | Phase 4 | Pending |
| GPU-06 | Phase 4 | Pending |
| GPU-07 | Phase 4 | Pending |
| GPU-08 | Phase 4 | Pending |
| SYS-01 | Phase 5 | Pending |
| SYS-02 | Phase 5 | Pending |
| SYS-03 | Phase 5 | Pending |
| SYS-04 | Phase 5 | Pending |
| SCR-01 | Phase 6 | Pending |
| SCR-02 | Phase 6 | Pending |
| SCR-03 | Phase 6 | Pending |
| SCR-04 | Phase 6 | Pending |
| TBR-01 | Phase 7 | Pending |
| TBR-02 | Phase 7 | Pending |
| TBR-03 | Phase 7 | Pending |
| DSH-01 | Phase 8 | Pending |
| DSH-02 | Phase 8 | Pending |
| DSH-03 | Phase 8 | Pending |

**Coverage:**
- v1 requirements: 24 total
- Mapped to phases: 24
- Unmapped: 0

---
*Requirements defined: 2026-10-03*
*Last updated: 2026-10-03 after initialization*