# UI review - Umi Achievement Claimer v1.11.2

Review date: 2026-10-23. Scope: Windows WinForms application, source at `D:\Code\Tools\UmiAchievementClaimer`.

## Root cause of the white line

The bottom white line in the user-provided Activity log screenshot was **not the DWM frame**. A native `ProgressBar` was still visible at idle, despite the parent `TableLayoutPanel` having a nominal zero-height row. WinForms laid out this visible control at **one pixel tall**, aligned exactly with the log's bottom edge.

Reproduction before fixing the control:
- `progressVisible=True`
- `progressBounds={X=18,Y=736,Width=1184,Height=1}`
- `logBottom=736` with a 1220 × 840 client.

Correction: initialize `progress.Visible=false`; `SetBusy(true)` shows it only when a worker is actually running and `SetBusy(false)` hides it immediately. The resulting idle control is invisible; the white one-pixel strip no longer renders.

## Other changes

- Current assembly version is shown as **v1.11.2** under Settings at the upper-right; the native window title includes the same dynamically derived version.
- Version is obtained from assembly metadata, not maintained as a separate manually edited UI string.
- Use **one canonical EXE in `dist/`**. No duplicated tracked EXE in the repository root.
- After successful portable packaging, obsolete `dist/UmiAchievementClaimer-v*-windows-x64-portable.zip` archives are pruned automatically. Only build outputs are deleted; user preferences, game caches and snapshots in AppData are unaffected.
- Simplified README (English + Vietnamese) and refreshed UI screenshots. Obsolete preview images are recoverable in Git history.

## Verification and current limits

- Local synthetic Windows regression: **122 PASS / 0 FAIL**, including assertions for the idle progress bar, showing it while busy, hiding again when idle, and the visible upper-right version label.
- Native screenshot review: empty state, first expansion of Activity log, Insights and Settings. The rebuilt version label is fully visible under Settings, with no header overlap.
- Portable ZIP is verified by extraction and SHA-256 hash comparison.
- No live Steam Claim was performed; no assertion of universal per-game compatibility.
- WinForms `DrawToBitmap` does not capture native non-client titlebar pixels. This is separate from the proven one-pixel ProgressBar defect.
- Real screen reader workflows and all physical DPI settings have not been certified.

## UI self-evaluation (subjective)

**8.5/10** for the current desktop UI, not a claim of perfection.

- Visual hierarchy and interaction: **8.8/10** - game selection, achievements, Claim, and diagnostics are clearly grouped.
- Theme consistency and contrast: **8.7/10** - coherent navy surface and accents; Activity log now legible on first open.
- Layout and responsiveness: **8.4/10** - stable at normal/minimum synthetic sizes; dense metadata at the smallest supported size.
- Discoverability and localization: **8.5/10** - readable first-run state, preflight, status feedback and bilingual settings.
- Accessibility and physical-DPI confidence: **7.8/10** - useful keyboard flow, but no end-to-end assistive-tech and real monitor DPI certification.

Remaining polish opportunities: larger explanatory metadata at constrained sizes, physical 125%-200% DPI testing, keyboard-only dialogs and screen-reader validation. These are not release blockers for the current focused white-line and version cleanup.
