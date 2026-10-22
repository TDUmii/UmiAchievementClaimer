# Screenshot provenance

`steam-midnight.png` is a native WinForms client-area render produced by `tests/UiRegression.cs` from Umi Achievement Claimer 1.8.0. Game identity uses Portal 2; achievement statuses and test rows are explicitly synthetic. The image contains no real Steam profile data or Steam account data. The installation path is a synthetic UI fixture.

The cover and locked/unlocked icons were downloaded by `tests/ArtworkSmoke.cs` from public Steam metadata and official Steam CDN image URLs. Source metadata: https://store.steampowered.com/api/appdetails?appids=620&l=english and https://steamcommunity.com/stats/620/achievements/?l=english.

Icon sources: https://shared.fastly.steamstatic.com/community_assets/images/apps/620/SURVIVE_CONTAINER_RIDE.jpg and https://shared.fastly.steamstatic.com/community_assets/images/apps/620/SURVIVE_CONTAINER_RIDE_BW.jpg.

The runtime app downloads these images when available. These images are not bundled as application assets. The screenshot shows the Steam Midnight default theme, the dedicated `SETTINGS` action, the compact `SELECT` achievement menu, the centered folder row and the Bahnschrift-style display hierarchy. It does not show or simulate compositor-rendered native titlebar colors.
