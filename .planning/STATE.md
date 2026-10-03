# Vain Tools - Project State
## Session Tracking & Progress

### Last Updated
- **Session Start:** Build failure fixed; solution green

### Build Status
- `dotnet build` — succeeded, **0 warnings / 0 errors** (was 1 error + 6 warnings)
- `dotnet test` — 99 passed / 0 failed
- App verified to launch: `VainTools.App.exe`, window "Vain Tools", Responding=True

### Resolved Build Issues
1. **WMC9999 "Could not find any resources appropriate for the specified culture"**
   - The WinUI markup compiler was resolving a binding on `GpuGovernorPage` and
     failing to format the resulting *warning* message (its `ErrorMessages`
     resource set could not be loaded), so the real error was never printed.
   - Actual cause: `x:Bind ViewModel.RefreshCommand` — `[RelayCommand]` on
     `RefreshMetricsAsync()` generates `RefreshMetricsCommand`, not
     `RefreshCommand`. Fixed the binding.
   - `FanCurveEditor`'s `x:Class` namespace also disagreed with its code-behind
     (`VainTools.GpuGovernor.Controls` vs `VainTools.App.Controls`); aligned to
     `VainTools.App.Controls`.
2. **58x MVVMTK0045** — field-based `[ObservableProperty]` is not AOT compatible
   in WinRT/WinUI 3. Converted every one to a partial property
   (`[ObservableProperty] public partial T Name { get; set; }`) across all six
   ViewModels.
3. Restored the `TemperatureToColorConverter` foreground bindings on the
   temperature card in `GpuGovernorPage.xaml` — these had been deleted as a
   failed workaround for the WMC9999 above.

### Debugging Note (important for future XAML build failures)
`WMC9999` on this stack is often a **masked** error. To see the real one, run the
XAML compiler directly against the generated input and read its JSON output:

```bash
XAMLCOMPILER=~/.nuget/packages/microsoft.windowsappsdk.winui/2.3.0/tools/net472/XamlCompiler.exe
cp src/VainTools.App/obj/Debug/net10.0-windows10.0.26100.0/win-x64/input.json /tmp/in.json
"$XAMLCOMPILER" 'C:\path\to\in.json' 'C:\path\to\out.json'
# then inspect MSBuildLogEntries in out.json for Type != 0
```
Note the input/output JSON paths must be native Windows paths, and the compiler
must be run with its own directory context for type resolution to work.

- **Files Created/Updated:** 
  - `.planning/config.json` [✓]
  - `.planning/REQUIREMENTS.md` [✓]
  - `.planning/ROADMAP.md` [✓]
  - `.planning/STATE.md` [✓]
  - `GpuGovernor/ViewModels/GpuGovernorViewModel.cs` [✓]
  - `GpuGovernor/Views/GpuGovernorView.xaml` [✓]
  - `GpuGovernor/Views/GpuGovernorView.xaml.cs` [✓]
  - `GpuGovernor/Converters/TemperatureToColorConverter.cs` [✓]
  - `GpuGovernor/Converters/InverseBooleanConverter.cs` [✓]
  - `GpuGovernor/Controls/FanCurveEditor.xaml` [✓]
  - `GpuGovernor/Controls/FanCurveEditor.xaml.cs` [✓]
  - `Services/GpuService.cs` [✓]
  - `Services/ProfileService.cs` [✓]
  - `Models/GpuInfo.cs` [✓]
  - `Models/GpuProfile.cs` [✓]
  - `Profiles/ProfileViewModel.cs` [✓]

### Current Context
**Project Goal:** Reverse engineer Vain Toolbox.exe and recreate it in WinUI 3 MVVM for programmatic access and cross-platform potential.

**Architecture Designed:**
- WinUI 3 / Windows App SDK (.NET 6+)
- MVVM pattern with proper separation of concerns
- Dependency injection for service abstraction
- NVML API integration for GPU control
- File-based profile persistence

### Phase Progress
- **Phase 1:** Architecture Foundation [✓] - 1/1 plans completed
- **Phase 2:** Profile System [✓] - 2/2 plans completed
- **Phase 3:** GPU Governor Core [✓] - 2/2 plans completed
- **Phase 4:** GPU Governor Control [✓] - 2/2 plans completed
- **Phase 5+:** Pending

### Completed Artifacts
- `.planning/phases/01-architecture-foundation/01-01-PLAN.md` [✓]
- `.planning/phases/01-architecture-foundation/01-VALIDATION.md` [✓]
- `.planning/phases/01-architecture-foundation/01-PLAN.md` [✓]
- `.planning/phases/02-profile-system/02-01-PLAN.md` [✓]
- `.planning/phases/02-profile-system/SUMMARY.md` [✓]
- `.planning/phases/03-gpu-governor-core/03-01-PLAN.md` [✓]
- `.planning/phases/03-gpu-governor-core/SUMMARY.md` [✓]
- `.planning/phases/04-gpu-governor-control/04-01-PLAN.md` [✓]
- `.planning/phases/04-gpu-governor-control/SUMMARY.md` [✓]

### Next Session Actions
- Execute Phase 5: System Tweaks (performance mode, game mode, apply/revert)
- Execute Phase 6: Screenshots (region capture, full screen, save to file)

---
*This state file tracks progress between development sessions.*
