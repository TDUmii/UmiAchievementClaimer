# Umi Achievement Claimer 1.11.1 — UI polish verification

Date: 2026-10-23

## Reported defects addressed
- First Activity log expansion rendered nearly black text on a dark panel. Root cause: WinForms TextBox foreground initialized only during theme refresh. The constructor now assigns the foreground explicitly, and the expansion path reasserts contrast.
- Achievement search placeholder was clipped and layered on the editable text area. Replaced the on-top Label overlay with the native Windows edit cue banner; retained accessible name and Ctrl+F workflow.
- Start / no-result guidance was obscured by the native DataGridView. The grid is hidden while a dedicated centered empty-state label is visible, then restored when results exist.
- A confirmed successful Steam enumeration returning zero achievements is now an empty-game state rather than a scan error. No Claim is enabled. **Caveat:** game-specific live Steam API behavior has not been independently validated.
- Settings redesigned as readable Language and Appearance sections with larger grouped controls, selected preset visual feedback and saved-state note.
- Insights Dashboard redesigned with three prominent KPI cards, tidy local snapshot history rows, themed scrolling, clearer recovery space and consistent controls.
- On Windows 11, DWM's documented `DWMWA_COLOR_NONE` request suppresses the bright native window border while retaining Windows titlebar and sizing. Unsupported Windows versions use previous black-border fallback.

## Verification
- Native synthetic WinForms regression suite: **115 PASS / 0 FAIL** after changes.
- Added first-click-before-theme-change log regression and screenshot.
- Added first-run / search empty state assertions, zero-achievement status test and screenshots.
- Main, expanded log, Settings and Insights screenshots reviewed after implementation; no Steam Claim performed.
- `DrawToBitmap` does not include the real Windows-composited titlebar or resize frame; final DWM border appearance still requires inspection on the user's actual desktop.
- All screenshots captured from synthetic data, not a real Steam account.
- Latest tested executable: `dist\UmiAchievementClaimer.exe`.
- Windows CI and GitHub release status must be checked separately.
