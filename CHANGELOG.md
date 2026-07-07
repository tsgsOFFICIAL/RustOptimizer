# Changelog

All notable changes to Rust Optimizer are documented here.

## Unreleased

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
