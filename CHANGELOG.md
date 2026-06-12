* Feature: Catch Stack Overflow Crash. Log stack overflow exceptions that cannot be caught.
* Compatibility: RepairableGear
* Fix: Thread-safe for GenLabel (Some mods use it multi-threaded)
* Fix: Pawn_RelationsTracker.RelatedPawns may fail in some situations.
* Fix: If you disable all multi-threading optimizations during a game and load a save file when Performance Fish is installed, the save file will fail to load due to map initialization failure.
* Refactor: Only the main thread can initiate parallel thought updates to avoid potential Unity Jobs System failures.

## 1.0.24  (2026-05-25)

* Compatibility: Vanilla Gravship Expanded - Chapter 1
* Fix: The false alarm that Elite Bionics Framework will detect itself violating the EBF protocol.
* Fix: Thread-safe for Pawn_RelationsTracker.FamilyByBlood_Internal.
* Fix: Parallel Post Map Tick attempts to duplicated destroy things, causing an error.
* Fix: PawnsFinder.AllMaps and AllMaps_Spawned fail when Vehicle Map Framework is installed.

## 1.0.23  (2026-05-18)

* Compatibility: Pick Up And Haul compatibility patch will recognize the unofficial fork (package id: mehni.pickupandhaul.unoffical), though this version is obsolete.
* Fix: Fast Patch Operation may behave differently from the original operation in rare, specific cases.
* Fix: CellFinder.TryFindRandomReachableCellNearPosition may not work.

## 1.0.22  (2026-05-17)

* Compatibility: BondageFurniture
* Refactor: Rewrite the XML compatibility patch loading method to provide better compatibility.

## 1.0.21  (2026-05-17)

* Compatibility: Make CostListCalculator.CostListAdjusted reentrant. (For compatibility with Nivarian Race)
* Fix: Constant job prediction may incorrectly dispose of the pawn's current job.

## 1.0.20  (2026-05-16)

* Compatibility: Vehicle Map Framework (Under testing. Partial compatibility; Parallel Work Giver will be disabled)
* Fix: Thread-safe for SituationalThoughtHandler.CheckRecalculateSocialThoughts.
* Fix: Thread-safe for RegionGrid.
* Refactor: Simplify patch injection points for Early Render Preparation, improving compatibility.

## 1.0.19  (2026-05-13)

* Compatibility: While You're Up - Performance Fix Fork
* Fix: Thread-safe for TendUtility, Medicine, and BuildableDef.PlaceWorkers.
* Refactor: Replace the PooledList of ThreadLocalHelper with TransientPool.

## 1.0.18  (2026-05-12)

* Compatibility: Job Failure Fixes (Under testing)
* Compatibility: Smarter Construction (Under testing)
* Fix: Thread-safe for Pawn.GetDisabledWorkTypes.
* Refactor: Replace the lock of GenConstruct.CanConstruct with thread local.
* Refactor: Optimize TransientPool performance.

## 1.0.17  (2026-05-11)

* Compatibility: Research Reinvented
* Fix: Thread-safe for Room.ContainedThings.
* Fix: Thread-safe for Room.Owners when Performance Fish is installed.
* Fix: Fix Memory Leak clears the cache too frequently.
* Refactor: Introduce TransientPool to solve the thread safety issue of Room.ContainedAndAdjacentThings.

## 1.0.16  (2026-05-10)

* Compatibility: DtrndG's Inverted Rack
* Fix: ListerThingsIndexer fails after an apocriton resurrects mechanoid.
* Refactor: Relax the thread safety checks for MapPawns.
* Refactor: When ListerThingsIndexer encounters an index mismatch error, it will stop indexing that type and issue a warning, instead of continuously reporting errors.

## 1.0.15  (2026-05-09)

* Compatibility: Thread-safe patch for Qing's More Traits 2.
* Compatibility: Thread-safe patch for DigitalStorage.
* Compatibility: Thread-safe patch for MinifyEverything.
* Compatibility: Thread-safe patch for Vivi Race / RPE Framework.
* Fix: Pawns don't repair damaged buildings when Parallel Work Giver is enabled.

## 1.0.14  (2026-05-08)

* Compatibility: Thread-safe patch for ReGrowth 2.
* Fix: Exception during game launch when both YaOpt and Performance Fish are installed, but Performance Fish's TryFindBestIngredientsHelpers_InnerDelegate is disabled.
* Fix: System.InvalidOperationException when both Target Finding Optimization and Constant Job Prediction are enabled.
* Fix: Thread-safe for ListerBuildingsRepairable and MineAIUtility.
* Refactor: Introduce CacheWarmer to avoid potential System.InvalidOperationException when creating caches during gameplay.
* Other: Add compatibility note for Pawn Render Node Worker Cache Fix.

## 1.0.13  (2026-05-08)

* Fix: When Facial Animation is installed and Parallel Facial Animation Update is enabled, switching to the world map results in a loop of the error message "Try to add a pending pawn while the updating facial animation job is running".

## 1.0.12  (2026-05-07)

* Compatibility: Thread-safe patch for Build From Inventory - Continued.
* Fix: Graphic Texture Caching fails to load mask textures with custom path.
* Refactor: Introduce ReservationPromiser to improve compatibility of Parallel Work Giver.

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