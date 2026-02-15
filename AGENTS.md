# YetAnotherOptimizer Development Guide

This project is a high-performance optimization mod for **RimWorld**, designed to significantly increase game frame rates (TPS/FPS) through Harmony patches, multi-threading parallelization, cache optimization, Unity Burst compilation, and low-level assembly hooks.

## 1. Project Architecture

The project consists of four core modules, each handling optimization at a different level:

### 1.1 `Source/YaOpt` (Core Module)
- **Purpose**: Contains the main logic of the mod, Harmony patches, and general helper tools.
- **Key Components**:
  - `Patches/`: Harmony patches organized by target namespace (Prefix, Postfix, Transpiler).
  - `Helpers/`: Contains `ParallelTickManager` (parallel Tick management), `MaterialColorCache` (rendering cache), etc.
  - `YaOptSettings.cs`: Mod settings management.

### 1.2 `Source/YaOpt.Unity` (Burst Module)
- **Purpose**: Utilizes Unity's **Burst Compiler** and **Job System** for high-performance mathematical operations and data processing.
- **Features**:
  - For reference only. It is not a complete C# project. It needs to be imported into a Unity project for compilation.
  - Uses `Unity.Mathematics` (`float4`, `float3`) instead of standard `Vector3` to leverage SIMD instructions.
  - Uses `[BurstCompile]` to mark critical paths.
  - Strictly prohibits referencing any managed objects (reference types); only uses `NativeArray` and unmanaged structures (`struct`).

### 1.3 `Source/YaOpt.Win64` (Native Module)
- **Purpose**: Handles x64 native interop and low-level memory modification.
- **Functions**:
  - Implements `ITrampolineFactory` to bypass Harmony limitations (e.g., hooking generic methods) by manually writing x64 machine code (`MOV RAX, ... JMP RAX`).
  - Uses `VirtualProtect` to modify memory permissions.
  - Makes extensive use of `unsafe` pointer operations.

### 1.4 `Source/YaOpt.OtherMod.*` (Compatibility Module)
- **Purpose**: Compatibility patches for specific popular mods (e.g., `FacialAnimation`, `HumanoidAlienRaces`).
- **Principles**: Must reference via reflection or soft dependencies to avoid hard crashes if the target mod is missing.

## 2. Directory Structure

```
Root
├── 1.6/                    # Release directory
│   ├── Assemblies/         # Compiled managed DLLs
│   └── Burst/              # Compiled Burst native libraries
├── Source/
│   ├── YaOpt/              # Core C# project
│   ├── YaOpt.Unity/        # Unity Burst project
│   ├── YaOpt.Win64/        # Win64 Interop project
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

### 3.2 Performance Guidelines
Since this is an optimization mod, performance is the highest priority:
- **Hot Paths**: In `Tick`, `Draw`, `Update` loops:
  - **Strictly NO LINQ**.
  - **Avoid Memory Allocations** (new Class(), lambda closures, params object[]).
- **Inlining**: Use `[MethodImpl(MethodImplOptions.AggressiveInlining)]` for small, frequently called methods.
- **Structs**: Strictly use `struct` in `YaOpt.Unity` and pass by `ref` to reduce copying.

### 3.3 Harmony Patch Standards
- **Transpiler Priority**: Prefer using `Transpiler` to modify IL instructions to avoid the overhead of Prefix/Postfix.
- **Defensive Programming**: When searching for instructions in a Transpiler, consider that other mods may have already modified the code.
- **Comments**: Complex IL operations must include comments explaining the intent (*Why*), not just the operation (*What*).

### 3.4 Unsafe Code
- `unsafe` code blocks and pointer operations are allowed in performance-critical areas but must ensure boundary checks and memory safety.
- In `YaOpt.Win64`, exercise extreme caution when directly manipulating memory addresses to ensure alignment and read/write permissions.

## 4. Build & Test
- **Build**: Open `Source/YaOpt.sln` with Visual Studio to compile.
- **Output**: Compilation results are automatically output to `1.6/Assemblies`.
- **Test**: Launch RimWorld with the mod loaded.
