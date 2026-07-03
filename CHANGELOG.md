# Changelog

All notable changes to Rust Optimizer are documented here.

## Unreleased

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
