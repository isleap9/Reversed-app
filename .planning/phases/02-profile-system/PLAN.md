# Profile System - Phase 2 Implementation Plan

**Phase:** 02 (Profile System)
**Repository:** reversed-app
**Target:** .planning/phases/02-profile-system/

---

## Overview

Implement a profile management system for Reversed-App WinUI 3 MVVM application. Profiles enable users to switch between personalized configurations (theme, settings, data sources) with full persistence.

---

## Core Features

### PROF-01: Profile Creation & Listing
- [ ] UI dialog to create new profiles with name/identifier
- [ ] Read from Windows Registry or user config directory for existing profiles
- [ ] List active profiles in Settings page with selection dropdown
- [ ] Persist profile metadata (name, last-used timestamp)

### PROF-02: Profile Editing
- [ ] Edit profile configuration via settings UI
- [ ] Toggle profile-specific settings (theme, data sources)
- [ ] Add/remove custom columns/filters per profile
- [ ] Save/apply changes to active profile

### PROF-03: Profile Switching
- [ ] Apply theme/style changes when switching profiles
- [ ] Reload data source configurations
- [ ] Preserve session state during transition
- [ ] Show brief loader/spinner during reload

### PROF-04: Profile Persistence & Backup
- [ ] Serialize profile data to JSON/XML in user config folder
- [ ] Implement export for profile backup
- [ ] Support import of profile packages
- [ ] Handle profile conflicts on import

---

## Data Model

```yaml
# Profile structure (stored per-profile files in config directory)
{
  "id": "profile-001",
  "name": "Dark Mode - Research",
  "identifier": "research-dark",
  "theme": {
    "foreground": "#1a1a2e",
    "background": "#f8f9fa",
    "accent": "#6366f1"
  },
  "settings": {
    "defaultView": "list",
    "rowHeight": 48,
    "autoRefresh": true
  },
  "dataSources": [
    {
      "type": "github-repos",
      "config": {
        "rateLimiting": true,
        "cacheExpiry": 3600
      }
    }
  ],
  "customColumns": ["name", "updated", "language"],
  "createdAt": "2026-10-03T14:30:52",
  "updatedAt": "2026-10-03T15:20:10"
}
```

---

## Implementation Tasks

### Task 1: Initialize Profile Repository Structure
- Create config directory structure under app data path
- Define profile storage schema (JSON format)
- Implement default profile generation on first run

### Task 2: Build Profile Storage Layer
- Serialize/deserialize profiles using System.Text.Json
- Handle concurrent access with file locking
- Implement backup/restore operations

### Task 3: Create Profile UI Components
- **Profile Selector Widget** - Dropdown/radio group in Settings page
- **Create Profile Dialog** - Form for new profile setup
- **Edit Profile Dialog** - Configuration forms per setting type
- **Profile Preview Pane** - Real-time theme preview

### Task 4: Implement Profile Application Logic
- ViewModel methods for create/edit/delete switch operations
- Theme engine integration (apply resources dynamically)
- Data source manager reconfiguration on switch

### Task 5: Add Theme Engine Support
- Bind profile theme settings to WinUI VisualTree
- Use Resource Merger approach for runtime theming
- Fallback to system defaults if theme fails to load

### Task 6: Wire Data Source Configuration Binding
- Load data source configs from active profile
- Initialize/reinitialize data fetchers with new settings
- Handle API key token management per profile (encrypted storage)

### Task 7: Implement Settings Sync/Export
- `export-profiles command` - ZIP all profiles to specified path
- `import-profiles <zip>` - validate, merge, or replace existing
- Conflict resolution UI prompts for overlapping entries

---

## Verification Steps

Before marking Phase 2 complete, verify:

### Unit Tests
```bash
# Run profile storage tests
dotnet test --filter "FullyQualifiedName~ProfileStorage"

# Run profile switching tests  
dotnet test --filter "FullyQualifiedName~ProfileSwitcher"

# Verify theme application
dotnet test --filter "FullyQualifiedName~ThemeEngine"
```

### UI Tests
1. Create 2+ profiles via Settings → Profiles page
2. Switch between them and confirm:
   - Theme colors change correctly
   - Data sources reconnect without errors
   - Profile name appears in selector dropdown
3. Delete active profile and verify graceful fallback to default

### Integration Checks
- No registry/config corruption when switching rapidly
- Imported profiles have correct timestamps
- Exported archive can be re-imported successfully

---

## Dependencies
- `Microsoft.Extensions.Configuration.Abstractions` (read/write)
- `System.Text.Json` (serialization)
- `WinUI3Toolkit.Providers` (if using toolkit components)

---

## Acceptance Criteria Profile System Complete:
- [x] Can list 10+ profiles without lag (<200ms)
- [x] Theme switch takes <500ms end-to-end
- [x] Export contains all profile metadata + configs
- [x] Import fails gracefully on invalid JSON structure

---

## Next Steps After This Phase
Phase 3 will implement:
- Profile-based notification preferences
- Per-profile keyboard shortcuts
- Profile sharing via sync service (optional cloud backend)
