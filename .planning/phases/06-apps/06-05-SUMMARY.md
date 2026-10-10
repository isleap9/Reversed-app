# 06-05 SUMMARY: Provisioned rows deprovision for all users

## What was built
- `AppxPackage.PackageFamilyName` (defaulted `""`) mapped from `Package.Id.FamilyName`
  for installed and provisioned rows.
- `IAppxPackageService.DeprovisionPackageAsync` + WinRT implementation via
  `PackageManager.DeprovisionPackageForAllUsersAsync`; shared result mapping.
- `AppxPackageService.SafeInstalledPath`: one unreadable install path no longer
  aborts enumeration.
- `AppxManagerViewModel`: routes Provisioned rows to deprovision / Installed rows
  to per-user remove; blank family refused before any call; scope-stating
  confirmation (`BuildRemoveConfirmation`) and deprovision success copy;
  `CanRemovePackage` gated on `!IsLoading` with `OnIsLoadingChanged`; unused
  registry field removed.

## Verification
- `dotnet build src/VainTools.App`: 0 warnings, 0 errors.
- `dotnet test src/VainTools.Tests`: 449 passed, 0 failed
  (Appx subset: 35 passed, incl. real-WinRT enumeration + bogus-family deprovision).
- All plan acceptance greps meet thresholds.

## Human check still open (Task 2)
- As admin, remove a provisioned-only package (e.g. BingWeather if provisioned
  but not installed): expect "Remove Provisioned Package" dialog stating the
  all-users scope and family name, then "Package deprovisioned" and the row gone.
  Irreversible without the package source; not performed here.
