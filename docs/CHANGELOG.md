## 1.11.0 — 2026-10-23 (Insights, local build)

- Introduced public global achievement rarity percentages with honest unavailable-state fallback.
- Optional Steam API unlock Unix time passed through compatible extended worker protocol.
- Local snapshot history with duplicate detection and 30-snapshot retention per AppID, timeline and progress comparison.
- New Steam Midnight Insights Dashboard: read-only recovery status, snapshot counts and safe CSV export.
- Pending selected-claim snapshot enables review after interrupted or uncertain outcomes; no automatic retry.
- Read-only version check against public official GitHub Releases, no automatic binary installation.
- Updated Windows synthetic regression suite: 112 PASS / 0 FAIL; no real claims performed.
- Snapshot storage is per Windows profile + AppID, not automatically isolated by Steam account ID.

## 1.10.0 - 2026-10-23 (local build, not yet released)

### Steam reliability and safety

- New read-only compatibility preflight for Steam process, game root, numeric AppID, API source, target architecture and compatibility-provider warnings.
- A visible cancel action and bounded worker runtime (90 seconds for reading; 120 seconds for a claim). Interrupted changes are reported as uncertain; no automatic claim retry.
- Parse worker `VERIFIED_COUNT` and per-achievement acceptance lines, show clear local readback counts and distinguish local verification from Steam server synchronization.

### User experience

- Favorites-only and recently used ordering in the installed-game picker, saving only AppIDs to local AppData.
- Achievement sort modes: original, A-Z, locked first, unlocked first.
- Read-only achievement inspector with status, ID, name, artwork and description.
- Main toolbar/inspector updated after visual QA while retaining Steam Midnight design and bilingual strings.

### Build and quality

- Portable ZIP and SHA-256 manifest via `tools/package-release.ps1`.
- GitHub Actions Windows build, synthetic UI regression and upload-artifact; GitHub Release publication on an explicit version tag.
- Expanded native synthetic UI regressions: **98 PASS, 0 FAIL** on the development PC.
- No live Steam achievement claim or account modification performed by QA.

## Unreleased - Steam Library & UX completion (2026-10-23)

- Added a searchable installed-game picker (**My games / Thư viện**) using local Steam appmanifests and libraryfolders, with explicit manual-folder fallback.
- Steam library discovery runs asynchronously; the game's Steam API detection also runs off the UI thread.
- Added clear first-run empty state, accessible search controls, a dark themed library scrollbar and bilingual labels.
- Added Ctrl+L (library), Ctrl+F (achievement search), F5 (rescan) keyboard shortcuts.
- Made the expanded Activity log retain at least one achievement row at minimum supported window dimensions.
- Updated build handling: an EXE locked by Windows no longer aborts compilation; the newest artifact remains under dist and UI tests always target it.
- Added synthetic manifest traversal/Unicode/search, picker overflow, themed scrollbar and minimum-size regression coverage.
- Validation: **83 PASS, 0 FAIL** native synthetic UI tests, independent discovery of 16 locally installed games. Steam claim compatibility and physical DPI not re-certified; no live achievements were claimed.

## Unreleased - Steam Midnight v2 UI

- Replaced the fixed game sidebar with a full-width horizontal game summary, including game metadata, unlock progress and folder controls.
- Combined search, status filters and Quick select into a single achievement toolbar, with a search placeholder.
- Preserved explicit checks, hidden selections and confirmation before claiming. Browse starts a scan; Rescan repeats it.
- Added UI regression checks for the horizontal layout, readable metadata, search placeholder and completion stripe (67 PASS, 0 FAIL on the development machine).

# Changelog

## 1.9.0 - 2026-10-22

### Native UI finish pass

- Removed the contrasting frame strip around the folder path and kept the field as a flat, vertically centered in-theme surface.
- Replaced the white Windows achievement scrollbar with a compact themed scrollbar that follows the active surface, border and accent palette and drives the list by wheel, keyboard and drag.
- Tightened the Settings dialog into a conventional compact form with aligned language, theme, custom accent and close actions.
- Applied a matching dark renderer to the `SELECT` context menu.

### Validation

- Built the x86 worker, x64 worker and x64 Windows EXE with the Windows .NET Framework compilers.
- Native UI regression: `60 PASS, 0 FAIL`, including borderless path rendering, themed scrollbar movement, compact Settings and the themed selection menu.
- Live artwork smoke check: `10 PASS, 0 FAIL`.
- Impeccable detector: clean JSON result (`[]`).
- No Steam achievements were claimed as part of this UI release validation.

