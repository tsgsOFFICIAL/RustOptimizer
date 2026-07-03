# Changelog

All notable changes to Rust Optimizer are documented here.

## Unreleased

- Added an in-app changelog viewer, so updates can explain *why* they happened instead of just announcing a version bump.

## 0.5.0

- Fixed framework-dependent builds shipping self-contained runtime files.
- Changed the product version used for the file version shown in the title bar.

## 0.4.0

- Added a custom, borderless `TitleBar` control on Windows, replacing the native title bar.
- Added localization support (English, Danish, Russian) with automatic system-language detection.
- Added light/dark/system theme switching.
- Added the full IconPacks.Avalonia icon set via a submodule (pending an official Avalonia 12 release).
