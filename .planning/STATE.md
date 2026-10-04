---
gsd_state_version: "1.0"
milestone: v2.0
status: executing
stopped_at: Phase 5 verified — gaps found
last_updated: "2026-10-04T00:45:00.000Z"
state_head: 254e5330b27107f5e03d5cbbb8cd4064a408ab6a
progress:
  total_phases: 10
  completed_phases: 4
  total_plans: 8
  completed_plans: 6
name: Real Vain Toolbox Rebuild
current_phase_name: Network, Sound, Affinity & Startup
current_phase: 5
---

# Vain Toolbox - Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-10-03)

**Core value:** The full Vain Toolbox feature surface, reimplemented in WinUI 3 with a faithful navigation structure.
**Current focus:** Phase 5 — Network, Sound, Affinity & Startup

## Current Position

Phase 5 of 10 — Network, Sound, Affinity & Startup
Plan 3 of 3 — complete, but phase verification found gaps
Status: 3 requirements unmet (SND-02 volume mixer, AFF-03 affinity rule persistence,
STR-03 delete startup entry); 2 unverified (NET-02/NET-03 DNS/NTP writes)

```
[===============                                   ] 45%
```

## Session Tracking

### Last Updated

- **Phase 5 verified — gaps found.** All four pages work and the wave-3 defects are
  fixed, but three requirements are not implemented. See
  `.planning/phases/05-network-sound-affinity-startup/VERIFICATION.md`.

### Phase 5 Outcome

- `Services/AffinityService.cs` — P/Invoke affinity read/write, process enumeration,
  system mask; skips PID 0/4 and protected processes
- `Services/StartupService.cs` — Run/RunOnce across HKCU/HKLM/WOW6432Node, plus
  boot/logon scheduled tasks via `schtasks.exe`
- `Services/SoundService.cs` — WASAPI device enumeration and volume/mute control
- `Services/NetworkService.cs` — adapter enumeration and DNS/NTP settings
- Pages: Network, Sound, Affinity, Startup (all custom layouts per the UI-SPEC)
- **225 tests pass** (was 172); 0 warnings / 0 errors
- Runtime-verified: all 28 pages navigate; Affinity read 168 processes and 16 CPUs on
  this machine; Startup read 5 Run keys + 64 boot/logon tasks; Sound read 4 real
  devices with correct friendly names

### Phase 5 notes — wave 3 was committed without compiling

Wave 3 (Affinity + Startup) was committed in a state that produced **40 build errors**.
Fixing it exposed four runtime defects that only appear when the pages are actually
exercised. Two of them had modified this machine; both were repaired. See
`05-03-SUMMARY.md` for the full account.

The important lesson for future phases:

- **`ToggleSwitch.Toggled` fires when the binding pushes a value into the control**, not
  only on user interaction. A handler that writes to the system on every `Toggled` will
  recurse (`Refresh → rebind → Toggled → write → Refresh`). Guard the handler by comparing
  the new value against the last state read from the system. This disabled 39 real
  scheduled tasks before it was caught.
- **`{Binding IsOn, Mode=TwoWay}` pushes the control's initial state into the view model**
  after the page renders. `x:Bind` (as `Controls/TweakList.xaml` uses) initialises from
  the view model instead and does not have this problem. Prefer `x:Bind`; where the rows
  are created by a view model and `x:Bind` is impossible, handle `Toggled` explicitly.
- **A `Auto`-sized grid row gives its child unbounded height.** A list in such a row is
  measured at full content height and clips everything below it, with no scrollbar. Put
  scrollable content in a `*` row.

### Phase 4 Outcome

- Security, Performance and Power pages with a power-plan editor
- `04-VERIFICATION.md` records the phase verification

### Phase 3 Outcome

- `Models/RegistryTweak.cs` + `Services/RegistryTweakService.cs` — one engine serving
  every toggle: read / apply / revert, elevation detection, 64-bit-then-32-bit registry
  view fallback, `RemoveKeyWhenDisabled` for key-presence switches, and
  `RestartExplorerAsync` (confirmed, never automatic)
- `Services/TweakCatalog.cs` — 20 real tweaks using the key paths and value names
  recovered from the binary
