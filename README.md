# Umi Achievement Claimer

A bilingual Windows desktop utility for detecting locally installed Steam games, reviewing their achievement state, and submitting only the achievements explicitly selected by the user.

Tiện ích Windows song ngữ dùng để nhận diện game Steam đã cài, xem trạng thái achievement và chỉ gửi những achievement người dùng chủ động lựa chọn.

## Current release / Bản phát hành hiện tại

| Item | Value |
|---|---|
| Release | `v1.4.0` |
| Assembly version | `1.4.0.0` |
| Release date | `2026-08-20` |
| Prebuilt artifact | `UmiAchievementClaimer.exe` |
| Checksum manifest | `SHA256SUMS.txt` |
| File size | `220,160 bytes` (`215 KiB`) |
| SHA-256 | `E784AA251DB529C4CE06722E991A6C2821A1820E1CBF63700441CB19AA8AE3A8` |
| Main process | Windows x64, .NET Framework 4.x WinForms |
| Embedded workers | Steamworks x86 and x64 |
| Default language | English |

The SHA-256 above identifies the prebuilt EXE committed with this release. A local rebuild can have a different hash because the legacy .NET Framework compiler writes build-time metadata into the PE file.

SHA-256 phía trên dùng để xác minh đúng file EXE được đóng gói trong bản phát hành này. File tự build lại có thể có hash khác vì trình biên dịch .NET Framework cũ ghi metadata thời điểm build vào PE.

### Verify the download / Xác minh file tải về

Run from the repository root in PowerShell:

```powershell
$expectedSha256 = 'E784AA251DB529C4CE06722E991A6C2821A1820E1CBF63700441CB19AA8AE3A8'
$actualSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath '.\UmiAchievementClaimer.exe').Hash

Write-Host "Expected: $expectedSha256"
Write-Host "Actual:   $actualSha256"
Write-Host "Match:    $($actualSha256 -eq $expectedSha256)"
```

Expected result / Kết quả đúng:

```text
Match: True
```

## v1.4.0 highlights

- English is now the default UI language; Vietnamese remains available from the header selector.
- Redesigned responsive card-based interface using the Umi navy/orange visual system.
- Achievement scan, detected-game details, selection and activity log are separated into clear sections.
- No achievement is selected by default; users may select individual items, all items, or only locked items.
- Source reorganized into `App`, `Core`, `Detection`, `Properties`, `UI` and `Worker` boundaries.
- Added embedded-CAB Steam API detection for games such as Cybarian.
- Preserved read-only `list` mode: scanning exits before `SetAchievement` and `StoreStats`.

- English hiện là ngôn ngữ mặc định; vẫn chuyển được sang Tiếng Việt trên header.
- UI dạng card mới, dùng hệ màu navy/cam của Umi và co giãn hợp lý.
- Luồng quét, thông tin nhận diện, lựa chọn achievement và nhật ký được chia khu vực rõ ràng.
- Mặc định không tick mục nào; có thể chọn riêng, chọn tất cả hoặc chỉ chọn mục chưa mở.
- Source được chia thành các lớp `App`, `Core`, `Detection`, `Properties`, `UI` và `Worker`.
- Bổ sung nhận diện Steam API trong CAB nhúng như trường hợp Cybarian.
- Giữ chế độ `list` chỉ đọc: quét dừng trước `SetAchievement` và `StoreStats`.

## Branding

The app uses the navy U mark with the orange rising curve as its primary identity. The transparent mark appears beside the product name in the main window, while the same simplified mark is embedded as the Windows EXE/taskbar icon.

- `assets/umi-logo-master.png`: high-resolution transparent source selected from the approved visual direction.
- `assets/umi-logo.png`: normalized 512 px UI resource.
- `assets/umi-app.ico`: compatible 64 px Windows icon; Explorer and the taskbar generate their display sizes from it.
- `tools/prepare-brand-assets.ps1`: reproducibly regenerates the PNG and ICO files from the master.

Run `tools\prepare-brand-assets.ps1` before `build.ps1` whenever the master mark changes.

## Features / Tính năng

