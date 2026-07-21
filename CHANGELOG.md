# Changelog

All notable changes to Rust Optimizer are documented here.

## Unreleased

## 0.8.7
- Added a **Gameplay** page: a curated list of optional `client.cfg` tweaks that improve visibility or clarity with no performance cost, split into "Recommended for Everyone" (no real downside for anyone) and "Preferences" (a matter of taste) - each tweak is individually toggleable, and each section has a master switch to flip everything in it at once.
- Toggling a Gameplay tweak writes straight to `client.cfg` without taking a full backup snapshot each time, unlike Preset Profiles - flipping a few settings back and forth no longer floods the Backup & Restore history with junk entries.
- Added Raidboi-1129 (iOS and Android) to the Utilities page's resource list, a mobile raid cost and loot calculator for Rust.
- Removed the Streamer preset from Preset Profiles (now Low End PC, Competitive, and Cinematic) to cut decision fatigue.
- Added a Display card to the System page: current vs. maximum refresh rate and resolution, flagged with a warning icon and tooltip whenever either is running below what the monitor actually supports - e.g. a 144Hz panel stuck at 60Hz, or a 4K panel running at 1080p.
- Filled in the missing Danish and Russian translations for the entire Backup & Restore page and its confirm dialogs, which had been silently falling back to English.
- Fixed the Settings page's Light/Dark/System theme labels and the Backup & Restore page's Restore/Delete tooltips being hardcoded in English regardless of the selected language.

## 0.8.6
- Removed the standalone Configs page and folded it into a new Backup & Restore page, since both were really about the same thing: managing Rust's cfg files.
- Added a Backup & Restore page: switch between Settings (`client.cfg`) and Keybinds (`keys.cfg`), see each one's backup history, create a manually named backup (or leave it blank for a timestamp), and restore or delete any entry - both ask for confirmation first, since a delete can't be undone.
- Backups are now taken automatically before a Preset Profile is applied and before a restore overwrites the live file, replacing the old single `client.cfg.bak` that only ever kept one copy.
- Locked the Dashboard's Preset Profiles behind Rust being installed, instead of letting them be clicked with no effect.
- The System page's Storage card now refreshes every few seconds instead of only loading once when the page opens.
- Fixed button hover backgrounds across the app showing hard square corners instead of matching the button's own rounded shape.

## 0.8.5
- Added a System page (reachable from the sidebar): CPU/GPU/RAM live usage and specs, motherboard/BIOS, storage, and OS details, plus OS-level tweaks - power plan, pointer precision, Game Mode, background recording, and fullscreen optimizations for Rust.
- Added warning icons next to any System page setting that isn't at its recommended value, with a tooltip explaining why - including low RAM (under 16 GB), RAM running below its rated speed (XMP/EXPO not enabled in BIOS), and low free space on Rust's drive (under 10%).
- Wired the Dashboard's Optimization Overview System tile to the System page's real settings instead of a hardcoded "12 / 16 settings" placeholder, and gave it a three-stage red/yellow/green status instead of a plain good/not-optimized split.
- Dimmed the Optimization Overview's Performance, Network, and Graphics tiles, still mock data, to match the "coming soon" treatment already used on other unfinished features, so only the System tile reads as interactive.
- Made the Optimization Overview's System tile clickable, jumping straight to the System page, the same destination as the System Information card's "More Details" row.
- Fixed warning-icon tooltips sometimes needing the mouse moved back and forth before they'd show.
- Fixed the Dashboard's System score not refreshing after changing a setting on the System page until the app was restarted.
- Added ~~strikethrough~~ support to the changelog renderer.

## 0.8.4
- Added real Rust install detection: the app now finds Rust's actual install folder via Steam, so "Launch Rust" and "Verify Game Files" correctly disable themselves (with a red status indicator in the sidebar) if Rust isn't installed, instead of assuming it always is.
- Added working Preset Profiles: Low End PC, Competitive, Streamer, and Cinematic now actually rewrite Rust's graphics/performance settings in `client.cfg` (backing up the original first) instead of being non-functional placeholders.
- Removed the "Quick Optimization" section, which duplicated Preset Profiles without adding anything new.

## 0.8.3
- Fixed the Dashboard picking your integrated GPU instead of the discrete one (and reporting its usage) on systems with both, like an AMD APU paired with a Radeon card.
- Moved CPU/GPU name and RAM detection off WMI and the Win32 memory API onto the same LibreHardwareMonitor backend already used for live usage.

## 0.8.2
- Added real system info to the Dashboard's System Information card: your actual CPU/GPU model and live usage, and RAM used/total, replacing the old placeholder values.
- Added smooth hover and press transitions to buttons, and fixed a color flash that briefly appeared when switching the active sidebar item, most noticeable in dark mode.
- Fixed changelog text overflowing past the window edge instead of wrapping.
- Added blockquote and fenced code block support to the changelog renderer, and gave inline `code` a proper background and border instead of just swapping the font.

> This is what a blockquote looks like now.

```
fenced code blocks now render
in a bordered, monospaced box
```

## 0.8.1
- Added a real "Launch Rust" button that starts the game through Steam and automatically disables itself while Rust is already running.
- Filled in the missing Danish and Russian translations for the dashboard, sidebar, and settings screens, which had been silently falling back to English.

## 0.8.0

- Added the sidebar-navigation Dashboard UI: system overview, quick optimization presets, and preset profiles (currently mock data ahead of real system detection).
- Redesigned the Settings page with icon-based Light/Dark/System and language pickers.
- Upgraded the About page with a manual "Check for Updates" button, and links to GitHub, Discord, and Ko-fi.
- Switched the icon set to Phosphor Icons (with Simple Icons for brand logos), significantly shrinking the app's download size.

![Mock UI](https://raw.githubusercontent.com/tsgsOFFICIAL/RustOptimizer/master/Assets/Changelog/0.8.0%20mock%20ui.png)

## 0.7.0

- Added a Windows installer (`Setup.exe`) as an alternative to the portable zips, installing per-user with Start Menu and optional desktop shortcuts.
- Added automatic update checking: on launch, checks GitHub for a newer release and, if one exists, shows the version and rendered changelog inline before you decide to update.
- Added one-click updating that downloads and applies the new version in place (re-running the installer for installed copies, swapping files for portable copies) and relaunches automatically.

## 0.6.1

- Fixed changelog images and GIFs growing oversized as the window got wider, by capping their display size instead of letting them stretch to fill the available width.

## 0.6.0

- Added an in-app changelog viewer, so updates can explain *why* they happened instead of just announcing a version bump.
- Added support for images and animated GIFs inside the changelog, decoded frame-by-frame with no extra dependency.

![Changelog viewer](https://raw.githubusercontent.com/tsgsOFFICIAL/RustOptimizer/master/Assets/Changelog/screenshot-0.6.0.png)

![Language switching demo](https://raw.githubusercontent.com/tsgsOFFICIAL/RustOptimizer/master/Assets/Changelog/demo-0.6.0.gif)

## 0.5.0

- Fixed framework-dependent builds shipping self-contained runtime files.
- Changed the product version used for the file version shown in the title bar.

## 0.4.0

- Added a custom, borderless `TitleBar` control on Windows, replacing the native title bar.
- Added localization support (English, Danish, Russian) with automatic system-language detection.
- Added light/dark/system theme switching.
- Added the full IconPacks.Avalonia icon set via a submodule (pending an official Avalonia 12 release).