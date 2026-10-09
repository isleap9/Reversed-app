---
phase: 06-apps
reviewed: 2026-10-09T00:00:00Z
depth: standard
files_reviewed: 35
files_reviewed_list:
  - src/VainTools.App/App.xaml.cs
  - src/VainTools.App/Features/Apps/AppxManagerPage.xaml
  - src/VainTools.App/Features/Apps/AppxManagerPage.xaml.cs
  - src/VainTools.App/Features/Apps/InstalledAppsPage.xaml
  - src/VainTools.App/Features/Apps/InstalledAppsPage.xaml.cs
  - src/VainTools.App/Features/Apps/OptionalFeaturesPage.xaml
  - src/VainTools.App/Features/Apps/OptionalFeaturesPage.xaml.cs
  - src/VainTools.App/Features/Apps/StorePage.xaml
  - src/VainTools.App/Features/Apps/StorePage.xaml.cs
  - src/VainTools.App/Services/AppxPackage.cs
  - src/VainTools.App/Services/AppxPackageService.cs
  - src/VainTools.App/Services/IAppxPackageService.cs
  - src/VainTools.App/Services/IInstalledAppsService.cs
  - src/VainTools.App/Services/IOptionalFeaturesService.cs
  - src/VainTools.App/Services/IProcessRunner.cs
  - src/VainTools.App/Services/IStoreService.cs
  - src/VainTools.App/Services/InstalledApp.cs
  - src/VainTools.App/Services/InstalledAppsService.cs
  - src/VainTools.App/Services/OptionalFeature.cs
  - src/VainTools.App/Services/OptionalFeaturesService.cs
  - src/VainTools.App/Services/ProcessRunner.cs
  - src/VainTools.App/Services/StoreApp.cs
  - src/VainTools.App/Services/StoreService.cs
  - src/VainTools.App/ViewModels/AppxManagerViewModel.cs
  - src/VainTools.App/ViewModels/InstalledAppsViewModel.cs
  - src/VainTools.App/ViewModels/OptionalFeaturesViewModel.cs
  - src/VainTools.App/ViewModels/StoreViewModel.cs
  - src/VainTools.Tests/AppxManagerViewModelTests.cs
  - src/VainTools.Tests/AppxPackageServiceTests.cs
  - src/VainTools.Tests/InstalledAppsServiceTests.cs
  - src/VainTools.Tests/InstalledAppsViewModelTests.cs
  - src/VainTools.Tests/OptionalFeaturesServiceTests.cs
  - src/VainTools.Tests/OptionalFeaturesViewModelTests.cs
  - src/VainTools.Tests/StoreServiceTests.cs
  - src/VainTools.Tests/StoreViewModelTests.cs
findings:
  critical: 3
  warning: 10
  info: 5
  total: 18
status: issues_found
---

# Phase 6: Code Review Report

**Reviewed:** 2026-10-09
**Depth:** standard
**Files Reviewed:** 35
**Status:** issues_found

## Summary

Reviewed the Apps phase: four pages (Appx Manager, Installed Apps, Optional Features, Store), their view models, the WinRT / registry / DISM / winget services, `ProcessRunner`, and the tests. OptionalFeaturesViewModelTests.cs and StoreViewModelTests.cs were not read in full (only grepped), so no findings are raised against them.

The layering is clean and the view models are consistently guarded (elevation check, confirmation, error InfoBar). The defects are in the places the unit tests cannot reach, because the process-launch and DISM/winget boundaries are mocked:

- The real uninstall launch path (`RunUninstallAsync`) is overridden in every test, so it is never exercised. As written it cannot run most real uninstall strings.
- DISM's "success, restart required" exit code is treated as failure.
- The Store page's Enter-key search reads a view-model property that has not yet been updated from the TextBox.

There are also several robustness gaps: provisioned-package removal, winget failures reported as "no results", output encoding, and re-entrancy.

No structural (fallow) findings were provided.

## Critical Issues

### CR-01: Uninstall passes the entire registry command line to ShellExecute as a file name