- English is the default interface language; Vietnamese remains switchable at runtime.
- Giao diện mặc định là English; vẫn có thể chuyển sang Tiếng Việt ngay khi app đang chạy.
- Responsive card-based WinForms layout with a clear scan → detection → selection → log workflow.
- UI dạng card, phân chia rõ luồng quét → nhận diện → lựa chọn → nhật ký.
- Select a Steam game installation folder; no manual AppID entry is normally required.
- Chỉ cần chọn thư mục cài game; thông thường không phải nhập AppID thủ công.
- Detects the matching `appmanifest_*.acf`, game name, AppID and installation root.
- Tự tìm manifest, tên game, AppID và thư mục cài chính xác.
- Recursively finds `steam_api.dll` or `steam_api64.dll`.
- Automatically detects x86/x64 and `STEAMUSERSTATS_INTERFACE_VERSION`.
- Supports stripped `steam_api` DLLs by scanning companion Steam wrappers and Unity IL2CPP metadata, then probing compatible interface versions at runtime when needed.
- Hỗ trợ game đã lược bỏ chuỗi interface khỏi `steam_api`: app quét wrapper đi kèm và metadata Unity IL2CPP, sau đó tự thử phiên bản tương thích khi cần.
- Detects and temporarily extracts `steam_api.dll`/`steam_api64.dll` embedded inside Java JAR/ZIP archives or CAB archives appended to a game executable; the game installation is not modified.
- Nhận diện và giải nén tạm Steam API nằm bên trong JAR/ZIP hoặc CAB được nhúng vào EXE game; không sửa file cài đặt game.
- Embeds both x86 and x64 workers in the final application.
- Hai worker x86/x64 được nhúng trong EXE cuối; người dùng chỉ cần một file chạy.
- Shows live worker output, achievement API names and final `StoreStats` result.
- Reads achievement title, description, API ID and current unlocked/locked status before any claim is possible.
- Quét danh sách achievement theo chế độ chỉ đọc: tên, mô tả, API ID và trạng thái đã mở/chưa mở.
- Every achievement starts unchecked. Use **Select all**, **Select locked**, or manually tick only the items you want to claim.
- Mặc định không chọn achievement nào; có thể **Chọn tất cả**, **Chọn chưa mở** hoặc tự tick từng mục cần claim.
- The list-loading mode exits before `SetAchievement` and `StoreStats`; opening or scanning a game cannot unlock anything.
- Chế độ tải danh sách dừng trước `SetAchievement` và `StoreStats`; mở hoặc quét game không thể tự unlock achievement.
- Does not edit or delete game save files.
- Không sửa hoặc xóa save game.

## Compatibility notes / Ghi chú tương thích

The detector supports games where `steam_api.dll` or `steam_api64.dll` does not contain a visible `STEAMUSERSTATS_INTERFACE_VERSION` string. It checks nearby wrappers such as `CSteamworks.dll`, Unity IL2CPP `global-metadata.dat`, and can fall back to runtime interface probing.

Trình nhận diện hỗ trợ game không để lộ chuỗi `STEAMUSERSTATS_INTERFACE_VERSION` trong `steam_api.dll` hoặc `steam_api64.dll`. App sẽ kiểm tra wrapper lân cận như `CSteamworks.dll`, metadata Unity IL2CPP `global-metadata.dat`, và có chế độ tự thử interface lúc chạy.

Verified detection case: `Just Move Fall Dungeon Endless Abyss` (AppID `3856040`, x64, `STEAMUSERSTATS_INTERFACE_VERSION011`). This verification only detects compatibility and does not submit achievements.

Verified archive case: `Slay the Spire` (AppID `646570`, Family Sharing install, `desktop-1.0.jar :: steam_api64.dll`, x64, `STEAMUSERSTATS_INTERFACE_VERSION012`). Steam Families keeps achievements on the borrowing player's own Steam account.

Dual-launcher note: EA titles such as `A Way Out` may expose Steam API only through an `__overlay` directory and also require a linked EA Account/EA App session. The worker has a probe-only diagnostic mode that discovers the interface and achievement count without setting or storing achievements.

Verified overlay case: `A Way Out` (AppID `1222700`, `__overlay\steam_api.dll`, x86). Its wrapper returns an unsafe pointer for interface v013; AUTO therefore prioritizes the stable v012-to-v001 compatibility range. Probe-only validation resolved v012 and discovered all 14 Steam achievements without setting them.

