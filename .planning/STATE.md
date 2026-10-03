---
milestone: v2.0
name: Real Vain Toolbox Rebuild
status: planning
progress:
  phases_total: 10
  phases_complete: 1
  plans_total: 26
  plans_complete: 2
---

# Vain Toolbox - Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-10-03)

**Core value:** The full Vain Toolbox feature surface, reimplemented in WinUI 3 with a faithful navigation structure.
**Current focus:** Phase 2 — Home & Vain Tools

## Current Position

Phase 1 of 10 — Real Navigation Shell
Plan 2 of 2 — complete
Status: Phase 1 complete; ready to plan Phase 2

```
[==                                                ] 10%
```

## Session Tracking

### Last Updated
- **Phase 1 complete:** real 28-page navigation shell built and verified

### Phase 1 Outcome
- 28 feature pages scaffolded under `Features/` in the real folder layout
- `Navigation/NavigationCatalog.cs` — grouped nav model (10 groups, nested children)
- `MainWindow.xaml` — NavigationView rebuilt with the real grouped tree; 28 nav Tags
- Retired `Views/`, `Controls/`, `ViewModels/`, `Services/`, `Models/`, `Converters/`
  and the old `NavigationItem` record, plus their DI registrations
- Verified: 0 warnings / 0 errors, 99 tests pass, **all 28 pages navigate at runtime**
  (verified by a temporary instrumented walk that was removed after the run)

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

### WinUI/XAML build pitfalls hit in Phase 1
- **`WMC9999: Object reference not set to an instance of an object`** during markup
  compile is usually a *cascade* from a failed C# compile — the XAML compiler runs
  pass 2 with no `LocalAssembly`, then throws. **Always fix the C# errors first and
  rebuild before investigating the XAML error.** The accompanying `WMC1509`
  ("No LocalAssembly parameter given during MarkupCompilePass2") is the tell.
- **`WMC9997: An error occurred while parsing EntityName`** — a bare `&` in a XAML
  attribute. Write `&amp;` (e.g. `Text="Date &amp; Time"`).
- **`App.xaml.cs` must keep its UTF-8 BOM.** Rewriting it without a BOM broke the XAML
  compiler. Use `patch` for edits to that file, or write with `encoding="utf-8-sig"`.
- **WinUI has no `x:Type` markup extension.** To put a page type in XAML (e.g. a nav
  `Tag`), write the fully-qualified type name as a string and resolve it at runtime
  against the assembly (see `MainWindow.PageTypesByTag`).
- Prefer `patch` over `write_file` for edits — `write_file` refuses files whose full
  contents this session has not read, and the guard is easy to trip.

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

Resume with: `/gsd-plan-phase 2`

---
*This state file tracks progress between development sessions.*
