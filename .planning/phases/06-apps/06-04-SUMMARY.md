# 06-04 SUMMARY: DISM 3010 as success, argv hardening, pending guards

## What was built
- `FeatureChangeResult(bool RestartRequired)` return type on Enable/Disable.
- `OptionalFeaturesService`: System32 `DismPath`, `ExitRestartRequired = 3010`,
  argv-vector DISM calls with `/NoRestart`, 0 = success, 3010 = success + restart,
  other codes throw with stderr (stdout fallback), `IsValidFeatureName` gate,
  unparseable-output guard.
- `OptionalFeature.IsPending` for DISM pending states.
- `OptionalFeaturesViewModel`: restart warnings, `!IsLoading` + `!IsPending` guards,
  `OnIsLoadingChanged` re-query, unused registry field removed.
- `OptionalFeaturesPage.xaml`: one state-dependent button per row.

## Live check
- Ran `dism.exe /Online /Get-Features /Format:List` read-only: all listed names
  match `^[A-Za-z0-9._-]+$`. Pattern confirmed against the live store.

## Verification
- `dotnet build src/VainTools.App`: 0 warnings, 0 errors.
- `dotnet test src/VainTools.Tests`: 435 passed, 0 failed
  (OptionalFeatures subset: 49 passed).
- All plan acceptance greps meet thresholds.

## Human check still open (Task 3)
- As admin, enable a reboot-requiring feature (e.g. Windows Sandbox):
  expect "Restart required" warning, list reload, row "Enable Pending" with
  action unavailable. Requires a real reboot to complete; not performed here.
