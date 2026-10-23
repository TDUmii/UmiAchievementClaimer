---
name: Umi Achievement Claimer - Steam Midnight
description: A native Windows game client for reviewing and explicitly selecting Steam achievements.
colors:
  canvas: "#0C1422"
  surface: "#19283B"
  surface-muted: "#131F2F"
  border: "#304156"
  primary: "#A999ED"
  cyan: "#74D3E8"
  text: "#EBF1FA"
  muted-text: "#A4B5CB"
  success: "#7ED9B8"
  warning: "#F3B37A"
  disabled: "#2D374B"
  primary-hover: "#BBADF8"
  primary-pressed: "#9785DE"
  secondary-hover: "#27384E"
  window-chrome: "#000000"
theme-presets:
  default: "Steam Midnight"
  options: ["Steam Midnight", "Carbon", "Arcade", "Custom accent"]
typography:
  display:
    fontFamily: "Bahnschrift with Segoe UI fallback"
    fontWeight: 700
  heading:
    fontFamily: "Bahnschrift"
    fontSize: "18pt"
    fontWeight: 700
  title:
    fontFamily: "Bahnschrift"
    fontSize: "18pt"
    fontWeight: 700
  metric:
    fontFamily: "Bahnschrift"
    fontSize: "20pt"
    fontWeight: 700
  body:
    fontFamily: "Segoe UI"
    fontSize: "9pt"
    fontWeight: 400
  achievement:
    fontFamily: "Segoe UI"
    fontSize: "9.5pt"
    fontWeight: 400
  caption:
    fontFamily: "Segoe UI"
    fontSize: "8pt"
    fontWeight: 400
  log:
    fontFamily: "Consolas"
    fontSize: "9pt"
    fontWeight: 400
rounded:
  button: "8px"
  card: "14px"
spacing:
  small: "8px"
  compact: "12px"
  gap: "16px"
  card: "16px"
  page: "24px"
components:
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.canvas}"
    rounded: "{rounded.button}"
    padding: "0 10px"
  button-primary-hover:
    backgroundColor: "{colors.primary-hover}"
    textColor: "{colors.canvas}"
    rounded: "{rounded.button}"
  button-secondary:
    backgroundColor: "{colors.surface-muted}"
    textColor: "{colors.text}"
    rounded: "{rounded.button}"
  button-ghost:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.text}"
    rounded: "{rounded.button}"
  card:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.text}"
    rounded: "{rounded.card}"
    padding: "16px"
  input:
    backgroundColor: "{colors.canvas}"
    textColor: "{colors.text}"
---

# Design System: Umi Achievement Claimer

## Overview

**Creative North Star: "Steam Midnight"**

The selected direction is **Operate**: a focused native workspace for choosing a game, reviewing status, making an explicit selection and confirming it. Navy surfaces, lavender actions and restrained cyan navigation establish the chosen world. Real Steam artwork identifies the game and achievement without competing with names, states or selection controls.

The window keeps native Windows chrome. DWM requests a black caption and border with light caption text on Windows 11; older Windows builds retain the supported dark-frame fallback. System buttons and resizing remain native. The black, VS Code-like frame is an operating-system treatment, not a drawn titlebar.

## Colors

The frontmatter is the normative palette, extracted from `src/UI/UiTheme.cs` and `WindowTheme.cs`.

- **Primary:** soft lavender marks the main Claim action, checked boxes and current-game label. Its hover and pressed tokens provide immediate feedback.
- **Secondary:** pale cyan marks scan/details navigation, active filter text and button keyboard focus.
- **Status:** mint communicates unlocked or ready states; warm amber communicates locked states and the profile-change warning. Status always has a text label.
- **Neutral:** the dark canvas supports raised navy cards, muted inset areas and quiet slate borders. Light text carries names and actions; muted text carries descriptions, captions and diagnostics. Disabled controls use their dedicated fill.
- **Chrome:** black belongs to the native caption and window border. The application canvas remains navy.

