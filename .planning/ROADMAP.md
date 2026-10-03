# Roadmap: Vain Tools

## Overview

This roadmap guides the reverse-engineered recreation of Vain Toolbox.exe using WinUI 3 MVVM. The project is structured into 8 phases, each delivering a coherent set of features. Phase 1 establishes the architectural foundation, followed by incremental feature delivery across GPU management, system tweaks, screenshots, taskbar customization, and finally the dashboard.

## Phases

- [x] **Phase 1: Architecture Foundation** - WinUI 3 MVVM skeleton with DI container ✓
- [ ] **Phase 2: Profile System** - Profile creation, persistence, and switching
- [ ] **Phase 3: GPU Governor Core** - GPU monitoring (temp, clocks, power, fan speed)
- [ ] **Phase 4: GPU Governor Control** - Manual GPU adjustments (fans, clocks, power)
- [ ] **Phase 5: System Tweaks** - Performance optimizations and settings
- [ ] **Phase 6: Screenshots** - Screen capture functionality
- [ ] **Phase 7: Taskbar** - Taskbar customization features
- [ ] **Phase 8: Dashboard** - Status overview and quick actions

## Phase Details

### Phase 1: Architecture Foundation
**Goal:** Establish the WinUI 3 MVVM architecture with proper separation of concerns and dependency injection.

**Depends on:** Nothing (first phase)

**Requirements:** [ARCH-01, ARCH-02, ARCH-03, ARCH-04]

**Success Criteria** (what must be TRUE):
1. Project compiles and runs with empty MainWindow
2. MVVM pattern established with base classes (ViewModel, View)
3. DI container registered and functional
4. Service layer abstraction defined for GPU operations

**Plans:** 1 plan

Plans:
- [x] 01-01: Create WinUI 3 MVVM project structure with DI container

### Phase 2: Profile System
**Goal:** Implement GPU profile management (creation, persistence, switching).

**Depends on:** Phase 1

**Requirements:** [PROF-01, PROF-02, PROF-03, PROF-04]

**Success Criteria** (what must be TRUE):
1. User can create named GPU profiles
2. Profiles persist to JSON/YAML file
3. Profile switching applies settings immediately
4. Factory presets (Gaming, Silent) are available

**Plans:** 2 plans

Plans:
- [ ] 02-01: Implement profile data model and persistence service
- [ ] 02-02: Create profile management UI with listing and switching

### Phase 3: GPU Governor Core
**Goal:** Display real-time GPU metrics (temperature, clocks, power, utilization).

**Depends on:** Phase 2

**Requirements:** [GPU-01, GPU-02, GPU-03, GPU-04]

**Success Criteria** (what must be TRUE):
1. GPU temperature displays and updates in real-time
2. Core/memory clock speeds display correctly
3. Power consumption shows accurate reading
4. Fan speed percentage is displayed and updates

**Plans:** 2 plans

Plans:
- [ ] 03-01: Integrate NVML/NVIDIA API for GPU metrics
- [ ] 03-02: Create GPU governor view with metrics display

### Phase 4: GPU Governor Control
**Goal:** Enable user control over GPU fan speed, clocks, and power limits.

**Depends on:** Phase 3

**Requirements:** [GPU-05, GPU-06, GPU-07, GPU-08]

**Success Criteria** (what must be TRUE):
1. User can adjust fan speed to specific percentage
2. User can see and modify fan curve presets
3. User can increase/decrease core clock offsets
4. User can set power limit targets

**Plans:** 2 plans

Plans:
- [ ] 04-01: Implement GPU control APIs over NVML
- [ ] 04-02: Create control UI with sliders and inputs

### Phase 5: System Tweaks
**Goal:** Provide system optimization settings for performance enhancements.

**Depends on:** Phase 4

**Requirements:** [SYS-01, SYS-02, SYS-03, SYS-04]

**Success Criteria** (what must be TRUE):
1. System tweaks panel displays current settings
2. Performance mode can be enabled/disabled
3. Game mode settings are configurable
4. Changes can be applied and reverted safely

**Plans:** 2 plans

Plans:
- [ ] 05-01: Implement system tweak service layer
- [ ] 05-02: Create system tweaks UI with toggle controls

### Phase 6: Screenshots
**Goal:** Add Windows-style screen capture functionality.

**Depends on:** Phase 5

**Requirements:** [SCR-01, SCR-02, SCR-03, SCR-04]

**Success Criteria** (what must be TRUE):
1. User can capture rectangular region of screen
2. User can capture full screen screenshot
3. Screenshots save to configured directory
4. Output format can be selected (PNG/JPG)

**Plans:** 2 plans

Plans:
- [ ] 06-01: Integrate Windows GraphicsCapture API
- [ ] 06-02: Create screenshot UI with capture modes

### Phase 7: Taskbar
**Goal:** Add taskbar customization capabilities.

**Depends on:** Phase 6

**Requirements:** [TBR-01, TBR-02, TBR-03]

**Success Criteria** (what must be TRUE):
1. Taskbar can be hidden via UI toggle
2. Taskbar modifications apply cleanly
3. Settings are user-configurable

**Plans:** 1 plan

Plans:
- [ ] 07-01: Implement taskbar modification APIs and UI

### Phase 8: Dashboard
**Goal:** Provide unified status overview and quick access actions.

**Depends on:** Phase 7

**Requirements:** [DSH-01, DSH-02, DSH-03]

**Success Criteria** (what must be TRUE):
1. Dashboard shows GPU temperature, clocks, utilization at a glance
2. System metrics summary is visible
3. Quick action buttons launch frequent operations

**Plans:** 1 plan

Plans:
- [ ] 08-01: Create dashboard page with status widgets and actions

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Architecture Foundation | 1/1 | Complete | 2026-10-03 |
| 2. Profile System | 2/2 | Complete | 2026-10-03 |
| 3. GPU Governor Core | 2/2 | Complete | 2026-10-03 |
| 4. GPU Governor Control | 1/2 | In progress | - |
| 5. System Tweaks | 0/2 | Pending | - |
| 6. Screenshots | 0/2 | Pending | - |
| 7. Taskbar | 0/1 | Pending | - |
| 8. Dashboard | 0/1 | Pending | - |

---
*Roadmap updated: 2026-10-03*