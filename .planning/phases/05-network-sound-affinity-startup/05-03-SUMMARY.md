---
phase: 5
plan: 03
subsystem: Affinity & Startup
tags: [affinity, startup, pinvoke, registry, schtasks, wasapi, bugfix]
key-files:
  created:
    - src/VainTools.Tests/TweakToggleViewModelTests.cs
  modified:
    - src/VainTools.App/Services/IAffinityService.cs
    - src/VainTools.App/Services/AffinityService.cs
    - src/VainTools.App/Services/IStartupService.cs
    - src/VainTools.App/Services/StartupService.cs
    - src/VainTools.App/Services/SoundService.cs
    - src/VainTools.App/ViewModels/AffinityViewModel.cs
    - src/VainTools.App/ViewModels/StartupViewModel.cs
    - src/VainTools.App/ViewModels/TweakToggleViewModel.cs
    - src/VainTools.App/Features/Affinity/AffinityPage.xaml
    - src/VainTools.App/Features/Startup/StartupPage.xaml
    - src/VainTools.App/Features/Startup/StartupPage.xaml.cs
    - src/VainTools.App/Features/Network/NetworkPage.xaml
    - src/VainTools.App/Features/Network/NetworkPage.xaml.cs
    - src/VainTools.App/Features/Sound/SoundPage.xaml
    - src/VainTools.App/Features/Sound/SoundPage.xaml.cs
    - src/VainTools.App/App.xaml
    - .gitignore
metrics:
  tests_added: 53
  tests_total: 225
  build_warnings: 0
  build_errors: 0
---

# Plan 05-03 Summary: Affinity & Startup

## What Was Built

- **AffinityService / IAffinityService** — process enumeration and CPU affinity via
  P/Invoke (`GetProcessAffinityMask` / `SetProcessAffinityMask`), plus
  `GetSystemAffinityMask`. PID 0 and PID 4 are skipped; protected processes are
  skipped rather than failing the whole enumeration.
- **StartupService / IStartupService** — Run/RunOnce entries across HKCU, HKLM and
  WOW6432Node, and scheduled tasks with boot/logon triggers.
- **AffinityViewModel / StartupViewModel** — orchestration, async where I/O is involved.
- **AffinityPage / StartupPage** — custom layouts per the UI-SPEC.

## The plan was not executable as written

The first `dotnet build` of this plan produced **40 errors**. Wave 3 was committed
without ever being compiled. Beyond the build break, the committed code had four
runtime defects that only surfaced when actually exercised. All are fixed here.

### 1. Build break: duplicated `CpuAffinityViewModel`

Declared both in `ViewModels/CpuAffinityViewModel.cs` and nested at the bottom of
`ViewModels/AffinityViewModel.cs`. Every one of the 40 errors cascaded from this
(`CS0102`/`CS0111`/`CS0756`), and the `WMC0909`/`WMC1111`/`WMC9999` XAML errors were
the known masked cascade from a failed C# compile. Removed the duplicate.

### 2. Plan/implementation drift in the tests

The four test files were written against the plan's async API
(`AffinityPageViewModel`, `IReadOnlyList<ProcessInfo>`), not the code that was actually
committed (`AffinityViewModel`, `ProcessInfo(Id, Name, CpuCount)`). Rewritten against
the real surface.

### 3. WMI scheduled-task query failed inside the app

`ManagementObjectSearcher` over `MSFT_ScheduledTask` threw **"Invalid query"** in-process
although the same query worked from PowerShell. Replaced with `schtasks.exe /Query /FO
LIST /V` parsing, which is deterministic and reuses the same tool the toggle path needs.

`MSFT_ScheduledTask` also **does not support a WMI write path** — `Set-CimInstance`
returns "The requested operation is not supported" (verified). Enable/disable therefore
goes through `schtasks.exe /Change`, which was verified round-trip on a throwaway task.

### 4. UI-thread deadlock (app freeze)

`_processRunner.RunAsync(...).GetAwaiter().GetResult()` blocked the UI thread on a task
that resumes on the captured `SynchronizationContext` — a classic async deadlock. The
Startup page froze on load. The process-running paths are now genuinely async and awaited.

### 5. Mass-disabling scheduled tasks (data loss) — CRITICAL

`ToggleSwitch.Toggled` on a row bound to an immutable record **also fires when the
binding pushes a value into the control**. The handler treated that echo as a user
action, so `Refresh → repopulate → rebind → Toggled → Toggle → Refresh` recursed without
end.

