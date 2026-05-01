# YetAnotherOptimizer Development Guide

This project is a high-performance optimization mod for **RimWorld**, designed to significantly increase game frame rates (TPS/FPS) through Harmony patches, multi-threading parallelization, cache optimization, and Unity Burst compilation.

## 1. Project Architecture

The project consists of three modules:

### 1.1 `Source/YaOpt` (Core Module)
- **Purpose**: Contains the main logic of the mod, Harmony patches, and general helper tools.
- **Key Components**:
  - `Patches/`: Harmony patches organized by target namespace. Subdirectories:
    - `Early/` — Patches applied before other mods load via the `EarlyHarmony` instance (ID `"YetAnotherOptimizer.Early"`).
    - `Prepatch/` — Patches for the Prepatcher mod.
    - `ThreadSafe/` — Thread-safety patches. Subcategories: `ThreadLocal/` (per-thread instance fields), `ThreadStatic/` (simple `[ThreadStatic]` replacements), `Locked/` (critical section lock injection), `Delayed/` (deferred execution).
    - `Compatibility/` — Runtime compatibility patches for other mods (e.g., `RocketMan`, `PerformanceFish`, `CombatExtended`, `VehicleFramework`) plus `Unpatcher.cs` for conflict resolution.
  - `Helpers/`: Utility classes including:
    - `ParallelPawnTickManager`, `ParallelMapTickManager`, `ParallelWorkGiver` — Multi-threaded tick computations via Unity Job System.
    - `UpdateCallbackHelper.cs` — Central event system for game lifecycle hooks (pre-tick, post-tick, pre-render, post-render, cache-clear).
    - `ThreadLocal/` — Per-thread helper classes for common APIs.
    - `ThreadSafe/` — Lock primitives (`GreedySpinLock`, `GreedyMonitor`) and `LockPatchManager`.
  - `YaOptMod.cs`: Handles initialization, settings loading, early patching, and logging.
  - `YaOptGlobal.cs`: Central hub for mod state, settings, platform capabilities, thread-safety flags.
  - `YaOptSubMod.cs`: Base class for sub-modules with lifecycle hooks (`OnPreInit`, `OnInit`, `OnPostInit`, `OnPatch`, `OnUnpatch`).
  - `CompatibilityChecker.cs`: Validates compatibility with other optimizer mods (Performance Fish, Butter++, Performance Optimizer).
  - `PostIniter.cs`: Startup initializer that triggers patching after game loads.
  - `NativeLoader.cs`: Loads platform-specific Burst native libraries.

### 1.2 `Source/YaOpt.Unity` (Burst Module)
- **Purpose**: Utilizes Unity's **Burst Compiler** for high-performance mathematical operations and data processing.
- **Features**:
  - For reference only. It is not a complete C# project. It needs to be imported into a Unity project for compilation.
  - Uses `Unity.Mathematics` (`float4`, `float3`) instead of standard `Vector3` to leverage SIMD instructions.
  - Uses `[BurstCompile]` to mark critical paths.
  - Strictly prohibits referencing any managed objects (reference types); only uses `NativeArray` and unmanaged structures (`struct`).

### 1.3 `Source/YaOpt.OtherMod.*` (Compatibility Submods)
- **Purpose**: Compatibility patches for specific popular mods (e.g., `CombatExtended`, `FacialAnimation`, `HumanoidAlienRaces`, `ImageOpt`, `VanillaExpandedFramework`).
- **Architecture**: Each project extends `YaOptSubMod` and receives lifecycle calls during initialization and patching.
- **Principles**: Must reference target mod APIs via reflection or soft dependencies to avoid hard crashes if the target mod is missing.

## 2. Directory Structure

```
Root
├── 1.6/                        # Release directory
│   ├── Assemblies/             # Main managed DLL (YaOpt.dll)
│   ├── Burst/                  # Burst native libraries (.dll/.so/.bundle)
│   └── Mods/                   # OtherMod standalone DLL output
├── Common/
│   ├── Defs/
│   │   └── Compatibilities/    # CompatibilityDef XML files
│   └── Languages/              # Translation files (ChineseSimplified, ChineseTraditional, English)
├── Source/
│   ├── YaOpt/                  # Core C# project
│   │   ├── Defines/            # Compile-time compatibility definitions
│   │   ├── Helpers/            # Utility classes and trampolines
│   │   ├── Patches/            # Harmony patches (Early, Prepatch, ThreadSafe, Compatibility)
│   │   └── Settings/           # Option framework
│   ├── YaOpt.Unity/            # Unity Burst project (reference only)
│   ├── YaOpt.OtherMod.*/       # Compatibility sub-module projects
│   ├── Settings.props          # Shared build properties (GameVersion, HarmonyVersion)
│   └── YaOpt.sln               # Visual Studio solution
├── About/                      # Mod metadata (About.xml, icon)
├── LoadFolders.xml             # Loading logic
└── Analyzer.xml                # Dubs Performance Analyzer configuration
```