## Typography

Use Bahnschrift for display type with a Segoe UI fallback, Segoe UI for supporting copy and Consolas for the activity log and installation path. Values are WinForms points; do not reinterpret them as CSS pixels. Windows supplies the native caption font.

The main workspace heading is bold (18pt); the application title is bold (18pt), the game title is bold (17pt), and metric values are bold (14.5pt). Body copy and buttons use about 9pt, achievement rows use 9.5pt with bold names, metadata captions use 8pt, and secondary footer/detail copy uses 8.5pt. Selection summaries use bold 11pt. The confirmation dialog uses a bold 16pt heading and 10pt body.

Truncate constrained single-line text with an ellipsis and retain full names, descriptions and API IDs in tooltips. Preserve room for both English and Vietnamese labels.

## Layout

The app has an 86px native header, a full-width horizontal game summary, a full-width achievement workspace and a collapsible full-width activity log. The default client is 1220 x 840 and the minimum window is 1040 x 760, with DPI scaling.

The horizontal game summary has three zones: official game artwork (180px), game identity and achievement progress (fluid), and installation-folder controls (328px). The main zone displays game name, detection status, Steam AppID, architecture, total/unlocked/locked metrics and a small progress stripe. The right zone contains a vertical path field, Browse folder and Rescan buttons, and a technical-details disclosure. Opening technical details replaces the right-hand controls without displacing artwork or game identity. Browsing starts a scan immediately; Rescan repeats it.

The achievement workspace has a heading, one combined toolbar (search, All/Locked/Unlocked tabs, Quick select menu), the scrollable two-line achievement list, selection detail and fixed selection/Claim footer. The list takes the remaining vertical space. Search has an inline placeholder until focused or filled. Bulk menu actions preserve hidden selection semantics. Both cards span the full width with aligned edges. The log and worker progress indicator sit below.

Keep the primary workflow immediately scannable: identify the current game, search/check exact achievements, then review/confirm Claim. Avoid unused sidebar space and tiny uppercase labels.

## Elevation & Depth

The client uses tonal layering and one-pixel slate outlines rather than painted shadows. Cards stand above the canvas through their surface color. Inset fields and selected achievement rows use darker tones. Keyboard focus is explicit: buttons receive a cyan two-pixel outline; grid cells use focus rectangles. Do not add decorative glow or shadow to recreate this depth.

## Shapes

Cards have gently rounded corners (14px) and buttons use a tighter radius (8px). Fields, achievement rows and checkboxes remain rectangular. Keep actual artwork inside its allotted cover or icon area: the cover uses proportional Zoom and achievement icons retain a square presentation.

## Components

- **Buttons:** the primary and accent variants share lavender fill, dark text and bold 9pt labels. Secondary buttons use a muted surface with a one-pixel border; ghost controls use the card surface. Hover/pressed feedback is immediate. Disabled buttons use muted text and the disabled fill. Shared horizontal padding is 10px, with compact local overrides for filters and details.
- **Settings controls:** the header exposes one small `SETTINGS` button. Its compact dialog uses aligned desktop-settings rows for the `EN` / `VI` toggle, Steam Midnight/Carbon/Arcade presets and a custom accent picker. The picker derives hover/pressed colors and contrast-aware primary text, then persists the choice in the user's application data folder.
- **Game summary:** the cover, current game, detection status, AppID and architecture establish context. Technical details are an explicit disclosure. Read-only scan and confirmation guidance stays at the bottom.
- **Achievement grid:** a native vertically scrollable explorer-style list pairs a checkbox, state-specific artwork, bold name, secondary description and text status. Compact rows are at least 54px and grow with the font. Overflow uses a custom themed scrollbar with keyboard, wheel and drag input. Focused rows and checked items are separate states; row focus alone does not select an achievement for claiming. Space toggles the current row's checkbox.
- **Search and filters:** borderless text fields sit on the canvas tone. All / Locked / Unlocked filters change the visible collection while retaining every checked item. A single Quick select menu holds the visible, locked-visible and clear-all actions; the footer includes hidden selected items.
- **Selection footer:** the count, confirmation hint and Claim button stay together. Zero selection leaves Claim disabled. Confirmation shows game, AppID, exact selected/total count and the consequence warning; Cancel receives initial focus and is the Enter/Escape default.
- **Artwork:** use public Steam store/community metadata, keyed by achievement API ID or name, and official Steam CDN images. Load icons for visible rows and choose locked/unlocked imagery from the current state. Missing images retain the built-in fallback. Do not treat review fixtures as bundled product artwork.
- **Diagnostics:** the activity log is a collapsible card with status and Copy log; its text uses the canvas and muted-text tokens. Technical details and log disclosure do not obscure the Claim footer.