**File:** `src/VainTools.App/ViewModels/InstalledAppsViewModel.cs:211-219`
**Issue:** `new ProcessStartInfo(command) { UseShellExecute = true }` sets `FileName` to the whole `UninstallString`. Real uninstall strings are command lines, not file paths. Examples are `"C:\Program Files\X\unins000.exe" /SILENT`, `MsiExec.exe /X{GUID}` and `C:\...\setup.exe -uninstall`. ShellExecute does not split arguments out of `lpFile`, so these fail with "The system cannot find the file specified". Only a bare, unquoted path with no arguments works. The Uninstall button is therefore broken for most entries, and in particular for every MSI-based program. The tests override `RunUninstallAsync`, so this path has zero coverage. UAT item 7 passed against the mock, not the real launch.
**Fix:** Split the command into executable and arguments, or hand the line to the command interpreter. For example:
```csharp
protected virtual async Task RunUninstallAsync(string command)
{
    var startInfo = new ProcessStartInfo("cmd.exe")
    {
        UseShellExecute = false,
        CreateNoWindow = true,
    };
    startInfo.ArgumentList.Add("/c");
    startInfo.ArgumentList.Add(command);   // the user already confirmed this exact string

    using var process = Process.Start(startInfo)
        ?? throw new InvalidOperationException("The uninstaller could not be started.");
    await process.WaitForExitAsync();
    if (process.ExitCode != 0)
        throw new InvalidOperationException($"The uninstaller exited with code {process.ExitCode}.");
}
```
Alternatively, parse with `CommandLineToArgvW` and start the first token directly. Add an integration test that launches a real `cmd /c exit 0` style command through this path.

### CR-02: DISM exit code 3010 (success, restart required) is reported as a failure

**File:** `src/VainTools.App/Services/OptionalFeaturesService.cs:73-77` and `:90-94`
**Issue:** Both calls pass `/NoRestart`. When a feature change needs a reboot (Hyper-V, WSL, Windows Sandbox, .NET 3.5 and others), DISM applies the change and exits with `3010` (`ERROR_SUCCESS_REBOOT_REQUIRED`). The code treats every nonzero exit as failure. It throws "dism.exe failed to enable...", the view model shows "Enable failed", and it skips `RefreshAsync`. The user is told the operation failed when it succeeded, is never told to restart, and the list is left stale. Retrying then fails differently.
**Fix:** Treat 0 and 3010 as success and surface the reboot requirement.
```csharp
private const int ExitRestartRequired = 3010;
...
if (result.ExitCode is not (0 or ExitRestartRequired))
{
    throw new InvalidOperationException(...);
}
if (result.ExitCode == ExitRestartRequired)
{
    _logger.LogInformation("Feature {Feature} changed; restart required", featureName);
    // return a result type or raise an info message so the VM can say "Restart required".
}
```
Consider returning a small result record (`RestartRequired`) instead of `Task`, so the VM can tell the user.

### CR-03: Store Enter-key search runs with a stale query; Installed Apps filter is not live

**File:** `src/VainTools.App/Features/Apps/StorePage.xaml:46-49` (also `InstalledAppsPage.xaml:42-44`)
**Issue:** `Text="{x:Bind ViewModel.SearchQuery, Mode=TwoWay}"` on a `TextBox` uses the default `UpdateSourceTrigger`, which is `LostFocus` for `TextBox.Text`. `OnSearchKeyDown` (StorePage.xaml.cs:30-37) calls `SearchCommand` on Enter while the box still has focus. The view model's `SearchQuery` still holds the previous value, so the first search via Enter reports "Search skipped: no query", and later searches re-run the old query. Only the Search button works, because clicking it moves focus first. On Installed Apps, `OnSearchQueryChanged` is designed to filter as the user types, but the filter only fires when the box loses focus. The stale generated file under `obj/.../InstalledAppsPage.xaml` shows an earlier revision used `UpdateSourceTrigger=PropertyChanged`, so this looks like a regression.
**Fix:**
```xml
<TextBox ... Text="{x:Bind ViewModel.SearchQuery, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
```
Apply it to both pages.

## Warnings

