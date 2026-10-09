# Roadmap: Vain Toolbox (WinUI 3 Rebuild)

## Overview

This roadmap rebuilds **Vain Toolbox** in WinUI 3 / C#, following the real
application's structure recovered by static analysis
(`.planning/research/VAIN-TOOLBOX-GROUND-TRUTH.md`).

The previous roadmap targeted six pages that do not exist in the real product. It
has been replaced. Phase 1 now delivers the real 30-page navigation shell; the
feature phases then fill those pages in, with the two flagship areas — the NVIDIA
DRS editor and the EDID/display editor — given their own dedicated phases because
of their depth.

## Phases

- [x] **Phase 1: Real Navigation Shell** - 28-page tree, grouped, routed, DI-resolved
- [x] **Phase 2: Home & Vain Tools** - System summary, quick actions, `.vain` import, restore defaults
- [x] **Phase 3: General & System** - Explorer, context menu, visual, date/time, visibility, system tweaks
- [x] **Phase 4: Security, Performance & Power** - Security toggles, timer/MPO, power plan editor (completed 2026-10-03)
- [x] **Phase 5: Network, Sound, Affinity & Startup** - Adapters/DNS/NTP, audio, CPU affinity, startup entries (completed 2026-10-04)
- [ ] **Phase 6: Apps** - Appx Manager, Installed Apps, Optional Features, Store
- [ ] **Phase 7: Tools** - Device Cleaner, Drive Scanner, Driver Manager
- [ ] **Phase 8: NVIDIA DRS Editor** - Driver settings read/stage/apply, per-game profiles, `.vain` export
- [ ] **Phase 9: Display / EDID Editor** - Monitor enumeration, custom resolutions, EDID override, driver restart
- [ ] **Phase 10: Experimental, About & Tray Helper** - Experimental page, About, screenshots, taskbar, latency

## Phase Details

### Phase 1: Real Navigation Shell

**Goal:** Replace the six guessed pages with the real 30-page Vain Toolbox navigation tree, grouped and routed.

**Depends on:** Nothing (first phase)

**Requirements:** [NAV-01, NAV-02, NAV-03, NAV-04, NAV-05, NAV-06]

**Success Criteria** (what must be TRUE):
1. Nav pane shows the real page tree in the real grouping order
2. Nested groups (General, Apps, GPU, Tools) expand to their sub-pages
3. Every one of the 28 feature pages is reachable and renders without error
4. The previously guessed pages are removed and no longer referenced
5. Navigation state is reflected correctly on startup and after each navigation
6. Solution builds with 0 warnings / 0 errors and the existing test suite still passes

**Plans:** 2 plans

Plans:
- [x] 01-01: Define the real navigation model and page scaffolding (28 pages, grouped)
- [x] 01-02: Rewire the shell (MainWindow nav, DI registration) and retire the old pages

### Phase 2: Home & Vain Tools

**Goal:** Deliver the landing page and the app's own settings, including `.vain` profile import.

**Depends on:** Phase 1

**Requirements:** [HOME-01, HOME-02, HOME-03, VAIN-01, VAIN-02, VAIN-03, VAIN-04]

**Success Criteria** (what must be TRUE):
1. Home shows OS/CPU/RAM/GPU summary and driver version
2. Home offers working quick actions
3. User can import a `.vain` file via picker and via drag-and-drop
4. A foreign or malformed `.vain` file is rejected with a clear message
5. "Restore Vain defaults" applies the documented default set after confirmation

**Plans:** 2 plans

Plans:
- [x] 02-01: Implement system-info service and Home page
- [x] 02-02: Implement `.vain` import pipeline and restore-defaults flow

### Phase 3: General & System

**Goal:** Implement the General sub-pages and the System tweak page with apply/revert.

**Depends on:** Phase 2

**Requirements:** [GEN-01, GEN-02, GEN-03, GEN-04, GEN-05, GEN-06, SYS-01, SYS-02, SYS-03, SYS-04]

**Success Criteria** (what must be TRUE):
1. General, Explorer, Context Menu, Visual, Date & Time and Settings Visibility pages all render their real controls
2. Explorer tweaks (extensions, hidden files) apply and revert
3. Context menu entries toggle correctly
4. System tweaks (autoplay, autorun, startup sound, transparency) apply and revert
5. Reboot-required changes are reported as such

