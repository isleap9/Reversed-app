---
milestone: v2.0
name: Real Vain Toolbox Rebuild
status: planning
progress:
  phases_total: 10
  phases_complete: 0
  plans_total: 26
  plans_complete: 0
---

# Vain Toolbox - Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-10-03)

**Core value:** The full Vain Toolbox feature surface, reimplemented in WinUI 3 with a faithful navigation structure.
**Current focus:** Phase 1 — Real Navigation Shell

## Current Position

Phase 1 of 10 — Real Navigation Shell
Plan 0 of 2 — not started
Status: Ready to plan

```
[                                                  ] 0%
```

## Session Tracking

### Last Updated
- **Milestone reset:** v1.0 (guessed features) retired; v2.0 planned from ground truth

### What Changed
The previous milestone built six pages — Dashboard, GpuGovernor, Profiles,
SystemTweaks, Screenshots, Taskbar — from a *guessed* feature list. Static analysis
of the shipped `Vain Toolbox.exe` proved none of those pages exist in the real
product. PROJECT.md, REQUIREMENTS.md and ROADMAP.md were rewritten against
`.planning/research/VAIN-TOOLBOX-GROUND-TRUTH.md`.

### Build Status
- `dotnet build --no-incremental` — succeeded, **0 warnings / 0 errors**
- `dotnet test` — 99 passed / 0 failed
- App verified to launch (`VainTools.App.exe`, window "Vain Tools", Responding=True)

### Resolved Build Issues (still relevant)
1. **WMC9999 "Could not find any resources appropriate for the specified culture"**
   was a *masked* error — the WinUI markup compiler failed while formatting a
   binding warning, so the real error was hidden. Actual causes:
   - `x:Bind ViewModel.RefreshCommand` — `[RelayCommand]` on `RefreshMetricsAsync()`
     generates `RefreshMetricsCommand`.
   - `FanCurveEditor`'s `x:Class` namespace disagreed with its code-behind.
   *(Both files are being retired in Phase 1, but the diagnostic technique remains useful.)*
2. **58x MVVMTK0045** — field-based `[ObservableProperty]` is not AOT compatible in
   WinRT/WinUI 3. All converted to partial properties
   (`[ObservableProperty] public partial T Name { get; set; }`).
   **Use partial properties for all new ViewModels.**

### Debugging Note — masked XAML errors
`WMC9999` on this stack is often masked. To see the real error, run the XAML compiler
directly against the generated input and read its JSON output:

```bash
XAMLCOMPILER=~/.nuget/packages/microsoft.windowsappsdk.winui/2.3.0/tools/net472/XamlCompiler.exe
cp src/VainTools.App/obj/Debug/net10.0-windows10.0.26100.0/win-x64/input.json "$HOME/xctest/in.json"
"$XAMLCOMPILER" 'C:\Users\<user>\xctest\in.json' 'C:\Users\<user>\xctest\out.json'
# inspect MSBuildLogEntries in out.json for entries with Type != 0
```
JSON paths must be native Windows paths. Note that a `write_file` guard blocks
overwriting files only partially read — delete and rewrite, or use `patch`.

## Recent Decisions

| Decision | Rationale |
|----------|-----------|
| Reimplement in C#/WinUI 3, not port the native binary | `Vain Toolbox.exe` is native C++/WinRT — no decompilation path to C# |
| Build the real 30-page shell first | Makes progress visible; stops further drift from the real structure |
| Retire the six guessed pages | They do not exist in Vain Toolbox |
| GPU area = NVIDIA DRS + EDID, not NVML monitoring | The real app edits driver settings and display overrides |
| Keep `.vain` XML format compatible | Documented on-disk contract users may already have files for |

## Pending Todos

- None captured.

## Blockers/Concerns

- **Elevation**: EDID overrides, driver deletion and some registry writes need
  administrator. Detect and explain rather than failing silently.
- **`nvapi64.dll` interop**: Phase 8's write paths need testing against real NVIDIA
  hardware before they can be called done. Read paths are lower risk.
- **Irreversible operations**: DRS "restore all defaults" and driver deletion must
  sit behind explicit confirmation (`safety.always_confirm_destructive`).

## Session Continuity

Resume with: `/gsd-plan-phase 1`

---
*This state file tracks progress between development sessions.*
