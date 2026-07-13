# Rust Optimizer

**Optimize your system and Rust for maximum performance.**

> Get the best performance out of your system so you can focus on what matters: **winning.**

[![Issues](https://img.shields.io/github/issues/tsgsOFFICIAL/RustOptimizer)](https://github.com/tsgsOFFICIAL/RustOptimizer/issues)
[![Last Commit](https://img.shields.io/github/last-commit/tsgsOFFICIAL/RustOptimizer)](https://github.com/tsgsOFFICIAL/RustOptimizer/commits/master)
[![Nightly Build](https://github.com/tsgsOFFICIAL/RustOptimizer/actions/workflows/nightly.yml/badge.svg)](https://github.com/tsgsOFFICIAL/RustOptimizer/actions/workflows/nightly.yml)
[![Release Build](https://github.com/tsgsOFFICIAL/RustOptimizer/actions/workflows/release.yml/badge.svg)](https://github.com/tsgsOFFICIAL/RustOptimizer/actions/workflows/release.yml)
[![GitHub license](https://img.shields.io/github/license/tsgsOFFICIAL/RustOptimizer)](https://github.com/tsgsOFFICIAL/RustOptimizer/blob/master/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-blueviolet)](https://dotnet.microsoft.com/download)
[![Avalonia](https://img.shields.io/badge/Avalonia-12-blue?logo=avaloniaui&logoColor=f5f5f5)](https://avaloniaui.net/)
[![GitHub stars](https://img.shields.io/github/stars/tsgsOFFICIAL/RustOptimizer?style=social)](https://github.com/tsgsOFFICIAL/RustOptimizer)
[![Discord](https://img.shields.io/discord/227048721710317569?logo=discord&logoColor=FFF&label=Discord&labelColor=5865F2)](https://discord.gg/Cddu5aJ)
[![Support on Ko-fi](https://img.shields.io/badge/Support%20me%20on%20Ko--fi-F16061?logo=ko-fi&logoColor=white)](https://ko-fi.com/tsgsOFFICIAL)

## Table of Contents

- [Features](#features)
- [Screenshots](#screenshots)
- [Requirements](#requirements)
- [Quick Start](#quick-start)
- [Building from Source](#building-from-source)
- [Support the Project](#support-the-project--the-creator)
- [How to Contribute](#how-to-contribute)
- [Star History](#star-history)
- [License](#license)

## Features

- **One-Click Smart Optimization**: Automatically tunes your system and Rust for peak performance
- **Preset Profiles**: Low-End PC (Recommended), Competitive (High FPS), Streamer (Balanced), Cinematic (Best Quality), Custom
- **Optimization Overview**: Real-time status for Performance, System, Network, and Graphics
- **Quick Actions**: Verify Game Files, Clear Cache, Optimize Startup, Update Drivers
- **Direct Rust Launch**: Start the game with optimized settings from the app
- **Modern Dark UI**: Clean, responsive WPF interface with live hardware info
- **Safe & Reversible**: All changes are non-destructive and easy to revert

## Screenshots

![Dashboard](https://github.com/tsgsOFFICIAL/RustOptimizer/raw/master/Assets/Github/Dashboard.png)
> *Main dashboard with optimization overview, quick actions, and preset profiles*

---

## Requirements

- Windows 10/11 (64-bit)
- Rust (the game) installed via Steam
- Administrator privileges (recommended for full system optimizations)

## Quick Start

1. **Download** the latest release from [Releases](https://github.com/tsgsOFFICIAL/RustOptimizer/releases/latest) (recommended). Run `Setup.exe` for a guided install, or extract one of the zips for a portable copy.
2. Run `Rust Optimizer.exe`.
3. Ensure Rust is not running, or close it down before optimizing.
3. Review your system info and click **Run Smart Optimization**.
4. Choose a preset profile or customize.
5. Click **Launch Rust** and dominate.

> **Nightly Builds:** Want the latest code before it gets a tagged release? Every push to `master` automatically rebuilds and republishes the [nightly release](https://github.com/tsgsOFFICIAL/RustOptimizer/releases/tag/nightly), both self-contained and framework-dependent. The version number includes the short commit hash it was built from, so you always know exactly what you're running. It's untested beyond CI passing, so expect the occasional rough edge, grab a regular [Release](https://github.com/tsgsOFFICIAL/RustOptimizer/releases/latest) instead if you want something stable.

## Building from Source

Requires the [.NET SDK](https://dotnet.microsoft.com/download) installed.

```bash
git clone https://github.com/tsgsOFFICIAL/RustOptimizer.git
cd RustOptimizer
dotnet restore
dotnet publish -c Release -r win-x64 --self-contained
```

Executable will be in the publish folder.

## Support the Project & the creator

This tool is **100% free**.  
If this tool helped you, and you want to fuel more rage-fueled coding sessions, then hit that button and become a legend.

[![Support on Ko-fi](https://img.shields.io/badge/Support%20me%20on%20Ko--fi-F16061?logo=ko-fi&logoColor=white)](https://ko-fi.com/tsgsOFFICIAL)

## How to Contribute

Bugs, ideas, docs, code, translation, all of it helps. A few ground rules so we can operate at mach speed:

**Don't:**

- Rebrand or republish this as your own, keep attribution to me (tsgsOFFICIAL) and this repo intact
- Strip license headers, credits, or "made by" notices when sharing builds or forks
- Drop a giant PR that rewrites half the codebase with no issue or discussion first
- Bundle unrelated changes into a feature fix

**Do:**

- Open an issue first for anything bigger than a small fix
- Keep PRs focused, one logical change per PR
- Test before submitting
- Match the existing naming, structure, and code style

### (Suggested) Workflow

1. Fork [tsgsOFFICIAL/RustOptimizer](https://github.com/tsgsOFFICIAL/RustOptimizer)
2. Branch off (`fix/optimization-bug`, `feature/new-preset`, `local/zh-CN`, etc.)
3. Make your changes and test
4. Open a PR against `master`

Not sure where to start? Check [open issues](https://github.com/tsgsOFFICIAL/RustOptimizer/issues) or ask in the [Discord Server](https://discord.gg/Cddu5aJ).

## Star History

<a href="https://www.star-history.com/#tsgsOFFICIAL/RustOptimizer&type=date&legend=top-left">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/svg?repos=tsgsOFFICIAL/RustOptimizer&type=date&theme=dark&legend=top-left" />
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/svg?repos=tsgsOFFICIAL/RustOptimizer&type=date&legend=top-left" />
   <img alt="Star History Chart" src="https://api.star-history.com/svg?repos=tsgsOFFICIAL/RustOptimizer&type=date&legend=top-left" />
 </picture>
</a>

## License

[MIT License](LICENSE), see the file for details.

---

**Made with rage, caffeine, and zero sleep by tsgsOFFICIAL**  
Enjoy the boost in performance! 🚀

---

*This tool is not affiliated with, endorsed by, or sponsored by Facepunch Studios*