# Changelog

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
