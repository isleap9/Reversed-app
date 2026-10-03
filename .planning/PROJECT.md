# Vain Tools

A Windows GUI application for GPU management, system configuration, screenshots, and taskbar customization. Built as a replacement/reverse-engineering effort around VainTool.exe for better programmability and cross-platform potential.

## Purpose

Understands and eventually replaces VainTool.exe's reverse engineering capabilities:
- **GPU Governor Management** - Control GPU frequency/boost states via NVAPI/NVML
- **System Tweaks** - Windows/system optimizations  
- **Screenshots/Snipping** - Windows screenshot utilities
- **Profiles** - Save/load different configuration profiles (Factory, Gaming, Silent, Custom)
- **Taskbar Customization** - Taskbar enhancements/modifications

## Technology Stack

- **Current Target:** WinUI 3 / Windows App SDK (.NET)
- **Alternative Targets.** Python + pywin32 / .NET/C# / native C++
- **APIs Used:** NVML (NVIDIA), vendor-specific APIs, standard WinRT/WinUI calls

## Project Structure

```
VainTools/
├── .planning/                    # This project planning folder
│   ├── PROJECT.md               # This file
│   ├── REQUIREMENTS.md          # Functional requirements
│   ├── ROADMAP.md               # Development phases
│   └── STATE.md                 # Session state/memory
│   
├── research/                    # Domain research files
│   └── VAIN-TOOL-REVERSE-ENGINEERING.md  # Reverse engineering context
│   
└── phases/                      # Implementation phases (created later)
    ├── README.md                # Phase structure documentation
```

## Current Status

**Phase:** Initialization Complete → Ready for Phase 1

**Initialization Complete:**
1. ✓ PROJECT.md - Project context established
2. ✓ REQUIREMENTS.md - 24 v1 requirements defined, 8 phases mapped
3. ✓ ROADMAP.md - 8-phase development plan created with traceability
4. ✓ config.json - Workflow configuration initialized

**Next Phase:** Phase 1 - Architecture Foundation

## Core Requirements

### Phase 1 Requirements (Architecture Foundation)
- [ ] WinUI 3 MVVM architecture with proper separation of concerns
- [ ] Dependency injection container for services
- [ ] Settings/service layer abstraction for future portability
- [ ] Unit testable view models

## Technical Details Discovered

### NVML Functions Used
```csharp
nvmlInit() / nvmlShutdown()              // Library lifecycle
nvmlDeviceGetCount()                     // Device enumeration
nvmlDeviceGetHandleByIndex()             // Device access
nvmlDeviceGetName()                      // Device identification
nvmlDeviceGetTemperature()               // Temperature monitoring
nvmlDeviceGetClockOffsets() / SetClockOffsets()  // Clock offset control
nvmlDeviceSetPowerManagementLimit()      // Power limit throttling
nvmlDeviceGetFanSpeed_v2() / nvFanCurv    // Fan curve configuration
// [and many more...]
```

### Known Profiles
- `Factory Default` (ID 0) - Stock settings
- `Gaming` (ID 1) - Performance oriented
- `Silent` (ID 2) - Noise/throttling focused  
- `Custom` (ID 3) - User-defined configurations

---
*Project initialized for understanding and potential replacement of VainTool.exe*
