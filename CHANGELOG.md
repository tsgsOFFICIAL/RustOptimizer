# Changelog

All notable changes to Rust Optimizer are documented here.

## 0.10.4

### Graphics profiles

![Graphics profiles](https://raw.githubusercontent.com/tsgsOFFICIAL/RustOptimizer/master/Assets/Changelog/graphics-profiles.gif)

The Graphics page now has a profile picker, reachable from the Dashboard's **Manage Profiles** link. Pick Low End PC, Competitive or Cinematic and every slider snaps to match; nudge a slider afterward and it's flagged as unsaved, so you always know whether what you're looking at is actually what's applied.

- **Save your own profiles** - tweak the sliders from any starting point and save the result under its own name. Picking a saved profile later reapplies its exact settings.
- **Rename and delete** your own profiles. The three built-in presets can't be touched.
- If your sliders happen to land on the exact same settings as one of the built-in presets, the picker recognizes it as that preset instead of letting you save a duplicate.
- **Shadow Quality and Water Quality now have three genuinely different levels.** Medium used to be identical to Low under the hood for both, so switching between them did nothing. Medium is now its own, distinct setting.

## 0.10.3

Clear Cache was tested against a real AMD machine and a live Steam install, and several of the places it looked at turned out not to be places. It now clears a good deal more than it did.

- **Steam's built-in browser is now cleared** - the store, community and overlay pages all cache through it, and it had grown to around 500 MB without ever being touched. This is now routinely the largest single thing Clear Cache frees. Your sign-in and store settings are left alone.
- **Steam's depot cache was never being cleared** - it was looked for one folder away from where it actually lives. Around 55 MB on a typical install.
- Clear Cache now also clears Steam's download staging folder and its crash dumps. Game artwork and Steam's own logs are deliberately left alone, since artwork costs a re-download to rebuild and the logs are what Steam support asks for.
- **More AMD shader caches are now cleared** - Vulkan, OpenGL and DirectX 9, plus a second DirectX cache that was being missed entirely. The OpenGL cache was previously looked for under NVIDIA's name for it, so on an AMD machine it was never found, and one folder was being looked for that simply doesn't exist on any driver.
- **Rust's Unity logs are now actually cleared.** They were being looked for under `Facepunch` when the folder Rust really writes to is `Facepunch Studios LTD`, so `Player.log` had never been removed - and since nothing rotates it, it grows for as long as the install lives.
- Clear Cache now clears Rust's logs before Windows' temporary files rather than after. One of the log locations sits inside the temp folder, so the earlier order meant the temp step had already deleted it and the log step reported finding nothing.

## 0.10.2

- Fixed **Keep logs for** having no effect above 30 days. Old logs were being cleared as the app started, before your setting had been read, so anything older than 30 days was deleted no matter what you'd chosen. Pruning now waits until your setting is known.

## 0.10.1

- Fixed screenshots and GIFs in this changelog being locked to a small fixed width, leaving them stranded in the middle of a wide window. They now use the space available, without being scaled up past their own resolution.

## 0.10.0

### Settings, rebuilt

![Settings page](https://raw.githubusercontent.com/tsgsOFFICIAL/RustOptimizer/master/Assets/Changelog/settings-page.gif)

Grouped into **Appearance**, **Application**, **Updates**, **Units** and **Data** - and every option now says what it actually does instead of being a bare label.

- **Theme and Language are dropdowns** - tidier as more languages arrive, and each language shows its flag.
- **Start with Windows** - launches Rust Optimizer when you log in. Per-user, so it never asks for administrator rights.
- **Update settings** - check for a newer version on startup, and optionally install it automatically. Auto-install is off by default, since applying an update restarts the app.
- **Network speed units** - `MB/s` to match file sizes, or `Mbps` to match how connections are advertised. The Network page follows your choice.
- **Log settings** - keep logs for 7, 30 or 90 days, open the log folder directly, and switch on **verbose logging**.

> Turn verbose logging on *before* reproducing a problem. The log will then explain what the app was actually doing - which paths it looked at, what it found, and how long each step took.

> **Heads up:** your theme and language reset to their defaults once when you upgrade. All preferences now live in a single settings file and the old ones aren't carried across. Set them again and they'll stick.

### About

- Added an **Application information** section - version, build date, operating system and licence - so a bug report can include the details that matter without hunting for them.
- Moved the Ko-fi link into the footer beside GitHub and Discord, reachable from every page instead of only this one.

### Elsewhere

- Links the app opens now tell the destination site they came from Rust Optimizer.

### Fixes

- Plain buttons had sharper corners than every other button in the app.
- The Danish and Russian names for *Backup & Restore* were cut off in the sidebar.
- Graphics, Gameplay, Settings and About sat at a different width from every other page, leaving their headings out of line.

## 0.9.0
- Added **Clear Cache** to the Dashboard's Quick Actions: clears Windows temporary files, GPU shader caches (NVIDIA, AMD, Intel and DirectX), Steam's download, depot and store caches, Rust's Unity logs, and application crash dumps - then reports how much it actually freed underneath the button.
- Clear Cache asks before it runs, with three things you can switch off first: emptying the Recycle Bin, clearing the thumbnail cache, and including system files (which asks for administrator approval). Everything else it clears is safe to remove and rebuilds on its own.
- Clear Cache shows its progress while it works, naming each group as it goes, and can be stopped part-way - a cancelled run still reports what it managed to free.
- Clear Cache leaves shader caches alone while Rust is running - and tells you it did, rather than silently freeing less than you expected. Files that are genuinely in use are skipped and counted rather than treated as errors.
- Rust may take a little longer to start the first time after clearing shader caches, while they rebuild. That's expected, and the prompt says so up front.
- Added a **Network** page: live info for your active adapter (link speed, local IPv4/gateway/MAC/DNS), a continuously updating ping and jitter reading to 1.1.1.1, live download/upload throughput, and an opt-in public IP lookup you check on demand rather than automatically - plus a link to Speedtest.net for a full download/upload/latency test.
- Added three Network tweaks - disabling Windows' network throttling, NIC power saving, and QoS reserved bandwidth - each briefly showing an administrator prompt when applied, since these particular settings can't be changed without elevated permissions.
- Added a warning icon on the Network page when your active connection is Wi-Fi instead of wired Ethernet, since a cable gives meaningfully lower and more consistent latency.
- Wired the Dashboard's Optimization Overview Network tile to the Network page's real settings instead of a hardcoded "8 / 10 settings" placeholder, matching how the System tile already works.

![Network page](https://raw.githubusercontent.com/tsgsOFFICIAL/RustOptimizer/master/Assets/Changelog/network-page.png)

## 0.8.8
- Added a **Graphics** page: simplified quality sliders (Shadow Quality, Texture Quality, Effects Quality, Draw Distance, World Detail, Water Quality, Inventory Display) that each write straight to `client.cfg` - the same convar values Preset Profiles already use, now adjustable one setting at a time instead of only as whole bundles.
- Each Graphics slider shows a live preview image that updates as you drag between Low/Medium/High, including animated GIFs for before/after comparisons - falls back to a plain placeholder for any setting that doesn't have a preview image yet.

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