Verified embedded-CAB case: `Cybarian: The Time Travelling Warrior` (AppID `928840`). Its x86 `steam_api.dll` is stored inside a CAB appended to `Cybarian.exe`; the app copies only that CAB range to a temporary directory, extracts only `steam_api.dll`, then uses the normal read/select/claim flow. Read-only validation resolved User Stats v012 and discovered all 10 achievements.

Known unsupported protected case: `Capcom Arcade Stadium` (AppID `1515950`) has 20 Steam achievements, but its Enigma Protector build does not expose a loose or extractable Steam API DLL on disk. The app reports this specifically instead of attempting unsafe runtime-memory extraction.

## Repository layout / Cấu trúc repository

```text
UmiAchievementClaimer/
├─ SHA256SUMS.txt
├─ src/
│  ├─ App/
│  │  └─ Program.cs
│  ├─ Core/
│  │  ├─ AchievementItem.cs
│  │  └─ DetectedGame.cs
│  ├─ Detection/
│  │  └─ SteamGameDetector.cs
│  ├─ Properties/
│  │  └─ AssemblyInfo.cs
│  ├─ UI/
│  │  ├─ CardPanel.cs
│  │  ├─ ClaimerForm.cs
│  │  └─ UiTheme.cs
│  └─ Worker/
│     └─ UniversalAchievementWorker.cs
├─ assets/
│  ├─ umi-logo-master.png
│  ├─ umi-logo.png
│  └─ umi-app.ico
├─ tools/
│  └─ prepare-brand-assets.ps1
├─ docs/
│  └─ ARCHITECTURE.md
├─ build.ps1
├─ README.md
└─ .gitignore
```

After building, generated files appear in ignored directories:

```text
build/   # temporary x86/x64 worker binaries
dist/    # final UmiAchievementClaimer.exe
```

## Requirements / Yêu cầu build

- Windows 10 or Windows 11, 64-bit.
- Windows PowerShell 5.1 or a compatible PowerShell host.
- Microsoft .NET Framework 4.x with the built-in C# compilers at:
  - `%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe`
  - `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe`

No NuGet packages or external runtime libraries are required.

Không cần NuGet package hoặc thư viện runtime ngoài.

## Build / Biên dịch

