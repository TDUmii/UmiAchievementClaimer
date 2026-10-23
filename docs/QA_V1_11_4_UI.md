# Umi Achievement Claimer v1.11.4 — Activity log visual polish

Date: 2026-10-23

## Problem shown in the user's screenshot

After the prior layout and white-line fixes, the **Activity log toggle retained a conspicuous hover-colored pill**, while the log body used a darker rectangular background than the rounded header/card. The seam between those backgrounds looked like an extra overlaid panel. This was distinct from the earlier native ProgressBar artifact.

## Changes

- Use one consistent `UiTheme.Surface` background for the Activity log card, its header, content viewport, and read-only TextBox.
- Keep **Activity log** and **Copy log** as accessible, clickable buttons, but make their hover/pressed backgrounds match the card. The hover no longer looks like a selected tab.
- Reduce the toggle's width from 210px to 168px, making the section control less visually dominant.
- Reapply those visual properties on theme changes (Steam Midnight, Carbon, Arcade, and custom themes); preserve prior first-click/reopen/resizing behavior.
- No Claim, Steam API, storage, or history behavior changed.

## Verification

- **130 PASS / 0 FAIL** in the native synthetic Windows UI suite, including surface continuity, quiet hover backgrounds, narrow toggle sizing, theme switching, first-click layout, collapse/reopen and worker controls.
- Inspected a live Windows screenshot of the test application `v1.11.4` with the pointer moved over the Activity log control. It showed a single uninterrupted log surface, with no large hover pill and no white footer line.
- The live screenshot diagnostic tool and photos containing desktop pixels were temporary and are **not committed to the repository**.
- The README screenshot is a synthetic native WinForms fixture, not Steam account data.
- A real Steam Claim was not attempted.

## Scope and limitation

The prior screenshot was a visual styling defect; replacing the ProgressBar alone could not have fixed it. This version fixes the separate header/body palette mismatch and interactive hover fill. The available Windows desktop was checked, but other graphics drivers and scaling modes remain unverified.
