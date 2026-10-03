# Phase 2: Home & Vain Tools — PLAN

**Phase:** 2 of 10
**Goal:** Deliver the landing page (system summary + quick actions) and the app's own
settings page, including the `.vain` profile import pipeline.
**Requirements:** HOME-01, HOME-02, HOME-03, VAIN-01, VAIN-02, VAIN-03, VAIN-04
**Depends on:** Phase 1 (real navigation shell) — complete

## Research notes (from ground truth)

Recovered from `Vain Toolbox.exe`; see
`.planning/research/VAIN-TOOLBOX-GROUND-TRUTH.md`.

**Home page real content**
- WMI: `SELECT Name FROM Win32_Processor`, `SELECT Manufacturer FROM Win32_BaseBoard`
- Displays: `Manufacturer`, `Model`, `Driver Version`, `Select driver version...`
- Placeholder fallbacks when a query fails: `Unknown CPU`, `Unknown GPU`,
  `Unknown Motherboard`, `Unknown RAM`
- Also surfaces: `Windows Insider Program` toggle, `GPU Performance Counters for All Users`

**Vain Tools page real content**
- `Import .vain profile` (picker + drag-and-drop; "Drop a .vain profile exported from
  Vain Toolbox.")
- `Restore Vain Defaults` — confirmation text: "The following settings will be changed
  to Vain defaults:" covering **Sound, Security, Performance** sections
- Shell-tweak application uses `taskkill.exe /f /im explorer.exe` then relaunch
- Branding: `https://vain.zone`, "Visit vain.zone for more tools"

**`.vain` file format** (recovered verbatim from the binary)
```xml
<?xml version="1.0" encoding="utf-16"?>
<ArrayOfProfile xmlns:xsd="http://www.w3.org/2001/XMLSchema"
                xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Profile>
    <Executeables>…</Executeables>
    <Settings>
      <ProfileSetting>
        <SettingNameInfo>…</SettingNameInfo>
        <SettingID>…</SettingID>
        <SettingValue>…</SettingValue>
        <ValueType>Dword|Binary|AnsiString</ValueType>
      </ProfileSetting>
    </Settings>
  </Profile>
</ArrayOfProfile>
```
Note the misspelling `Executeables` and that the encoding is **UTF-16** — both are
load-bearing for compatibility.

## Scope boundary (important)

Phase 2 imports and validates `.vain` files and **round-trips their contents**.
It does **not** apply the settings to the NVIDIA driver — that requires the NVAPI DRS
interop built in Phase 8. The import pipeline must therefore:
- parse and validate the document,
- surface a clear summary of what the file contains,
- persist the parsed profiles for Phase 8 to consume,
- and explicitly state that applying is not yet available.

Doing otherwise would mean shipping a button that silently does nothing.

## Tasks

### Task 2-01 — System information service
**Deliverable:** `Services/ISystemInfoService.cs` + `SystemInfoService.cs`
- Query CPU name (`Win32_Processor`), motherboard manufacturer/model (`Win32_BaseBoard`)
- Report OS version/build, installed RAM, GPU name, GPU driver version
- Every field independently fault-tolerant → fall back to `Unknown CPU` / `Unknown GPU` /
  `Unknown Motherboard` / `Unknown RAM` rather than throwing
- Use `System.Management` (WMI) for the Win32 queries; add the package reference
- Return an immutable `SystemInfo` record

**Verify:** unit tests with a fake WMI provider cover the fallback paths.

### Task 2-02 — Home page
**Deliverable:** `Features/Home/HomePage.xaml(.cs)` + `HomeViewModel.cs`
- Cards: OS, CPU, Motherboard, RAM, GPU, GPU driver version
- Quick actions: navigate to GPU, Display, NVIDIA, Tools (via `NavigationRequestedMessage`)
- App version line (HOME-03)
- Loading and error states; no unhandled exceptions when a query fails

**Verify:** all fields render with real data on this machine; app still builds clean.

### Task 2-03 — `.vain` profile model and parser
**Deliverable:** `Models/VainProfile.cs`, `Services/IVainProfileService.cs` +
`VainProfileService.cs`
- Model matching the recovered XML exactly (including `Executeables` and UTF-16)
- `ValueType` enum: `Dword`, `Binary`, `AnsiString` (+ `Bitmask` for the UI layer)
- Parse with `XmlSerializer`; accept both UTF-16 and UTF-8 input
- **Validate and reject**: malformed XML, a root element that is not `ArrayOfProfile`,
  a file whose settings are not DWORD-importable ("That profile did not contain any
  importable DWORD settings."), and non-`.vain` extensions
- Expose a `VainProfileParseResult` (success + profiles, or failure + reason)

**Verify:** unit tests for a valid file, malformed XML, wrong root, empty settings,
and a UTF-8-encoded file.

### Task 2-04 — Vain Tools page: import + restore defaults
**Deliverable:** `Features/VainTools/VainToolsPage.xaml(.cs)` + `VainToolsViewModel.cs`
- `Import .vain profile` button → `IFilePickerService.PickOpenFileAsync(".vain")`
- Drag-and-drop target accepting `.vain` (set `AllowDrop`, handle `DragOver`/`Drop`)
- On success: show a summary (profile count, setting count) via `IInfoBarService`
- On failure: show the specific rejection reason
- Imported profiles persisted via `ISettingsService` under a `Vain.ImportedProfiles` key
- `Restore Vain Defaults` button → `IDialogService.ConfirmAsync` with the real wording,
  listing the Sound/Security/Performance sections; on confirm, record the intent and
  report that application arrives with the relevant phases
- Branding line linking to `https://vain.zone`

**Verify:** importing a synthetic valid `.vain` file shows the summary; a malformed one
shows the reason; the drop target rejects a non-`.vain` file.

## Success criteria (from ROADMAP)

1. Home shows OS/CPU/RAM/GPU summary and driver version
2. Home offers working quick actions
3. User can import a `.vain` file via picker and via drag-and-drop
4. A foreign or malformed `.vain` file is rejected with a clear message
5. "Restore Vain defaults" applies the documented default set after confirmation

> Criterion 5 is **partially** met in this phase: the confirmation flow and the
> documented default set are implemented, but *applying* them depends on the Sound
> (Phase 5), Security (Phase 4) and Performance (Phase 4) services. The phase will
> state this explicitly rather than claim completion.

## Risks

| Risk | Mitigation |
|---|---|
| WMI unavailable or slow on some machines | Per-field try/catch + timeout; fall back to `Unknown …` labels as the real app does |
| `System.Management` needs a package reference and may be trim-unfriendly | Add explicit `PackageReference`; avoid reflection-based WMI paths |
| `.vain` files in the wild may vary | Parse tolerantly; accept UTF-8 as well as UTF-16; never crash on a bad file |
| Import looks like it applies settings | UI states clearly that applying arrives with the DRS phase |

## Out of scope for this phase

- Applying DRS settings (Phase 8)
- The actual Sound/Security/Performance default values (Phases 4 and 5)
- Any NVIDIA driver interop
