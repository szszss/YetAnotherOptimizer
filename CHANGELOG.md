## 1.0.11  (2026-05-07)

* Fix: Thread-safe for HaulAIUtility.
* Compatibility: Thread-safe patch for Zoology: Realistic Animal Overhaul.

## 1.0.10  (2026-05-05)

* Fix: Errors related to DynamicDrawManager.DrawDynamicThings in Early Render Preparation and Plant Sway Optimization.
* Refactor: Simplify the modifications made to TickList by Parallel Pawn Tick, providing better compatibility.

## 1.0.9  (2026-05-05)

* Compatibility: Make Room.Role reentrant.
* Fix: Thread-safe for PawnsFinder.

## 1.0.8  (2026-05-05)

* Fix: Graphic Texture Caching may fail to load north-facing textures.

## 1.0.7  (2026-05-05)

* Fix: TryFindRandomReachableNearbyCell fails if any multithreading options are enabled.
* Fix: Thread-safe for ListerBuildings.

## 1.0.6  (2026-05-04)

* Fix: Thread-safe for LovePartnerRelationUtility, Pawn_RelationsTracker and ThoughtHandler.
* Fix: When ParallelThoughtUpdater encounters an error, it clears the work cache before the worker threads stop, that may cause more errors to occur.

## 1.0.5  (2026-05-04)

* Fix: Thread-safe for SpouseRelationUtility and TraitSet.

## 1.0.4  (2026-05-04)

* Fix: System.ArgumentNullException when retrieving IThingHolders from a ThingWithComps whose compsByType is null.
* Fix: Lazy Loading doesn't work when loading textures in bulk using GetAllInFolder.
* Fix: Lazy Loading may failed to load png textures.

## 1.0.3  (2026-05-03)

* Fix: CheckCellBasedReachability fails in non-main thread, which could cause roads cannot be generated during map generation.
* Fix: Crashes when lazy loading textures whose size is not a multiple of 4.
* Fix: Errors related to ListerThings.
* Refactor: ListerThingsIndexer create and destroy.

## 1.0.2  (2026-05-03)

* Fix: LoadTextureDds may uploads more data than required, which could cause crashes in some environments.

## 1.0.1  (2026-05-03)

* Fix: System.InvalidOperationException when a thread uses two Room.Regions simultaneously.

## 1.0.0 (2026-05-02)

* Initial version