### WR-01: Provisioned packages are "removed" with the per-user remove API; `IsProvisioned` is ignored

**File:** `src/VainTools.App/ViewModels/AppxManagerViewModel.cs:142`, `src/VainTools.App/Services/AppxPackageService.cs:49-57`
**Issue:** Every row, installed or provisioned, calls `RemovePackageAsync(FullName)`. That removes only the current user's registration. It does not deprovision. A provisioned-only row fails or does nothing, and a provisioned package that is also installed is removed for one user and re-appears for new users. `Refresh` concatenates both lists, so the same package normally appears twice (once as "Installed", once as "Provisioned") with identical `FullName`. The "Remove Package" button on a "Provisioned" row therefore does not do what the label implies. The confirmation dialog also does not distinguish the two cases.
**Fix:** Branch on `IsProvisioned` and call `PackageManager.DeprovisionPackageForAllUsersAsync(familyName)`, or `RemovePackageAsync(name, RemovalOptions.RemoveForAllUsers)`. Carry `PackageFamilyName` on `AppxPackage`, and de-duplicate or merge installed/provisioned rows by full name.

### WR-02: Package enumeration is admin-only and one bad package aborts the whole list; tests depend on the machine

**File:** `src/VainTools.App/Services/AppxPackageService.cs:28`, `:40`, `:82`; `src/VainTools.Tests/AppxPackageServiceTests.cs:16-70`
**Issue:**
- `PackageManager.FindPackages()` (all users) and `FindProvisionedPackages()` require elevation. A non-elevated session throws `UnauthorizedAccessException`, so the page shows "Load failed" even though the UI banner implies only the mutating operations need admin.
- `Map` reads `package.InstalledLocation.Path`. That `StorageFolder` getter can throw (`FileNotFoundException` / `UnauthorizedAccessException`) for staged, stale or inaccessible packages. One such package throws out of the `.Select(...)` and fails the entire enumeration. `Package.InstalledPath` (a plain string) avoids the StorageFolder activation.
- `AppxPackageServiceTests` call the real `PackageManager` and assert `NotEmpty` on provisioned packages. They fail or are flaky on a non-elevated or CI machine.

**Fix:** Use `FindPackagesForUser(string.Empty)` when not elevated. Use `package.InstalledPath` wrapped in a try/catch that falls back to an empty string. Mark the real-system tests with a trait or skip them when not elevated.

### WR-03: DISM feature name is spliced into a shell-style argument string

**File:** `src/VainTools.App/Services/OptionalFeaturesService.cs:70-71`, `:87-88`
**Issue:** `$"/Online /Enable-Feature /FeatureName:\"{featureName}\" /NoRestart"` builds a command line by interpolation. The feature name originates from DISM output, but the service takes an arbitrary string, so a value containing `"` can break out and add switches. This is exactly the T-06-09 class of defect the phase fixed for winget by introducing the argument-vector overload, and it was not applied here. Because `IProcessRunner` has both `(string, string)` and `(string, params string[])`, a call with exactly one argument silently binds to the unsafe string overload, which makes this easy to repeat.
**Fix:**
```csharp
await _processRunner.RunAsync(DismExecutable, "/Online", "/Enable-Feature", $"/FeatureName:{featureName}", "/NoRestart");
```
Also validate `featureName` against `^[A-Za-z0-9._-]+$`. Consider renaming the vector overload (for example `RunArgsAsync`) so the two cannot be confused.

### WR-04: Genuine winget failures are reported as "No apps found"

**File:** `src/VainTools.App/Services/StoreService.cs:70-76`
**Issue:** Any nonzero winget exit returns an empty list, including real failures: no network, a broken or unconfigured source (`-1978335294` "No sources configured", used in the test), a source agreement problem, or a corrupt index. The page then shows the empty-state copy "No installable apps matched your search. Try a different query." The documented rationale only justifies treating the "no match" code (`-1978335212`, `0x8A150014`) that way. The comment says the error state is "reached through the exception path", but that path only fires when `winget.exe` is missing.
**Fix:** Return an empty list only for the known no-match exit code, and throw `InvalidOperationException` with the stdout/stderr text for every other nonzero code, so the VM's existing "Search failed" InfoBar is shown.