Open PowerShell in the repository root and run:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\build.ps1
```

The final application is generated at `dist\UmiAchievementClaimer.exe` and copied to the repository root as the release artifact:

```text
dist\UmiAchievementClaimer.exe
UmiAchievementClaimer.exe
```

App cuối được tạo trong `dist` và đồng thời chép ra thư mục gốc để có thể đính kèm trực tiếp vào GitHub Release.

The build script compiles two workers, embeds both as managed resources, compiles the WinForms application and prints its SHA-256 checksum.

Script build sẽ biên dịch hai worker, nhúng chúng vào app WinForms cuối và in checksum SHA-256.

## Release validation / Xác minh bản phát hành

Validation recorded for the included `v1.4.0` artifact:

```text
Assembly: UmiAchievementClaimer
Assembly version: 1.4.0.0
Embedded resources: AchievementWorker32, AchievementWorker64, UmiBrandLogo
Default language: English
Source files: 9
Read-only regression: 83/83 achievement rows
Resolved test interface: STEAMUSERSTATS_INTERFACE_VERSION012
Read-only result: RESULT=LIST_SUCCESS
Installed/package hash match: True
```

The regression used worker `list` mode. It did not call `SetAchievement` or `StoreStats`.

Bài regression sử dụng chế độ `list` chỉ đọc, không gọi `SetAchievement` hoặc `StoreStats`.

## GitHub publishing checklist

Before creating the release:

```powershell
git status --short
git add .gitignore build.ps1 README.md SHA256SUMS.txt UmiAchievementClaimer.exe assets docs src tools
git status --short
git commit -m "Release Umi Achievement Claimer v1.4.0"
```

Recommended GitHub tag:

```text
v1.4.0
```

Recommended release asset:

```text
UmiAchievementClaimer.exe
SHA256SUMS.txt
```

Do not add `build/` or `dist/`; both are generated and already excluded by `.gitignore`.

## Selecting achievements / Chọn achievement

1. Start Steam in Online mode and choose the game's installation folder.
2. The application first loads the list in read-only mode. Nothing is checked automatically.
3. Review the unlocked/locked state and description, then tick only the achievements you want.
4. Use **Select all** or **Select locked** only when that is what you intend.
5. Click **Claim selected achievements** and confirm the displayed selected count.

1. Mở Steam ở chế độ Online và chọn thư mục cài game.
2. App tải danh sách ở chế độ chỉ đọc trước; không có mục nào tự được tick.
3. Xem trạng thái/mô tả, rồi chỉ tick các achievement bạn muốn lấy.
4. Chỉ dùng **Chọn tất cả** hoặc **Chọn chưa mở** khi đúng nhu cầu.
5. Bấm **Claim mục đã chọn** và xác nhận số lượng hiển thị.

## Usage / Cách dùng

1. Close the target game. / Đóng game cần xử lý.
2. Start Steam in Online mode. / Mở Steam ở chế độ Online.
3. Run `UmiAchievementClaimer.exe`.
4. English opens by default; choose `Tiếng Việt` from the language selector if preferred.
5. Select the game's own installation folder, not the shared `steamapps\common` folder.
6. Verify the detected game name, AppID, DLL, architecture and User Stats interface.
7. Tick only the achievements you want, then click `CLAIM SELECTED` / `CLAIM ĐÃ CHỌN`.
8. Confirm and wait for `RESULT=SUCCESS`.

## How it works / Cơ chế

The main app scans Steam library manifests and the selected game directory. It picks a matching embedded worker based on the PE machine type of the Steam API DLL. List mode requests current User Stats and returns achievement names, descriptions and status without writing. Selected mode calls `SetAchievement` only for checked API IDs and calls `StoreStats` once at the end.

App chính đọc Steam library manifest và thư mục game. App chọn worker nhúng dựa trên kiến trúc PE của Steam API DLL. Chế độ danh sách chỉ đọc tên, mô tả và trạng thái; chế độ claim chỉ gọi `SetAchievement` với những API ID đã tick rồi gọi `StoreStats` một lần ở cuối.

For older Steamworks DLLs without flat C exports, the worker includes a legacy vtable fallback for known `ISteamClient` and `ISteamUserStats` layouts.

Với DLL Steamworks cũ không có C export, worker có nhánh tương thích vtable cho các layout `ISteamClient` và `ISteamUserStats` đã biết.

## Compatibility limits / Giới hạn tương thích

- Requires a standard Steamworks `steam_api.dll` or `steam_api64.dll`.
- Server-authoritative achievements may be rejected.
- Third-party launchers, anti-cheat, encrypted/custom Steam API builds or unsupported interface layouts may fail.
- DLC/server requirements still apply.
- The Steam account must have a valid session/license for the selected AppID.
- This changes Steam profile data and may not be fully reversible.

- Cần game sử dụng Steamworks DLL tiêu chuẩn.
- Achievement do máy chủ riêng kiểm soát có thể bị từ chối.
- Launcher ngoài, anti-cheat, DLL tùy biến/mã hóa hoặc interface chưa hỗ trợ có thể thất bại.
- Điều kiện DLC/máy chủ vẫn được áp dụng.
- Tài khoản Steam cần có phiên/license hợp lệ cho AppID.
- Thao tác ảnh hưởng dữ liệu hồ sơ Steam và có thể không hoàn tác đầy đủ.

## Source notes / Ghi chú mã nguồn

- `UmiAchievementClaimer.cs` contains manifest discovery, DLL/architecture/interface detection, localization, WinForms UI, embedded-worker extraction and process/log management.
- `UniversalAchievementWorker.cs` contains Steam API initialization, flat-export calls, legacy vtable fallback, achievement enumeration, `SetAchievement`, callback pumping and `StoreStats`.
- The project intentionally has no external dependencies so the entire implementation can be audited from these two source files.

## Security / An toàn

Only select a game directory you trust. The application loads the Steam API DLL found inside that directory into a temporary worker process. The worker is deleted from `%TEMP%\UmiAchievementClaimer` after completion when possible.

Chỉ chọn thư mục game đáng tin cậy. App sẽ nạp Steam API DLL tìm thấy trong thư mục đó vào worker tạm và cố gắng xóa worker khỏi `%TEMP%\UmiAchievementClaimer` khi hoàn tất.

## License

No license file is included by default. Add the license you want before publishing the repository publicly.

Mặc định chưa kèm file giấy phép. Hãy thêm license phù hợp trước khi public repository.
