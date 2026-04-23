# YetAnotherOptimizer Development Guide

This project is a high-performance optimization mod for **RimWorld**, designed to significantly increase game frame rates (TPS/FPS) through Harmony patches, multi-threading parallelization, cache optimization, Unity Burst compilation, and low-level assembly hooks.

## 1. Project Architecture

The project consists of four core modules, each handling optimization at a different level:

### 1.1 `Source/YaOpt` (Core Module)
- **Purpose**: Contains the main logic of the mod, Harmony patches, and general helper tools.
- **Key Components**:
  - `Patches/`: Harmony patches organized by target namespace.
  - `Helpers/`: Contains `ParallelTickManager` (parallel Tick management), `MaterialColorCache` (rendering cache), etc.
  - `YaOptMod.cs`: Handles initialization, settings loading, early patching.
  - `YaOptGlobal.cs`: Provides centralized access to mod state, settings, platform capabilities.
  - `YaOptSettings.cs`: Mod settings management.

### 1.2 `Source/YaOpt.Unity` (Burst Module)
- **Purpose**: Utilizes Unity's **Burst Compiler** for high-performance mathematical operations and data processing.
- **Features**:
  - For reference only. It is not a complete C# project. It needs to be imported into a Unity project for compilation.
  - Uses `Unity.Mathematics` (`float4`, `float3`) instead of standard `Vector3` to leverage SIMD instructions.
  - Uses `[BurstCompile]` to mark critical paths.
  - Strictly prohibits referencing any managed objects (reference types); only uses `NativeArray` and unmanaged structures (`struct`).

### 1.3 `Source/YaOpt.Prepatch` (Prepatch Module)
- **Purpose**: Handle Prepatch that must be applied before the game starts.
- **Functions**:
  - Uses `Prepatcher` instead of Harmony because of some limitations (e.g., hooking generic methods).
  - Uses `FreePatch` to modify method.
  - Uses `Mono.Cecil.Cil` to emit IL code.

### 1.4 `Source/YaOpt.OtherMod.*` (Compatibility Module)
- **Purpose**: Compatibility patches for specific popular mods (e.g., `FacialAnimation`, `HumanoidAlienRaces`).
- **Principles**: Must reference via reflection or soft dependencies to avoid hard crashes if the target mod is missing.

## 2. Directory Structure

```
Root
├── 1.6/                    # Release directory
│   ├── Assemblies/         # Compiled managed DLLs
│   └── Burst/              # Compiled Burst native libraries
├── Common/
│   ├── Defs/               # Definition files
│   └── Languages/          # Language files
├── Source/
│   ├── YaOpt/              # Core C# project
│   ├── YaOpt.Unity/        # Unity Burst project
│   ├── YaOpt.Prepatch/     # Prepatch project
│   └── YaOpt.OtherMod.*/   # Compatibility projects
├── About/                  # Mod metadata
└── LoadFolders.xml         # Loading logic
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
  - **NO LINQ IN HOT CODE** (LINQ in the initialization code are acceptable.). 
  - **Avoid Memory Allocations** (new Class(), lambda closures, params object[]).
- **Inlining**: Use `[MethodImpl(MethodImplOptions.AggressiveInlining)]` for small, frequently called methods.
- **Structs**: Strictly use `struct` in `YaOpt.Unity` and pass by `ref` to reduce copying.

### 3.3 Harmony Patch Standards
- **Transpiler Priority**: Prefer using `Transpiler` to modify IL instructions to avoid the overhead of Prefix/Postfix.
- **Defensive Programming**: When searching for instructions in a Transpiler, consider that other mods may have already modified the code.
- **Comments**: Complex IL operations must include comments explaining the intent (*Why*), not just the operation (*What*).

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
- **Build**: Open `Source/YaOpt.sln` with Visual Studio to compile.
- **Output**: Compilation results are automatically output to `1.6/Assemblies`.
- **Test**: Launch RimWorld with the mod loaded.
