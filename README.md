# Umi Achievement Claimer

A lightweight bilingual Windows utility for detecting Steam games, reading their achievement status and claiming only the achievements selected by the user.

## Features

- English interface by default, with Vietnamese available in the app.
- Clear two-column dashboard with a dedicated game-information sidebar, achievement workspace and activity log.
- Detects the game name, Steam AppID, installation directory and Steam API architecture.
- Reads achievement names, descriptions and locked/unlocked status before making changes.
- Select individual achievements, all achievements or only locked achievements.
- Nothing is selected or claimed automatically.
- Supports x86 and x64 Steamworks games through embedded workers.
- Can detect Steam API libraries stored in common game wrappers, JAR/ZIP archives and supported embedded CAB archives.
- Supports Capcom Arcade Stadium through a compatible x64 Steam API provider already installed in the local Steam library.
- Does not edit or delete game save files.

## Download

Download `UmiAchievementClaimer.exe` from the repository and run it on Windows.

| Item | Value |
|---|---|
| Version | `1.5.0` |
| File | `UmiAchievementClaimer.exe` |
| Size | `223,232 bytes` |
| SHA-256 | `165730ED5D42C0A36E8C60C59E24B33173D803B69D8B5239C596D421F48B77D7` |

Verify the downloaded file in PowerShell:

```powershell
Get-FileHash -Algorithm SHA256 .\UmiAchievementClaimer.exe
```

## Requirements

- Windows 10 or Windows 11, 64-bit.
- Steam running in Online mode with a valid session and game license.
- A standard or supported packaged Steamworks API used by the selected game.

## Usage

1. Close the target game and keep Steam running.
2. Open `UmiAchievementClaimer.exe` and select the game's installation folder.
3. Review the detected game and achievement list.
4. Select only the achievements you want, then choose **Claim selected** and confirm.

## Build from source

The project uses Windows PowerShell and the .NET Framework 4.x compilers included with Windows. No NuGet packages are required.

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\build.ps1
```

The compiled application is written to `dist\UmiAchievementClaimer.exe` and copied to the repository root.

## Project structure

```text
src/       Application, detection, UI and worker source code
assets/    Umi logo and Windows icon
tools/     Brand asset preparation script
docs/      Architecture notes
build.ps1  Build script
```

## Compatibility notes

- Server-authoritative achievements, anti-cheat protected games and encrypted or custom Steam API implementations may reject or prevent achievement changes.
- Third-party launchers, DLC and account-linking requirements still apply.
- Steam Families games use the borrowing player's own Steam achievement profile.
- The app reports unsupported protected game builds instead of attempting unsafe runtime-memory extraction.

## Important

Claiming achievements changes Steam profile data and may not be fully reversible. Use the tool only with games and Steam accounts you are authorized to access.

Việc claim achievement sẽ thay đổi dữ liệu hồ sơ Steam và có thể không hoàn tác hoàn toàn. Chỉ sử dụng ứng dụng với game và tài khoản Steam mà bạn có quyền truy cập.

## License

No license is currently included.
