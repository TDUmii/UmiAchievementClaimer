# Umi Achievement Claimer

Umi Achievement Claimer is a native Windows WinForms utility for reviewing Steam achievement status and claiming the achievements a user explicitly selects. It is intended for people working with games and Steam accounts they are authorized to access. No narrower audience or demographic is established.

The main workflow is to choose a game installation folder, run a read-only scan, review the detected game and achievement list, select individual achievements, then confirm the game, AppID and selected count before submitting changes. The application opens in English; language and appearance choices live in a dedicated Settings panel.

Product commitments:

- Start with zero selected achievements. A scan does not claim anything.
- Preserve checked items across searches and All / Locked / Unlocked filters. Report selections hidden by the current filter.
- Apply Select visible and Select locked only to visible results. Clear all clears hidden selections too.
- Keep the selection count and Claim action visible below the list. Claim requires an explicit confirmation; Cancel is the dialog's focused, Enter and Escape action.
- Disable game-context and selection controls while a worker is active.
- Keep the native Windows frame, system buttons and resize behavior. The chosen visual direction is Steam Midnight, with a black caption and border requested through DWM and a compatible dark-frame fallback.
- Keep a game-client visual hierarchy: Bahnschrift display type for titles, stats and action labels, monospace treatment for the installation path, and the explanatory subtitle limited to the Achievements workspace.
- Let the user choose Vietnamese/English and Steam Midnight, Carbon, Arcade or a custom accent from the dedicated Settings panel. Apply the selected choices live and persist the theme for the next launch.
- Keep the main achievement workflow compact: a full-width scrollable explorer-style list, combined search/filter toolbar and one Quick select menu for visible, locked-visible and clear-all operations.
- Keep list navigation visually native to the Steam Midnight world: borderless in-theme path input and a dark themed scrollbar with keyboard, wheel and drag support.
- Show real game cover artwork and state-specific achievement icons when available. Public Steam metadata and CDN downloads are optional; failed artwork leaves a fallback and does not block the core workflow. Artwork requests do not upload installation paths or Steam account details.
- Keep diagnostics available through collapsible technical details and an activity log with Copy log.

The UI orchestrates a separate x86 or x64 Steam worker rather than calling native Steam functions in the WinForms process. The build uses the Windows .NET Framework compiler and requires no NuGet packages. Windows 10/11 x64 and a valid, online Steam session with the required game license are the supported environment; game-specific compatibility still varies.

Claiming changes Steam profile data and may not be fully reversible. The confirmation communicates that consequence and asks the user to close the game. The utility does not edit or delete game saves.

Native UI checks use synthetic games and achievement states; the artwork check exercises public Steam downloads. DWM checks establish setter acceptance and dark-mode readback, while client render captures do not establish composed titlebar pixel colors. Simulated scaling does not establish behavior on a physical high-DPI display. These checks do not prove a real Steam scan or claim for a target game.

See [DESIGN.md](DESIGN.md) for the shipped visual system, [README.md](README.md) for build and usage instructions, and [architecture](docs/ARCHITECTURE.md) for the worker boundary.


## Installed-game discovery and first-run UX (2026-10-23)

- The initial call to action is **My games / Thư viện**, opening a keyboard-accessible, searchable picker of locally installed Steam games. The picker reads Steam's appmanifests, does not run a Steam worker, and never changes achievements.
- Users can search by name or AppID. The **Browse** fallback remains available for games outside discoverable Steam libraries.
- Library enumeration and game detection avoid blocking the window. Controls cannot initiate competing scans while detection is in progress.
- **Rescan** is disabled when no valid folder has been selected. Search, filters, selection and Claim remain explicit.
- **Ctrl+L**, **Ctrl+F** and **F5** speed up common workflows. The empty state shows the next step and activity-log expansion preserves an achievement row even at the minimum window size.
- Client-only synthetic UI tests do not establish real Steam claim support or accessibility across every physical display.