## 1.8.0 - 2026-10-22

### Focused settings and achievement list

- Moved language and appearance controls out of the main workflow into a dedicated Settings dialog with live VI/EN switching, Steam Midnight/Carbon/Arcade presets and a custom accent picker.
- Reduced the header to one `SETTINGS` action and removed the extra theme/language controls from the achievement workspace.
- Replaced the three visible bulk-selection buttons with one compact `SELECT` menu for visible, locked-visible and clear-all actions.
- Converted the folder path input to a vertically centered single-line field and tightened the card, button, footer and achievement-row geometry.
- Kept the achievement collection as a native vertically scrollable explorer-style list with compact two-line rows and state-specific artwork.

### Validation

- Built the x86 worker, x64 worker and x64 Windows EXE with the Windows .NET Framework compilers.
- Native UI regression: `57 PASS, 0 FAIL`, including Settings, language toggling, theme switching, centered path layout, compact scrolling rows, minimum-size layout and artwork fixtures.
- Live artwork smoke check: `10 PASS, 0 FAIL`.
- No Steam achievements were claimed as part of this UI release validation.

## 1.7.0 - 2026-10-22

### Game client polish

- Reworked the visual hierarchy with Bahnschrift display typography and a compact HUD-like treatment for the game title, stats, status labels and action controls.
- Removed the header subtitle and the separate folder subtitle. The folder path, Browse and Scan controls now share one aligned row with a monospace path field and a clear empty-state cue.
- Kept the explanatory subtitle inside the Achievements workspace, where it supports the list without adding noise to the rest of the client.
- Replaced the language ComboBox with a compact `EN` / `VI` toggle button that keeps the current selection and filter state.
- Added a `THEME` picker with Steam Midnight, Carbon and Arcade presets plus a custom accent ColorDialog. Theme preferences persist under `%APPDATA%\\UmiAchievementClaimer\\theme.cfg` and apply live without restarting the app.
- Added contrast-aware primary button and checkbox marks so custom accent colors remain readable.

### Validation

- Built the x86 worker, x64 worker and x64 Windows EXE with the Windows .NET Framework compilers.
- Native UI regression: `55 PASS, 0 FAIL`, including theme switching, language toggling, path alignment, the theme picker layout, minimum-size layout and artwork fixtures.
- Live artwork smoke check: `10 PASS, 0 FAIL`.
- No Steam achievements were claimed as part of this UI release validation.

## 1.6.0 - 2026-10-22

### Steam Midnight

- Replaced the light navy/orange dashboard with the requested navy, lavender and cyan interface while retaining the Umi logo and native WinForms application.
- Added black native titlebar and window border on Windows 11, preserving the Windows resize and system buttons. Windows 10 uses the compatible dark-frame fallback.
- Added game cover artwork and achievement icons from public Steam metadata. Visible icons load lazily in the background, with built-in fallbacks when metadata or images are unavailable.
- Made achievements the main workspace, with two-line rows, status, search by name/description/API ID and All / Locked / Unlocked filters.
- Added total/unlocked/locked statistics and a fixed Claim footer that includes the number of hidden selected achievements.
- Scoped bulk selection to visible results. Clear all clears hidden selections too. Searches and filters do not discard selections or select new achievements.
- Added a collapsible activity log, Copy log and expandable technical details.
- Added a themed confirmation with game name, Steam AppID and selected count. Cancel is the default action.
- Preserved English and Vietnamese throughout the new controls and states.

### State handling

- Disabled game switching, language, search, filters and selection while a worker is active.
- Kept the window open until the worker's exit callback has finished and restored controls on startup errors.
- Preserved the isolated x86/x64 workers, read-only list mode and explicit selected-ID claim mode.

### Validation

- Built the x86 worker, x64 worker and x64 Windows EXE with the Windows .NET Framework compilers.
- Added native UI regression tests and rendered synthetic states at standard/minimum sizes, in both languages and with scaled controls.
- No Steam achievements were claimed as part of this UI release validation. Game-specific Steam integration is not re-certified by the UI tests.

- UI hotfix: readonly folder display, search placeholder alignment, mouse-vs-keyboard focus, segmented filter theme, larger activity log with themed scrollbar, and local Steam artwork cache fallback.
