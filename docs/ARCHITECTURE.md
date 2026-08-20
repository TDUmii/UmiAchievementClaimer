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
