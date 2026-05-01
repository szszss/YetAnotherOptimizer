# Yet another Optimizer

![RimWorld 1.6](https://img.shields.io/badge/RimWorld-1.6-blue)

*Yet another Optimizer* (*YaOpt*) is a performance optimization mod for *RimWorld*. It improves TPS and FPS through Harmony patching, multithreading, Burst-compiled math, and targeted caching.

## Features

Optimizations are grouped into three categories, all configurable in the in-game mod settings:

- **TPS** — Multi-threaded pawn tick, work giver scanning, thought updates, and map post-tick. Smart throttling for meditation, beds, idle checks, and wind updates. Caching for stats, component lookups, ideology checks, and lister operations.
- **FPS** — Early parallel render preparation, burst-compiled matrix computation, material property caching, texture caching, and mesh update throttling.
- **Misc** — Lazy texture loading, accelerated XML and translation operations, runtime type info caching, and mipmap atlas fixes.

## Requirements

- RimWorld 1.6
- [Harmony](https://github.com/pardeike/Harmony)
- *(Optional)* [Prepatcher](https://github.com/Zetrith/Prepatcher) — required for certain optimizations

## Installation

1. Download the latest release
2. Extract to `RimWorld/Mods/`
3. Enable in the mod menu

## Building from Source

Open `Source/YaOpt.sln` in Visual Studio 2022+ and build. Copy `Source/UserSettings.props.template` to `Source/UserSettings.props` first if you need to adjust paths.

The Burst module (`Source/YaOpt.Unity`) requires the Unity Editor 2022.3.35f1 with the Burst package. After compilation, copy the output to `1.6/Burst/`.

## License

MIT
