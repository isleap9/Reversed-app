# Plan 04-01 Summary: Security Tweaks & Security Page

## Status: ✅ Complete

## Tasks Executed

### Task 04-01-01: Add Security tweaks to TweakCatalog
- **Commit:** `f58324f` — `feat(security): add 8 Security tweaks to TweakCatalog`
- Added `Security` property to `TweakCatalog` with 8 registry tweaks:
  1. `security-defender-disable` — Disable Windows Defender (DisableAntiSpyware=1)
  2. `security-defender-tamper` — Disable Tamper Protection (TamperProtection=0, inverted: 5=on)
  3. `security-vbs` — Enable Virtualization-Based Security (EnableVirtualizationBasedSecurity=1)
  4. `security-memory-integrity` — Enable Memory Integrity (Enabled=1)
  5. `security-vulnerable-driver-blocklist` — Enable Vulnerable Driver Blocklist (VulnerableDriverBlocklistEnable=1)
  6. `security-spectre-meltdown` — Enable Spectre & Meltdown Mitigations (FeatureSettingsOverride=0, inverted: 3=off)
  7. `security-uac` — Disable User Account Control (EnableLUA=0, inverted: 1=on)
  8. `security-smartscreen` — Disable SmartScreen (EnableSmartScreen=0, inverted: 1=on)
- All tweaks write to HKLM and require admin elevation
- Updated `TweakCatalog.All` to include Security collection

### Task 04-01-02: Wire up SecurityPage with TweakPageViewModel
- **Commit:** `d493fb3` — `feat(security): wire SecurityPage to TweakPageViewModel via TweakList`
- Replaced SecurityPage.xaml with TweakList control (Title="Security", Subtitle="Windows security settings")
- Updated SecurityPage.xaml.cs to use `TweakPageViewModelFactory.Create("Security", TweakCatalog.Security)`
- Follows the exact same pattern as GeneralPage and other Phase 3 pages

### Task 04-01-03: Add Security catalog tests
- **Commit:** `0dd909a` — `test(security): add 10 Security catalog tests`
- Added 10 tests to `RegistryTweakServiceTests.cs`:
  - `Catalog_SecurityTweaks_HaveCorrectIds` — verifies all 8 tweak IDs
  - `Catalog_SecurityTweaks_AllRequireAdmin` — all write HKLM
  - `Catalog_SecurityTweaks_NoneRequireExplorerRestart`
  - `Catalog_TamperProtection_InvertedValues` — EnabledValue=0, DisabledValue=5
  - `Catalog_SpectreMeltdown_InvertedValues` — EnabledValue=0, DisabledValue=3
  - `Catalog_Uac_InvertedValues` — EnabledValue=0, DisabledValue=1
  - `Catalog_SmartScreen_InvertedValues` — EnabledValue=0, DisabledValue=1
  - `Catalog_GpuScheduling_NotInSecurity` — sanity check
  - `Catalog_SecurityTweaks_HaveRebootRequiredInDescription`
  - `Catalog_SecurityTweaks_HaveWarningsInDescription`

## Verification
- **Build:** 0 warnings, 0 errors
- **Tests:** 140 passed (was 130, +10 new)
- **All commits atomic:** one commit per task

## Files Modified
- `src/VainTools.App/Services/TweakCatalog.cs` — added Security collection + updated All
- `src/VainTools.App/Features/Security/SecurityPage.xaml` — replaced with TweakList
- `src/VainTools.App/Features/Security/SecurityPage.xaml.cs` — wired to TweakPageViewModelFactory
- `src/VainTools.Tests/RegistryTweakServiceTests.cs` — added 10 security catalog tests

## Notes
- 4 tweaks use inverted values (EnabledValue < DisabledValue): TamperProtection, Spectre/Meltdown, UAC, SmartScreen
- 3 tweaks mention "Reboot required" in description: VBS, Memory Integrity, Vulnerable Driver Blocklist
- 2 tweaks include security warnings in description: UAC, SmartScreen
- No WMI queries used — registry only (per D-02)
- Reuses Phase 3 elevation pattern via TweakPageViewModel (per D-05)