## 3. Coding Conventions

### 3.1 Naming & Style
- **Class/Method/Property**: `PascalCase` (e.g., `ParallelTickManager`, `DoSingleTick`)
- **Private Fields**: `_camelCase` (e.g., `_cachedMaterial`, `_padding0`)
- **Parameters/Local Variables**: `camelCase`
- **Brace Style**: **Allman** (braces on a new line)
  ```csharp
  if (condition)
  {
      DoSomething();
  }
  ```
- **Exemption**: Ignore the non-standard naming in `Source/YaOpt/CompatibilityDef.cs`, where member names are designed for compatibility with RimWorld deserialization.

### 3.2 Performance Guidelines
Since this is an optimization mod, performance is the highest priority:
- **Hot Paths**: In `Tick`, `Draw`, `Update` loops:
  - **NO LINQ IN HOT CODE** (LINQ in the initialization code are acceptable). 
  - **Avoid Memory Allocations** (new Class(), lambda closures, params object[]).
- **Inlining**: Use `[MethodImpl(MethodImplOptions.AggressiveInlining)]` for small, frequently called methods.
- **Structs**: Strictly use `struct` in `YaOpt.Unity` and pass by `ref` to reduce copying. In core modules, prefer `struct` for hot-path data containers (e.g., `JobResult` in `ParallelWorkGiver`).
- **Unity Job System**: Use `Unity.Jobs` (`IJobParallelFor`, `JobHandle`, `NativeArray`, `NativeList`, `NativeQueue`) for multi-threaded data processing. Jobs must only access unmanaged data.
- **Thread Safety**:
  - Use `[ThreadStatic]` for simple per-thread mutable state.
  - Use `ConcurrentDictionary`, `ConcurrentBag`, `ConcurrentQueue` for thread-safe producer/consumer patterns.
  - Use `GreedySpinLock` / `GreedyMonitor` (from `Helpers/ThreadSafe/`) for lightweight critical sections when lock contention is low.
  - Lock patches declared via `CompatibilityDef` XML use `LockPatchManager` for runtime lock injection.
- **Bloom Filter**: Use for fast negative match filtering in hot paths (e.g., `CompatibilityDefines.IsJobFailurePredictingIgnored`).

### 3.3 Harmony Patch Standards
- **Transpiler Priority**: Prefer using `Transpiler` to modify IL instructions to avoid the overhead of Prefix/Postfix.
- **Defensive Programming**: When searching for instructions in a Transpiler, consider that other mods may have already modified the code.
- **Comments**: Complex IL operations must include comments explaining the intent (*Why*), not just the operation (*What*).
- **Patch Attributes**:
  - `[HarmonyPatch]` — Standard Harmony auto-detection. Used for most patches.
  - `[ManualPatch]` — For patches requiring custom logic beyond what auto-detection supports. The type must implement a static `Patch(Harmony)` or `bool Patch(Harmony)` method.
  - `[EarlyPatch]` — Marks patches to be applied on the `EarlyHarmony` instance before other mods load.
- **Harmony Instances**: The project maintains two Harmony instances:
  - `"YetAnotherOptimizer"` — Main instance for standard patches.
  - `"YetAnotherOptimizer.Early"` — Applied during mod constructor, before other mods' patches execute.

### 3.4 Documentation Style
- **Be Concise**: XML documentation should be brief and to the point.
- **Summary Only for Simple Members**: For straightforward methods/properties, a single `<summary>` line is sufficient.
- **Remarks for Complexity**: Use `<remarks>` only when explaining non-obvious behavior, thread safety, or performance implications.
- **No param/returns**: Do not write `<param>` or `<returns>` tags unless the meaning is truly ambiguous.
- **Avoid Lists**: Minimize use of `<list>` elements. Prefer direct prose.
- **Example**:
  ```csharp
  // Good - simple and clear
  /// <summary>
  /// Gets the singleton mod instance.
  /// </summary>
  public static YaOptMod Instance { get; }
  
  // Good - complex behavior warrants remarks
  /// <summary>
  /// Checks if thread-safe code paths are required.
  /// </summary>
  /// <remarks>
  /// Returns true when parallel pawn tick or job giver optimizations are enabled.
  /// </remarks>
  public static bool NeedThreadSafe => ...;
  ```

## 4. Build & Test
- **Build Config**: Version parameters are in `Source/Settings.props` (shared) and `Source/UserSettings.props` (local, gitignored; copy from `.template`).
- **Build**: Open `Source/YaOpt.sln` with Visual Studio to compile.
- **Output**:
  - Main DLL: `1.6/Assemblies/YaOpt.dll`
  - OtherMod DLLs: `1.6/Mods/<ModName>/Assemblies/`
  - Burst native libraries: `1.6/Burst/yaopt_burst_*.dll`
- **Conditional Compilation**: `DEBUG` symbol enables performance diagnostics and profiling output.
- **Test**: Launch RimWorld with the mod loaded. Verify via in-game TPS display and log messages.
