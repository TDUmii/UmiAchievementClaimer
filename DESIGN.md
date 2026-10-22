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

The main workspace heading is bold (18pt); the application title is bold (18pt), the game title is bold (16pt), and metric values are bold (20pt). Body copy and buttons use about 9pt, achievement rows use 9.5pt with bold names, metadata captions use 8pt, and secondary footer/detail copy uses 8.5pt. Selection summaries use bold 11pt. The confirmation dialog uses a bold 16pt heading and 10pt body.

Truncate constrained single-line text with an ellipsis and retain full names, descriptions and API IDs in tooltips. Preserve room for both English and Vietnamese labels.

## Layout

The default client is 1220 x 840; the minimum outer window is 1040 x 760. These are native desktop dimensions before DPI scaling, not web breakpoints. `AutoScaleMode.Dpi` and font-relative row metrics adapt the layout.

An 86px header holds the logo, name, Steam status and one compact `SETTINGS` button. There is no header subtitle. The page uses 24px horizontal inset, 20px top inset and 18px bottom inset. A fixed 266px sidebar column includes a 16px right gap; the achievement workspace fills the remaining width. The summary card uses 16px padding; the workspace uses 16px horizontal padding.

The workspace orders heading, the single-row folder path controls, metrics, filters/search, a compact selection menu, scrollable results, selected-item detail and a fixed selection footer. The path is a flat, borderless, vertically centered single-line field. The footer stays outside the scrolling rows. The only explanatory subtitle is inside the Achievements heading. The log spans both columns and is collapsed initially. At smaller heights its expanded area is reduced. Expanding technical details replaces the cover area in the sidebar so both regions do not compete for height.

## Elevation & Depth

The client uses tonal layering and one-pixel slate outlines rather than painted shadows. Cards stand above the canvas through their surface color. Inset fields and selected achievement rows use darker tones. Keyboard focus is explicit: buttons receive a cyan two-pixel outline; grid cells use focus rectangles. Do not add decorative glow or shadow to recreate this depth.

## Shapes

Cards have gently rounded corners (14px) and buttons use a tighter radius (8px). Fields, achievement rows and checkboxes remain rectangular. Keep actual artwork inside its allotted cover or icon area: the cover uses proportional Zoom and achievement icons retain a square presentation.

## Components

- **Buttons:** the primary and accent variants share lavender fill, dark text and bold 9pt labels. Secondary buttons use a muted surface with a one-pixel border; ghost controls use the card surface. Hover/pressed feedback is immediate. Disabled buttons use muted text and the disabled fill. Shared horizontal padding is 10px, with compact local overrides for filters and details.
- **Settings controls:** the header exposes one small `SETTINGS` button. Its compact dialog uses aligned desktop-settings rows for the `EN` / `VI` toggle, Steam Midnight/Carbon/Arcade presets and a custom accent picker. The picker derives hover/pressed colors and contrast-aware primary text, then persists the choice in the user's application data folder.
- **Game summary:** the cover, current game, detection status, AppID and architecture establish context. Technical details are an explicit disclosure. Read-only scan and confirmation guidance stays at the bottom.
- **Achievement grid:** a native vertically scrollable explorer-style list pairs a checkbox, state-specific artwork, bold name, secondary description and text status. Compact rows are at least 54px and grow with the font. Overflow uses a custom themed scrollbar with keyboard, wheel and drag input. Focused rows and checked items are separate states; row focus alone does not select an achievement for claiming. Space toggles the current row's checkbox.
- **Search and filters:** borderless text fields sit on the canvas tone. All / Locked / Unlocked filters change the visible collection while retaining every checked item. A single `SELECT` menu holds the visible, locked-visible and clear-all actions; the footer includes hidden selected items.
- **Selection footer:** the count, confirmation hint and Claim button stay together. Zero selection leaves Claim disabled. Confirmation shows game, AppID, exact selected/total count and the consequence warning; Cancel receives initial focus and is the Enter/Escape default.
- **Artwork:** use public Steam store/community metadata, keyed by achievement API ID or name, and official Steam CDN images. Load icons for visible rows and choose locked/unlocked imagery from the current state. Missing images retain the built-in fallback. Do not treat review fixtures as bundled product artwork.
- **Diagnostics:** the activity log is a collapsible card with status and Copy log; its text uses the canvas and muted-text tokens. Technical details and log disclosure do not obscure the Claim footer.

## Do's and Don'ts

- **Do** keep the selected Steam Midnight palette and the native black window frame.
- **Do** preserve text labels alongside status colors, clear focus treatment and English/Vietnamese fit.
- **Do** review empty, long-name, hidden-selection, busy, expanded-log and minimum-window states when changing composition.
- **Do** use `UiTheme.cs` as the runtime token source and update this document when the shared visual system changes.
- **Do** keep display typography, compact status labels and the aligned folder control row consistent across themes.
- **Don't** replace native caption buttons or resize behavior with fake chrome.
- **Don't** auto-select achievements, lose hidden checks during filtering, or make confirmation the default Enter action.
- **Don't** let artwork availability block review, selection or confirmation.
- **Don't** infer composed titlebar pixels, physical DPI behavior or real Steam claim compatibility from client-only synthetic render captures.