### WR-05: Query beginning with `-` is still parsed by winget as a switch; the T-06-09 claim is overstated

**File:** `src/VainTools.App/Services/StoreService.cs:65-66`; `src/VainTools.Tests/StoreServiceTests.cs:191-208`
**Issue:** The argument vector prevents shell-level splitting, but winget itself treats any argv element that starts with `-` or `--` as an option. A query such as `--help`, `-s` or `--source` is interpreted as a switch rather than as search text. The test `SearchApps_PassesQueryAsASingleArgument` only covers `"notepad++ --exact"`, which does not begin with a dash, so it does not prove the property its comment claims ("winget cannot be tricked into running --exact").
**Fix:** Pass the query via the option form: `"search", "--query", query, "--accept-source-agreements"`. Check that winget accepts a dash-leading value there, or reject or escape queries starting with `-`. Extend the test with a dash-leading query.

### WR-06: Uninstall result is never checked; success is reported unconditionally

**File:** `src/VainTools.App/ViewModels/InstalledAppsViewModel.cs:152-158`, `:213-218`
**Issue:** `RunUninstallAsync` discards the process exit code (and silently does nothing when `Process.Start` returns null). After it returns, the VM always shows the success InfoBar "Uninstall started ... has finished" and the status "Uninstalled {name}", even if the user cancelled the uninstaller, it crashed, or it returned a failure code. The InfoBar text ("started" / "has finished") is also self-contradictory. Many uninstallers (MSI bootstrappers, Squirrel) return before the work is done.
**Fix:** Surface the exit code (nonzero becomes an error). Make the wording accurate ("The uninstaller for X has exited."). Refresh without claiming "Uninstalled" unless the entry has disappeared from the list.

### WR-07: No re-entrancy guard on Remove / Uninstall / Enable / Disable

**File:** `src/VainTools.App/ViewModels/AppxManagerViewModel.cs:171`, `src/VainTools.App/ViewModels/InstalledAppsViewModel.cs:231`, `src/VainTools.App/ViewModels/OptionalFeaturesViewModel.cs:201-205`
**Issue:** `CanInstallApp` in `StoreViewModel` includes `!IsLoading`, but the other three view models do not. A second click (or a click on another row) while a confirmation dialog is open throws "Only a single ContentDialog can be open at a time" (caught and shown as a failure). A click while a long DISM/Appx operation is running starts a second concurrent mutation. `RefreshAsync` can also run concurrently with itself (the page's `OnNavigatedTo` plus the user's Refresh), and the later one wins arbitrarily.
**Fix:** Add `&& !IsLoading` to each `CanExecute`, add `partial void OnIsLoadingChanged` calling `NotifyCanExecuteChanged()` as `StoreViewModel` does, and set `IsLoading` before showing the confirmation dialog or use a separate busy flag.

### WR-08: `dism /Format:List` parser depends on English field labels

**File:** `src/VainTools.App/Services/OptionalFeaturesService.cs:131-135`
**Issue:** DISM output is localized. On a non-English Windows install the labels are not "Feature Name" / "State", so `ParseFeatures` silently returns an empty list. The page then displays "No Windows optional features were found on this system" with no error.
**Fix:** Force English output where possible, or use a locale-independent source (the `DismApi` P/Invoke, or `Get-WindowsOptionalFeature -Online | ConvertTo-Json` via PowerShell). At minimum, treat a successful DISM exit with non-empty stdout and zero parsed features as an error rather than an empty result.

### WR-09: Install confirmation hides flags that auto-accept license agreements

**File:** `src/VainTools.App/ViewModels/StoreViewModel.cs:139-142`, `src/VainTools.App/Services/StoreService.cs:88-90`
**Issue:** The dialog says "winget will run: install --id {Id} --exact". The real command line also includes `--accept-source-agreements --accept-package-agreements`. The user never sees the package's license terms and is not told that the app consents to them on their behalf. The dialog text is also not the command that actually runs.
**Fix:** Show the full argument list, or at least add a line such as "License and source agreements will be accepted automatically." Better, drop `--accept-package-agreements` and let the user opt in.

