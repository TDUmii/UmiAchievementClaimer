# Architecture

Umi Achievement Claimer keeps the desktop UI, detection logic, data models and native Steam worker separated so changes remain reviewable.

## Source responsibilities

- `src/App`: process entry point and application startup configuration.
- `src/Core`: dependency-free data models shared by detection and UI.
- `src/Detection`: Steam manifest discovery, Steam API discovery, PE architecture detection, ZIP/JAR/CAB inspection and compatibility messages.
- `src/UI`: WinForms composition, visual theme, localization, user selection and worker orchestration.
- `src/Worker`: isolated x86/x64 native Steamworks bridge. It is compiled twice and embedded in the main EXE.
- `src/Properties`: assembly identity and version metadata.

## Dependency direction

```text
App -> UI -> Detection -> Core
          -> Core

Worker (separate executable; communicates with UI through UTF-8 stdout)
```

The UI never calls Steam native functions itself. This prevents a 32-bit game DLL from being loaded into the 64-bit WinForms process and keeps native crashes isolated to the temporary worker process.

For protected games that do not expose their own Steam API on disk, detection may select a tested compatible x64 Steam API provider already present in another local Steam installation. The selected AppID and game root remain those of the target game; the provider file is read in place and neither game installation is modified.

## Safe list/claim boundary

The worker has explicit modes:

- `list`: reads schema, names, descriptions and current status, then exits before `SetAchievement` and `StoreStats`.
- `selected`: reads a temporary Base64-encoded selection file and submits only matching achievement API IDs.
- `probe`: validates interface/count only.

The application starts with zero checked achievements. Selection changes are local UI state until the user presses the claim button and confirms the exact selected count.

After `StoreStats`, selected mode reloads current stats and verifies every requested achievement before reporting success.

## Steam Midnight UI

`AchievementGrid` retains the canonical achievement collection separately from its visible rows. Checked model objects remain selected when search or state filters hide them. The Claim footer reports all selected objects and the hidden subset; the selection file serializes only the explicitly confirmed models' API IDs.

Bulk selection changes only visible rows. Clear all clears the full selection. Filtering is case-insensitive across name, description and API ID, and never changes checked state.

`ClaimConfirmationDialog` is a presentation-only modal. It shows game/AppID/count and the profile-change warning, returning OK only from its explicit Claim button. Enter and Escape default to Cancel. The existing worker launch remains in `ClaimerForm` after confirmation.

The UI disables all game-context and selection controls while a worker is active. Detection rejects attempts to switch game during a worker operation, and close protection remains active until the UI-thread exit callback has finished.

`SteamArtwork` is separate from the Steam worker. It reads public store/community metadata and downloads cover/icon images from official Steam CDN hosts. The form requests icons only for displayed rows, caches them for the current game and discards stale results after a game change. Artwork failures keep the built-in placeholder and never prevent the normal achievement workflow.

`WindowTheme` uses the Windows DWM API to request an immersive dark native frame and explicit black border/caption colors. Windows 11 supports the explicit colors; older builds can fall back without changing the standard system buttons or resize behavior.

`tests/run-ui.ps1` compiles a separate STA regression harness against the built application. It uses reflection to seed synthetic data and test native UI state without invoking detection, launching workers or claiming real achievements. Its `DrawToBitmap` captures are render checks, not proof of real Steam integration or a hardware DPI session.