- `Controls/TweakList` — shared presenter; pages supply title + tweak set
- Pages: General, Explorer, Context Menu, Visual, Date & Time, Settings Visibility, System
- **130 tests pass** (was 113); 0 warnings / 0 errors
- Runtime-verified: all 20 tweaks read on this machine, all 7 pages navigate, and an
  apply→read→revert round-trip on a sandbox key returned Unset→Enabled→Disabled

### Phase 3 notes

- The test host runs **elevated**, so the admin-refusal path is covered by an
  assertion that branches on `IsElevated` rather than assuming non-elevation.
- Reading the catalog on this machine showed most tweaks as `Unset` (the OS default
  state) rather than Enabled/Disabled — that is expected and is why
  `DefaultWhenUnset` exists.

### Phase 2 Outcome

- `Services/SystemInfoService.cs` — WMI machine summary, per-field fault tolerant
- `Services/VainProfileService.cs` — `.vain` parser/writer matching the recovered schema
  (UTF-16 + BOM-less UTF-16 + UTF-8 sniffing; misspelled `Executeables` preserved)
- `Features/Home/HomePage` — live CPU/motherboard/OS/RAM/GPU/driver cards + quick actions
- `Features/VainTools/VainToolsPage` — picker import, drag-and-drop import,
  restore-defaults confirmation, vain.zone link
- `VainTools.Tests` now references `VainTools.App` (needed to test the services)
- **113 tests pass** (was 99); 0 warnings / 0 errors
- Runtime-verified on this machine: CPU "AMD Ryzen 7 5800X 8-Core Processor",
  GPU "NVIDIA GeForce RTX 5070" driver 32.0.16.1714, RAM 31.9 GB;
  a valid `.vain` imported as 1 profile / 3 settings / 3 DWORDs, a malformed one
  rejected with the user-facing message

### Phase 2 deviations (honest scope)

- **VAIN-03 is partial.** The restore-defaults confirmation flow and the documented
  section list (Sound, Security, Performance) are implemented, but *applying* the
  defaults needs the Sound (Phase 5), Security (Phase 4) and Performance (Phase 4)
  services. The UI says so explicitly instead of pretending to have changed anything.
- Import parses, validates and persists profiles but does not apply them to the
  NVIDIA driver — that is Phase 8 (DRS interop).

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
- **`x:Bind` inside a `DataTemplate` is scoped to the template's `x:DataType`.** A
  template typed to a row model cannot reach the page's ViewModel, so
  `{x:Bind ViewModel.SomeCommand}` fails to compile with a *masked* WMC9999. Put the
  value in the element's `Tag` and handle the event in code-behind instead.
- **Encoding sniffing for XML**: `XDocument.Load(stream)` trusts the
  `encoding=` declaration, so a UTF-8 file declaring `utf-16` mis-decodes. Read bytes,
  sniff BOM / NUL bytes, decode to string, then `XDocument.Parse(text)`.
- **`XmlWriter` async**: `FlushAsync` throws `InvalidOperationException` unless
  `XmlWriterSettings.Async = true`.
- **Referencing the app project from the test project makes WinRT resolvable**, which
  can invalidate tests that asserted a `COMException` caused by the old host limitation
  rather than real behaviour. Re-check such tests when adding the reference.
- **A namespace segment and a type name can collide.** `Features.General.GeneralPage`
  is ambiguous when a `GeneralPage` type is also in scope; write
  `typeof(VainTools.App.Features.General.GeneralPage)` in code-behind.
- **Never test against real registry keys.** Registry tests use a throwaway
  `HKCU\Software\VainTools\Test\<guid>` key and delete it in `Dispose`; a mistake
  there would modify the developer's machine.
- **A running app locks its own exe.** `dotnet build` fails with MSB3021/MSB3027 if
  `VainTools.App.exe` is still running from a previous smoke test — kill it first.

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

**Last session:** 2026-10-04T00:45:00.000Z
**Stopped at:** Phase 5 verified — gaps found, 3 requirements unmet
**Resume file:** .planning/phases/05-network-sound-affinity-startup/VERIFICATION.md

Resume with: close the gaps in Phase 5 (SND-02, AFF-03, STR-03) before planning Phase 6

---
*This state file tracks progress between development sessions.*