### WR-10: Process output is decoded with the OEM code page; there is no timeout or cancellation

**File:** `src/VainTools.App/Services/ProcessRunner.cs:37-75`; `src/VainTools.App/Services/OptionalFeaturesService.cs:49`, `:65`, `:83`
**Issue:**
- Neither `ProcessStartInfo` sets `StandardOutputEncoding` / `StandardErrorEncoding`. winget writes UTF-8 to the pipe, but .NET decodes with the console/OEM code page. Non-ASCII package names are garbled. The `…` truncation character in winget's table becomes 3 characters, which shifts every column offset on that row. `StoreService.MapRow` computes regions from character offsets, so truncated rows are misparsed or fall through to shape parsing.
- `RunAsync` has no `CancellationToken` or timeout. The `cancellationToken` parameters on `GetFeaturesAsync` / `EnableFeatureAsync` / `DisableFeatureAsync` are accepted and ignored. A hung `dism.exe` or `winget.exe` leaves `IsLoading` true with no way out.

**Fix:** Set `StandardOutputEncoding = Encoding.UTF8` for winget (or take an encoding parameter). Add an optional `CancellationToken` to `IProcessRunner.RunAsync`, kill the process tree on cancellation, and pass the token through from the services.

## Info

### IN-01: Elevated execution of a command read from a user-writable hive

**File:** `src/VainTools.App/Services/InstalledAppsService.cs:22-27`, `src/VainTools.App/ViewModels/InstalledAppsViewModel.cs:124-140`
**Issue:** HKCU uninstall entries are writable by any non-elevated process of the same user. The elevated app will run the command stored there. The confirmation dialog shows the command, which mitigates this, but the dialog does not say which hive the entry came from.
**Fix:** Show `app.RegistryPath` in the confirmation, or label HKCU-sourced entries.

### IN-02: `dism.exe` / `winget` resolved through the search path while elevated

**File:** `src/VainTools.App/Services/OptionalFeaturesService.cs:37`, `src/VainTools.App/Services/ProcessRunner.cs:21`
**Issue:** `CreateProcess` searches the application directory before System32. A planted `dism.exe` next to the app would run elevated.
**Fix:** Use `Path.Combine(Environment.SystemDirectory, "dism.exe")`. winget has to stay on PATH because it is an app execution alias.

### IN-03: Uninstall list de-duplicates and lists by display name only

**File:** `src/VainTools.App/Services/InstalledAppsService.cs:77-81`
**Issue:** Distinct programs that share a `DisplayName` (for example different versions or publishers) collapse into one, which hides the second. Entries flagged `SystemComponent=1` or with a `ParentKeyName` / `ReleaseType` (hotfixes, updates) are not filtered and show up with Uninstall buttons.
**Fix:** De-duplicate on name plus version (and publisher), and skip `SystemComponent` / `ParentKeyName` entries. Use `RegistryKey.OpenBaseKey(hive, RegistryView.Registry64/32)` instead of the hard-coded `WOW6432Node` path, which is wrong for an x86 build of the app.

### IN-04: Unused `_registry` field in all four view models

**File:** `src/VainTools.App/ViewModels/AppxManagerViewModel.cs:19`, `InstalledAppsViewModel.cs:23`, `OptionalFeaturesViewModel.cs:19`, `StoreViewModel.cs:20`
**Issue:** `_registry` is assigned in the constructor but never read afterwards (only `registry.IsElevated` is used).
**Fix:** Drop the field and keep the constructor parameter.

### IN-05: Test organisation and wording

**File:** `src/VainTools.Tests/StoreServiceTests.cs:329-342`
**Issue:** `ProcessRunnerTests` (a real-process test of `cmd.exe`) is defined inside `StoreServiceTests.cs`, which is hard to find. The test runs a real process, unlike the rest of that file.
**Fix:** Move it to `ProcessRunnerTests.cs`.

---

_Reviewed: 2026-10-09_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