**Plans:** 3 plans

Plans:
- [x] 03-01: Registry tweak service with apply/revert/read-state
- [x] 03-02: General + Explorer + Context Menu + Visual pages
- [x] 03-03: Date & Time, Settings Visibility and System pages

### Phase 4: Security, Performance & Power

**Goal:** Implement security toggles, performance tuning and the power plan editor.

**Depends on:** Phase 3

**Requirements:** [SEC-01, SEC-02, SEC-03, SEC-04, SEC-05, PERF-01, PERF-02, PERF-03, PERF-04, PWR-01, PWR-02, PWR-03]

**Success Criteria** (what must be TRUE):
1. Security page displays current Defender/SmartScreen/VBS state
2. Vulnerable Driver Blocklist and Spectre/Meltdown overrides are configurable
3. Timer resolution and MPO/GPU-scheduling toggles apply and revert
4. Power Editor lists plans and can edit plan settings
5. All changes report applied vs reboot-required accurately

**Plans:** 3/3 plans complete

Plans:
**Wave 1**
- [x] 04-01: Security service (Defender/VBS/blocklist) and Security page

**Wave 2** *(blocked on Wave 1 completion)*
- [x] 04-02: Performance service (timer, MPO, GPU scheduling) and Performance page

**Wave 3** *(blocked on Wave 2 completion)*
- [x] 04-03: Power plan service (powercfg) and Power Editor page

### Phase 5: Network, Sound, Affinity & Startup

**Goal:** Implement the networking, audio, CPU affinity and startup-management pages.

**Depends on:** Phase 4

**Requirements:** [NET-01, NET-02, NET-03, NET-04, SND-01, SND-02, SND-03, SND-04, AFF-01, AFF-02, AFF-03, STR-01, STR-02, STR-03, STR-04]

**Success Criteria** (what must be TRUE):
1. Network page lists adapters and can set DNS + NTP servers
2. Adapter offload settings are viewable and toggleable
3. Sound page lists devices and exposes spatial audio / enhancement toggles
4. Affinity page can set CPU affinity for a running process and save the rule
5. Startup page lists Run-key and scheduled-task entries, and can enable/disable/delete them

**Plans:** 4/4 plans complete

Plans:
**Wave 1**
- [x] 05-01: Network service (adapters, DNS, NTP, offloads) and Network page

**Wave 2** *(blocked on Wave 1 completion)*
- [x] 05-02: Audio service and Sound page

**Wave 3** *(blocked on Wave 2 completion)*
- [x] 05-03: Affinity service and Startup service with their pages

**Gap remediation**
- [x] 05-04: Volume mixer (SND-02), affinity rule persistence (AFF-03), startup delete (STR-03), DNS/NTP path tests (NET-02/NET-03)

### Phase 6: Apps

**Goal:** Implement app and package management across the four Apps sub-pages.

**Depends on:** Phase 5

**Requirements:** [APPX-01, APPX-02, INST-01, INST-02, INST-03, OPT-01, OPT-02, STOR-01, STOR-02]

**Success Criteria** (what must be TRUE):
1. Appx Manager lists installed and provisioned packages and can remove them
2. Installed Apps lists uninstall-registry programs with uninstall + copy-command
3. Optional Features lists features and can enable/disable them
4. Store page lists installable apps and can install one

**Plans:** 1/2 plans executed

Plans:
**Wave 1**
- [x] 06-01: Package service (Appx + uninstall registry + optional features) and three pages

**Wave 2** *(blocked on Wave 1 completion)*
- [ ] 06-02: Store integration and Store page

**Cross-cutting constraints:**
- All ViewModels use partial properties (MVVMTK0045 compliance)

### Phase 7: Tools

**Goal:** Implement Device Cleaner, Drive Scanner and Driver Manager.

**Depends on:** Phase 6

**Requirements:** [TOOL-01, TOOL-02, TOOL-03, TOOL-04, DRV-01, DRV-02, DRV-03, DRV-04, DRV-05]

**Success Criteria** (what must be TRUE):
1. Device Cleaner detects orphaned processes, services, devices, driver packages, registry keys and folders
2. Selected orphans can be removed, with force-delete offered on failure
3. Drive Scanner reports space usage broken down by folder
4. Driver Manager enumerates, exports, deletes and adds driver packages via `pnputil`
5. GPU driver uninstall works for NVIDIA and AMD, with elevation detected and explained