This **disabled 39 real scheduled tasks on the development machine** (`.NET Framework
NGEN`, `CertificateServicesClient`, `Data Integrity Scan`, `SystemSoundsService`,
`Wininet\CacheTask`, `FanControl`, `TRCCAppStartup`, …). All 39 were re-enabled and the
disabled-task count returned to its 55 baseline.

Fix: `StartupViewModel.IsUserToggle(entry, newIsOn)` — a toggle is only acted on when it
differs from the model. A regression test pins this.

### 6. Every tweak page wrote to the registry on load

`TweakToggleViewModel.OnIsOnChanged` wrote on *any* `IsOn` change. Because the Sound and
Network pages use `{Binding IsOn, Mode=TwoWay}`, the binding pushes the control's initial
state into the view model after the page renders — so merely **opening** those pages
rewrote the registry. This had set `DisableAudioEnhancements = 1` (audio enhancements
off); that value does not ship with Windows and was deleted to restore the original state.

Fix: the pages handle `Toggled` and call `TweakToggleViewModel.ApplyToggle`, which
ignores a value equal to the last state observed from the registry.

Note: `Controls/TweakList.xaml` uses `x:Bind IsOn, Mode=TwoWay`, which initialises the
control from the view model rather than the reverse, and did not exhibit this. Only the
two `{Binding}` pages were affected.

### 7. Sound page crash on Refresh — access violation

`SoundService.GetDeviceFriendlyName` did:

```csharp
var ptr = Marshal.ReadIntPtr(var.pointerValue);  // pointerValue IS the string
var name = Marshal.PtrToStringUni(ptr);          // reads from a garbage address
Marshal.FreeCoTaskMem(ptr);                      // frees garbage
```

For a `VT_LPWSTR` PROPVARIANT the string pointer is stored **directly** in the union;
`ReadIntPtr` treated the first characters of the device name as an address and
dereferenced it. The `PROPVARIANT` is now read correctly and released with
`PropVariantClear` rather than a wrong `FreeCoTaskMem`.

### 8. Affinity and Startup had no way to scroll

The process list sat in an `Auto`-sized row, so the list was measured at its full content
height (~170 rows), overflowed the window and clipped everything below it, with no scroll
container anywhere. Affinity's list row is now `*` with a `ListView`; Startup wraps both
sections in one `ScrollViewer`.

## Verification

- `dotnet build` — **0 warnings / 0 errors**
- `dotnet test` — **225 passed / 0 failed** (53 new)
- Instrumented navigation walk (temporary, removed after the run): **all 28 pages
  navigate, 0 failures**
- Affinity at runtime: 168 processes enumerated, 16 CPUs read via P/Invoke for the
  selected process, all 16 mask bits reflected in the checkboxes
- Startup at runtime: 5 Run-key entries + 64 boot/logon scheduled tasks, name/folder
  split correct, enabled state read correctly
- Sound at runtime: 4 real devices with correct friendly names (previously an access
  violation); `"Speakers (Focusrite USB Audio)"` correctly identified as default
- **Opening every page now produces 0 registry writes and 0 scheduled-task changes**
  (asserted by diffing the app log before/after a full navigation sweep)

## Machine state restored

Two defects had modified the development machine. Both were repaired:

- 39 scheduled tasks re-enabled (disabled-task count back to its 55 baseline)
- `HKLM\...\Audio\DisableAudioEnhancements` deleted (does not ship with Windows)

`HKCU\...\Run` still contains a `-test` value left behind by the *previous* version of
`StartupServiceTests`, which wrote to the real registry Run key. That is not residue from
this plan; the rewritten tests touch no real registry key.

## Deviations

- Plan specified async `AffinityPageViewModel`/`StartupPageViewModel` over
  `IReadOnlyList` records; the committed code uses synchronous `AffinityViewModel` /
  `StartupViewModel`, matching the Phase 5 house style (Network, Sound). Kept the
  committed shape; only the genuinely I/O-bound scheduled-task calls are async.
- Plan specified `Microsoft.Win32.TaskScheduler`; `schtasks.exe` was used instead
  (already an available path, no new dependency, verified working).
- `AllowUnsafeBlocks` was added to the csproj by wave 3. Left in place; the current code
  does not require it.

## Self-Check: PASSED

- Build: 0 warnings, 0 errors
- Tests: 225 passed, 0 failed
- All 28 pages navigate; Affinity, Startup and Sound verified against live hardware
- No page writes to the machine on load
