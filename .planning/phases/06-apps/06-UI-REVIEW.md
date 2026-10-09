---
phase: 06-apps
baseline: 06-UI-SPEC.md
method: code-only (WinUI 3 desktop — no screenshots)
overall: 15/24
created: 2026-10-09
---

# Phase 06 — UI Review

Code-only audit of the four Apps pages (XAML + ViewModels) against `06-UI-SPEC.md`.
Nothing here was verified at runtime.

| Pillar | Score | Key finding |
|---|---|---|
| Copywriting | 3/4 | CTAs and dialog copy match spec; empty-state heading and body fused into one string |
| Visuals | 2/4 | Optional Features shows Enable and Disable at once; opacity dimming; no AutomationProperties.Name |
| Color | 3/4 | Theme brushes only, no hardcoded hex; destructive buttons are red text only |
| Typography | 2/4 | Section headers 14px SemiBold instead of SubtitleTextBlockStyle (20px); undeclared 11px size |
| Spacing | 3/4 | Mostly on the 4/8/16/24 scale; flat rows instead of a card per item |
| Experience Design | 2/4 | No on-page error state; empty state shows before load completes |

## Top fixes

1. **Experience Design — no persistent error state; empty state shows during loading.**
   Failures go only through `_infoBar.ShowError` / `StatusMessage`; no page binds an InfoBar to
   `ErrorMessage`. Spec load-failure copy is unused (raw `ex.Message` instead, e.g.
   `AppxManagerViewModel.cs:95`). Empty-state TextBlocks bind only `HasPrograms`/`HasApps`
   (e.g. `InstalledAppsPage.xaml:67`), so they appear on first paint and after a failed load.
   Fix: InfoBar bound to `ErrorMessage`, spec copy for load failures, hide empty state while
   `IsLoading` or an error is set.
2. **Visuals — Optional Features always shows both Enable and Disable** (`OptionalFeaturesPage.xaml`,
   grid columns 1 and 2). Spec calls for one state-dependent button. Fix: toggle visibility on state,
   or one button with a state-derived label.
3. **Typography — type scale deviates.** Section headers ("Programs", "Installable Apps", …) use the
   14px default; spec says SubtitleTextBlockStyle. `FontSize="11"` on every page is outside the
   12/14/20/28 scale. Secondary text uses `Opacity="0.7"` (e.g. `InstalledAppsPage.xaml:84`,
   `StorePage.xaml:96`) instead of `TextFillColorSecondaryBrush`/`TextFillColorTertiaryBrush`.

## Other findings

**Copywriting**
- Empty states fuse heading + body (e.g. `InstalledAppsPage.xaml:64`).
- Confirm button text ("Remove", "Uninstall", "Install") differs from spec "Confirm" — arguably better.
- Per-operation elevation errors in ViewModels diverge from the spec string; the static page InfoBar matches.

**Visuals**
- No `AutomationProperties.Name`/ToolTip on repeated row buttons — screen readers hear identical labels.
- Flat Grid rows inside one card; no per-item card or separators.
- Store shows Version in the publisher slot (winget reports source, not publisher) — reasonable adaptation.
- Store has an explicit Search button + Enter handling (not in spec, sensible).

**Color**
- Destructive buttons set only `Foreground` to `SystemFillColorCriticalBrush`; contrast unchecked in both themes.
- Accent brush unused; focus/selection rely on defaults.

**Spacing**
- Page padding 24, row spacing 16, card padding 16 / radius 8 match spec.
- Installed Apps' two row buttons are 16px apart (spec 8).

**Experience Design**
- Present: ProgressRing, elevation InfoBar, confirmations, elevation guards, reload after success.
- Store does not search on navigation (deliberate deviation from spec's debounced search).
- `TextTrimming` only on names; Appx `FullName` wraps.
- Row buttons may stay enabled while a page-level operation runs (not verified) — possible double-start.
- Copy Command feedback is via `StatusMessage` + InfoBar success.

## Scope

Audited: `Features/Apps/{AppxManagerPage,InstalledAppsPage,OptionalFeaturesPage,StorePage}.xaml`,
`StorePage.xaml.cs`, and the four ViewModels (partial read). Other code-behind files not read in depth.