**Plans:** 3 plans

Plans:
- [ ] 07-01: Device Cleaner detection engine and page
- [ ] 07-02: Drive Scanner service and page
- [ ] 07-03: Driver Manager (pnputil wrapper) and page

### Phase 8: NVIDIA DRS Editor

**Goal:** Implement the flagship NVIDIA driver-settings editor over `nvapi64.dll`.

**Depends on:** Phase 7

**Requirements:** [DRS-01, DRS-02, DRS-03, DRS-04, DRS-05, DRS-06, DRS-07, DRS-08, DRS-09, DRS-10, DRS-11, DRS-12, DRS-13]

**Success Criteria** (what must be TRUE):
1. All global-profile driver settings load and display with driver default vs current
2. Per-application profiles are listed and selectable
3. Settings can be searched across profiles
4. Dword/Binary/AnsiString/bitmask/custom-value editors all work
5. Edits stage and only apply on explicit Apply; readback verifies the result
6. Staged changes can be discarded and values reloaded from the driver
7. Global profile exports to `.vain`; restore-all-defaults works behind a confirmation
8. Missing `nvapi64.dll` or a failed DRS session produces a clear error, not silence

**Plans:** 3 plans

Plans:
- [ ] 08-01: NVAPI DRS interop layer (session, setting enumeration, typed values)
- [ ] 08-02: DRS page with profile list, search and typed editors
- [ ] 08-03: Staged-change engine, readback verification and `.vain` export

### Phase 9: Display / EDID Editor

**Goal:** Implement the monitor EDID override and custom-resolution editor.

**Depends on:** Phase 8

**Requirements:** [DISP-01, DISP-02, DISP-03, DISP-04, DISP-05, DISP-06, DISP-07, DISP-08, DISP-09, DISP-10]

**Success Criteria** (what must be TRUE):
1. Monitors enumerate from the registry with override state indicated
2. Custom resolutions can be added, edited and deleted with full timing parameters
3. A timing can be marked preferred/native
4. A refresh-range descriptor can be added for VRR/FreeSync
5. EDID override saves to the registry and can be restored to default
6. Display driver can be restarted in place, with the black-screen escape documented
7. Invalid timings are rejected before any write
8. Elevation requirements are detected and explained

**Plans:** 3 plans

Plans:
- [ ] 09-01: EDID/timing model and validation
- [ ] 09-02: Monitor enumeration, EDID read/parse and registry override service
- [ ] 09-03: Display page with timing editor and driver-restart action

### Phase 10: Experimental, About & Tray Helper

**Goal:** Finish the remaining pages and the tray-helper feature set.

**Depends on:** Phase 9

**Requirements:** [EXP-01, ABT-01, TRAY-01, TRAY-02, TRAY-03, TRAY-04]

**Success Criteria** (what must be TRUE):
1. Experimental page surfaces unfinished features behind a clear warning
2. About page shows version, links and credits
3. Region and full-screen screenshot capture work
4. Screenshot location and format are configurable
5. Taskbar hide/restore works and reverts cleanly
6. Latency toggles apply and revert

**Plans:** 2 plans

Plans:
- [ ] 10-01: Experimental and About pages
- [ ] 10-02: Tray helper — screenshots, taskbar, latency

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Real Navigation Shell | 2/2 | Complete | 2026-10-03 |
| 2. Home & Vain Tools | 2/2 | Complete | 2026-10-03 |
| 3. General & System | 3/3 | Complete | 2026-10-03 |
| 4. Security, Performance & Power | 3/3 | Complete    | 2026-10-03 |
| 5. Network, Sound, Affinity & Startup | 4/4 | Complete | 2026-10-04 |
| 6. Apps | 1/2 | Pending | - |
| 7. Tools | 0/3 | Pending | - |
| 8. NVIDIA DRS Editor | 0/3 | Pending | - |
| 9. Display / EDID Editor | 0/3 | Pending | - |
| 10. Experimental, About & Tray Helper | 0/2 | Pending | - |

---
*Roadmap rewritten: 2026-10-03 after ground-truth reverse engineering*
