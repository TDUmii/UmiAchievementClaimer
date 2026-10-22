# Steam Midnight v2 screenshot provenance

These screenshot files are rendered from **the current local WinForms UI** by `tests/UiRegression.cs` (last checked October 22, 2026).

- `steam-midnight-v2.png` — copied from `.impeccable/review/artwork-client.png`; 1220 x 840 client-area render. Shows **Portal 2** cover and achievement icons obtained from public Steam metadata/CDN, but the game identity text, installation path, achievement entries and states are **synthetic UI test fixtures**, not a real account session.
- `steam-midnight-v2-log.png` — copied from `.impeccable/review/artwork-minimum-details-log-client.png`; 1024 x 721 client-area render with technical details and activity log expanded at minimum-size layout.

The screenshots use the current horizontal game summary / unified achievement toolbar / themed log layout. They are **not** screenshots of a real Steam achievement claim and should not be represented as evidence of compatibility with a target game.

The rendered images do not contain the compositor-rendered native Windows titlebar. The Steam artwork is downloaded for UI verification and is **not packaged** as an asset inside the compiled application.

For historical reference, `steam-midnight.png` and `steam-midnight.sources.md` describe the previous UI design, which is no longer used in the main README preview.