## Do's and Don'ts

- **Do** keep the selected Steam Midnight palette and the native black window frame.
- **Do** preserve text labels alongside status colors, clear focus treatment and English/Vietnamese fit.
- **Do** review empty, long-name, hidden-selection, busy, expanded-log and minimum-window states when changing composition.
- **Do** use `UiTheme.cs` as the runtime token source and update this document when the shared visual system changes.
- **Do** keep display typography, compact status labels and the stacked folder input and action row consistent across themes.
- **Don't** replace native caption buttons or resize behavior with fake chrome.
- **Don't** auto-select achievements, lose hidden checks during filtering, or make confirmation the default Enter action.
- **Don't** let artwork availability block review, selection or confirmation.
- **Don't** infer composed titlebar pixels, physical DPI behavior or real Steam claim compatibility from client-only synthetic render captures.


## Steam library picker and compact-state finish (2026-10-23)

The game summary's folder action row has three controls: lavender **My games** (primary discovery entry), **Browse** (manual fallback), and **Rescan** (disabled until a valid path is available). The corresponding picker has a focused heading, short guidance, in-theme search, two-line game rows with AppID and path, a custom dark scrollbar for overflowing libraries, a visible result count and explicit Scan / Cancel / Browse actions. The picker never auto-selects a game to claim.

The achievement explorer renders its empty-state guidance directly in the grid's paint pass to stay visible over native child-window composition. Keyboard shortcuts are Ctrl+L, Ctrl+F and F5. On smaller windows, expanding the diagnostics panel prioritizes retaining at least one complete achievement row. Discovery and detection use asynchronous read-only operations; the interface shows busy status while scanning. The Windows-native titlebar remains system-composited, and client DrawToBitmap captures do not verify its exact pixels.

## Native UI additions — v1.10.0

The main toolbar now has a compact Sort control beside the status filters and Quick select menu, preserving the Steam Midnight dark surfaces. A Preflight command is visible in the native header. The Cancel button appears only during worker execution, and all status reports distinguish cancelled/timed-out operations from ordinary errors. Double-clicking an achievement opens a read-only details dialog. The Steam library dialog offers Favorites-only filtering and a favorite toggle; cards continue to use a concise two-line, accessible game list. See docs/QA_V1_10.md for scope and validation limits.

## Insights 1.11.0

Steam Midnight Insights is a read-only dialog with local game metrics, snapshot history, timeline drill-down and recovery comparison, plus manual CSV export and update checking. Achievement Inspector displays public global rarity and optional native unlock time, with explicit unavailable fallbacks. The main header exposes Insights without changing the Claim flow.

## Windows UI polish 1.11.1

The first Activity log expansion uses explicit high-contrast text; the search box uses a native Windows cue (never an overlaid placeholder); a visible empty-state label replaces the hidden DataGridView whenever there are no visible rows. Settings presents readable Language and Appearance panels. Insights offers three distinct metric cards, a themed history list and roomier recovery actions. No native window resizing behavior is removed to fake a dark border. See docs/QA_V1_11_1_UI.md.
