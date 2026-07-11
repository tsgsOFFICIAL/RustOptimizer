# Changelog

All notable changes to Rust Optimizer are documented here.

## Unreleased
- Added **Memory Speed** and **Max Memory Speed** rows to the Dashboard's System Information card, read from Windows' `Win32_PhysicalMemory` info - the same source Task Manager uses.
- Added ~~strikethrough~~ support to the changelog renderer.

> If the two numbers differ, your RAM's XMP/EXPO profile probably isn't enabled in BIOS, so it's running at a slower default speed instead.

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
