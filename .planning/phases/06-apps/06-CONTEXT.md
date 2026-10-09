# Phase 6: Apps - Context

**Gathered:** 2026-10-04
**Status:** Ready for planning

<domain>
## Phase Boundary

Implement four app/package management pages: Appx Manager (enumerate/remove provisioned packages via WinRT `PackageManager`), Installed Apps (direct registry read of uninstall keys + `Process.Start` on `UninstallString`), Optional Features (DISM API enable/disable), and Store (winget CLI for listing and install). All pages use custom per-page XAML layouts following the Phase 5 pattern.

</domain>

<decisions>
## Implementation Decisions

### Appx Manager — Package Enumeration & Removal
- **D-01:** Use the WinRT `Windows.Management.Deployment.PackageManager` API directly in C# — this is the native API the real `Vain Toolbox.exe` uses (it is native C++/WinRT). The faithful equivalent in C# is the same WinRT API. — **Reversibility:** reversible — service interface can be swapped later
- **D-02:** Appx Manager lists both installed and provisioned packages. Removal via `PackageManager.RemovePackageAsync`. The real app enumerates/removes provisioned packages.
  - **D-02 amendment (2026-10-09, user-approved during gap-closure planning):** Installed rows keep `RemovePackageAsync` (per-user). Provisioned rows are removed with `PackageManager.DeprovisionPackageForAllUsersAsync`, because per-user removal cannot remove a provisioned package (APPX-02). Deprovisioning is system-wide and also affects new user accounts, so the confirmation dialog must state the all-users effect. Implemented by 06-05.

### Installed Apps — Registry Enumeration & Uninstall Execution
- **D-03:** Use direct registry reads (`Microsoft.Win32.Registry`) on `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall`, `HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall`, and `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall` — matches the real native app exactly.
- **D-04:** Use `UninstallString` and `QuietUninstallString` from registry values. "Copy uninstall command" copies the raw string to clipboard.
- **D-05:** Execute uninstall via direct `Process.Start` on the `UninstallString` — matches the real native app (it just creates a process). No IProcessRunner abstraction for this page.

### Optional Features — DISM API
- **D-06:** Use the DISM API (via P/Invoke or COM interop) for `Enable-WindowsOptionalFeature` / `Disable-WindowsOptionalFeature` — matches the real native app's likely approach. Requires elevation.
- **D-07:** List features via DISM's `GetOptionalFeatures` or equivalent. Ground truth says "Windows optional feature enable/disable."

### Store — winget CLI
- **D-08:** Use `winget` CLI for listing installable apps (`winget search`) and installing (`winget install`) — matches the ground truth's "winget-style app installs" description.
- **D-09:** The Store page lists installable apps and can install one. The listing mechanism (search results, curated list) is at the researcher's discretion — the ground truth only says "winget-style."

### Elevation & Safety
- **D-10:** Appx removal, optional feature enable/disable, and uninstall all require admin. Follow the Phase 3/4/5 elevation pattern: `IsElevated` check, report "applied vs reboot-required" or "requires administrator."
- **D-11:** Destructive operations (Appx removal, program uninstall) follow `safety.always_confirm_destructive` — explicit confirmation before execution. The real app may or may not have confirmations; the project safety config requires them.

### Error/Failure States
- **D-12:** Follow the real app's behavior 1:1 for failure states. The researcher should identify from ground truth or binary analysis what the real app shows when: Appx removal fails (package in use, system package protected), UninstallString is missing/invalid, optional feature enable fails, winget install fails.

### Refresh Behavior After Mutations
- **D-13:** After removing an Appx package or uninstalling a program, reload the list. The researcher should confirm the real app's refresh behavior (automatic reload, manual refresh button, or navigation-triggered).

### Layout Pattern
- **D-14:** Follow the Phase 5 custom layout pattern — each page has its own custom XAML with a list/grid and action buttons, similar to Sound, Affinity, and Startup pages. No shared list component; per-page layouts match the real app's per-page custom layouts.

### Claude's Discretion
- Exact DISM API P/Invoke signatures and COM interop details — researcher should provide concrete implementation approach.
- winget CLI output parsing (search results, install progress) — researcher should identify the exact winget commands and output format.
- Error/failure state UI messages — researcher should identify from ground truth or binary analysis.
- Store page listing mechanism (winget search vs curated list) — researcher decides based on ground truth.
- Test strategy for WinRT PackageManager, DISM P/Invoke, and winget CLI mocking — planner decides approach.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Ground Truth
- `.planning/research/VAIN-TOOLBOX-GROUND-TRUTH.md` — Reverse engineering findings: real page structure, registry keys, feature descriptions. Source of truth for all implementation decisions. Key section: "Apps" — Appx Manager (enumerate/remove provisioned packages), Installed Apps (reads `SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall`, uses `UninstallString`/`QuietUninstallString`, "Copy uninstall command"), Optional Features (Windows optional feature enable/disable), Store (winget-style app installs).

### Requirements
- `.planning/REQUIREMENTS.md` — APPX-01, APPX-02, INST-01, INST-02, INST-03, OPT-01, OPT-02, STOR-01, STOR-02

### Roadmap
- `.planning/ROADMAP.md` — Phase 6 details, success criteria, plan breakdown (2 plans: 06-01 package service + three pages, 06-02 Store integration + Store page)

### Prior Phase Patterns
- `.planning/phases/05-network-sound-affinity-startup/05-CONTEXT.md` — Prior phase context (custom layout pattern, IProcessRunner, elevation pattern)
- `.planning/phases/04-security-performance-power/04-CONTEXT.md` — Prior phase context (elevation pattern, TweakList vs custom layout decisions)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Services/IProcessRunner.cs` / `Services/ProcessRunner.cs` — Process execution abstraction (from Phase 4). Not used for Installed Apps (real app uses direct Process.Start), but available for other services if needed.
- `Services/RegistryTweakService.cs` — Read/apply/revert registry tweaks, elevation detection. Not directly used (Installed Apps reads uninstall keys, not tweak keys), but the elevation pattern is reusable.
- `Controls/TweakList.xaml` — Shared presenter for toggle lists. NOT used for Apps pages (all custom layouts).
- `ViewModels/TweakPageViewModel.cs` — Shared VM for toggle pages. NOT used for Apps pages.

### Established Patterns
- Elevation detection: `IsElevated` check, report "applied vs reboot-required" (Phase 3/4/5 pattern)
- Custom page layouts: per-page XAML with list/grid + action buttons (Phase 5 pattern for Sound, Affinity, Startup)
- Partial properties for all new ViewModels (MVVMTK0045 compliance)
- Test project references app project for service testing

### Integration Points
- `src/VainTools.App/Features/Apps/AppxManagerPage.xaml.cs` — Existing scaffold page, needs implementation
- `src/VainTools.App/Features/Apps/InstalledAppsPage.xaml.cs` — Existing scaffold page, needs implementation
- `src/VainTools.App/Features/Apps/OptionalFeaturesPage.xaml.cs` — Existing scaffold page, needs implementation
- `src/VainTools.App/Features/Apps/StorePage.xaml.cs` — Existing scaffold page, needs implementation
- DI registration in `App.xaml.cs` — New services need registration

</code_context>

<specifics>
## Specific Ideas

- User wants to "copy everything from Vain Toolbox.exe" — follow the ground truth exactly for registry keys, feature names, API choices, and behavior.
- All API choices (WinRT PackageManager, direct registry, DISM, winget, direct Process.Start) are chosen to match the real native app 1:1.
- All four pages use custom per-page XAML layouts following the Phase 5 pattern.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 6-Apps*
*Context gathered: 2026-10-04*