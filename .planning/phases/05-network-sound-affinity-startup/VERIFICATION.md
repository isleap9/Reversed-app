---
status: gaps_found
phase: 5
date: 2026-10-04
---

# Phase 5 Verification: Network, Sound, Affinity & Startup

## Verdict: ⚠️ GAPS FOUND

The four pages exist, build cleanly and work for their primary use cases, but **three
requirements are not implemented** and **two are partial**. Phase 5 should not be marked
complete until the gaps are closed or explicitly moved out of scope.

Verified by: clean build, 225 passing tests, an instrumented navigation sweep of all 28
pages, and direct exercise of Affinity, Startup and Sound against this machine's hardware.

## Requirements Coverage

### Network

| ID | Description | Status | Evidence |
|----|-------------|--------|----------|
| NET-01 | Network page lists adapters | ✅ PASS | `NetworkPageViewModel.Adapters` bound to `NetworkService.GetAdaptersAsync()`; 6 `NetworkServiceTests` pass |
| NET-02 | User can set DNS servers | ⚠️ PARTIAL | `DnsServer` textbox + `SaveDnsNtp` writes `NameServer`; the write path is **not** runtime-verified (needs elevation) |
| NET-03 | User can manage NTP servers | ⚠️ PARTIAL | `NtpServer` textbox + write to `W32Time\Parameters`; same caveat as NET-02 |
| NET-04 | Offload settings viewed and toggled | ✅ PASS | `TweakCatalog.Network` (3 tweaks: LSO v2 IPv4, RSS, checksum offload IPv4); verified they no longer write on page load |

### Sound

| ID | Description | Status | Evidence |
|----|-------------|--------|----------|
| SND-01 | Sound page shows audio devices | ✅ PASS | Runtime: 4 devices with correct friendly names, e.g. `Speakers (Focusrite USB Audio)` marked default |
| SND-02 | Volume mixer is accessible | ❌ **FAIL** | `ISoundService.GetVolumeInfoAsync`/`SetVolumeAsync`/`SetMuteAsync` are implemented and the WASAPI interop is complete, but **`SoundPage.xaml` has no slider or volume control** — nothing in the UI calls them |
| SND-03 | Spatial audio can be toggled | ✅ PASS | `sound.spatial-audio` tweak in `TweakCatalog.Sound` |
| SND-04 | Audio enhancements can be toggled | ✅ PASS | `sound.audio-enhancements` tweak; verified it no longer writes on load |

### Affinity

| ID | Description | Status | Evidence |
|----|-------------|--------|----------|
| AFF-01 | User can view running processes | ✅ PASS | Runtime: 168 processes enumerated with names, PIDs and per-process CPU counts |
| AFF-02 | User can set CPU affinity for a process | ✅ PASS | Runtime: 16 CPUs read via P/Invoke for the selected process, all 16 mask bits reflected in the checkboxes; `SetAffinityMask` round-trip covered by tests |
| AFF-03 | Affinity rules can be saved and reapplied | ❌ **FAIL** | No persistence exists — no `AffinityRule` model, no save/apply-rule path in `AffinityViewModel` or `IAffinityService` |

### Startup

| ID | Description | Status | Evidence |
|----|-------------|--------|----------|
| STR-01 | Lists startup entries grouped by source | ✅ PASS | Two sections (Run Keys, Scheduled Tasks); each row carries its `Source` (`HKCU`/`HKLM`/`Scheduled Task`) |
| STR-02 | User can enable/disable a startup entry | ✅ PASS | Run keys via `-` prefix, tasks via `schtasks.exe /Change`; verified working after the recursion fix |
| STR-03 | User can delete a startup entry | ❌ **FAIL** | No delete path in `IStartupService` or `StartupViewModel` |
| STR-04 | Scheduled tasks listed alongside Run-key entries | ✅ PASS | Runtime: 5 Run-key entries + 64 boot/logon scheduled tasks on one page |

## Summary

| Status | Count |
|--------|-------|
| ✅ PASS | 9 |
| ⚠️ PARTIAL | 2 |
| ❌ FAIL | 3 |

**Unmet:** SND-02 (volume mixer), AFF-03 (affinity rule persistence), STR-03 (delete entry).
**Unverified:** NET-02, NET-03 (DNS/NTP writes not exercised end-to-end).

## Defects found and fixed during verification

Wave 3 was committed without ever compiling. See `05-03-SUMMARY.md` for the full account.
The material ones:

1. **40 build errors** from a duplicated `CpuAffinityViewModel`.
2. **Mass-disabling of scheduled tasks (data loss)** — `ToggleSwitch.Toggled` fires when the
   binding pushes a value in, so the handler recursed. This disabled **39 real scheduled
   tasks** on this machine. All were re-enabled; the disabled-task count returned to its 55
   baseline. Fixed with `StartupViewModel.IsUserToggle` + regression test.
3. **Registry writes on page load** — the `TwoWay` binding's initial push made opening Sound
   or Network rewrite the registry (`DisableAudioEnhancements` had been set to 1; that value
   does not ship with Windows and was deleted). Fixed via explicit `Toggled` handling and
   `TweakToggleViewModel.ApplyToggle`.
4. **Sound page crash** — `PROPVARIANT` `VT_LPWSTR` was dereferenced with `Marshal.ReadIntPtr`
   and freed with `FreeCoTaskMem`; both were wrong and caused an access violation on Refresh.
5. **UI-thread deadlock** — Startup froze on load via `.GetAwaiter().GetResult()`.
6. **No scrollable region** on Affinity or Startup — an `Auto`-sized row measured the
   170-row list at full height and clipped everything below it.

## Machine state

Both machine-modifying defects were repaired:

- 39 scheduled tasks re-enabled (55 disabled = baseline)
- `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Audio\DisableAudioEnhancements` deleted

`HKCU\...\Run` retains a `-test` value written by the *previous* version of
`StartupServiceTests`, which wrote to the real registry Run key. Not residue from this
plan; the rewritten tests touch no real registry key.

## Recommendation

Close the three FAILs (SND-02, AFF-03, STR-03) and exercise the two DNS/NTP write paths
before marking Phase 5 complete. Alternatively, move them out of scope in `PROJECT.md`
with a reason.
