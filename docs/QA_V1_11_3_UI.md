# Umi Achievement Claimer 1.11.3 - Activity log layout regression

Date: 2026-10-23

## Root causes and changes

The previous 1.11.2 fix hid the native Windows ProgressBar while idle. That did not remove the WinForms control entirely and did not address deferred Activity log sizing during the initial paint.

Version 1.11.3:
- Replaces the native ProgressBar with a simple, theme-colored WinForms Panel used only as a non-interactive busy indicator. This removes the native 1-pixel seam source from the Activity log footer.
- Replaces deferred `BeginInvoke(LayoutLog)` with a synchronous, batched layout when Activity log is expanded or collapsed.
- Recalculates the expanded log size when the parent window resizes.
- Adjusts the achievement empty-state label's vertical padding when the log is expanded and the remaining area is short. This keeps both lines of the first-run message readable.

## Evidence

- **126 PASS / 0 FAIL** in the local native synthetic WinForms test suite.
- Verified first-click expansion, collapse/reopen, minimum window size, and minimize/restore.
- Captured **real Windows desktop pixels** using `Graphics.CopyFromScreen` while displaying the tested WinForms app with no game selected. Verified the first expanded log, reopened log, restored app, and minimum-size log. No horizontal white line appeared and the dark log content remained within its card.
- The minimum-size capture initially revealed clipping of the second line of first-run guidance. This was corrected and verified again.
- UI screenshots in `docs/images` are generated from **synthetic WinForms test fixtures**. Screen captures that contain desktop/background pixels are not committed to the repository.
- No Steam achievement changes or live claims were performed.

## Limits

The real desktop screenshot test was conducted in the available Windows session at its current DPI, not every hardware configuration. A user opening a different EXE or older portable archive may still see older behavior. Always check that the title and top-right label show **v1.11.3**, and launch `dist\UmiAchievementClaimer.exe`.
