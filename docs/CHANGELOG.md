# Changelog

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
