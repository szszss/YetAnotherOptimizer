using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Verse;
using YaOpt.Patches.Compatibility.VehicleMapFramework;
using Region = Verse.Region;

namespace YaOpt.Helpers
{
	internal static class ParallelRegionTraverser
	{
		private static readonly ConcurrentBag<ParallelBFSWorker> pool = new ConcurrentBag<ParallelBFSWorker>();

		internal static bool HasVehicleMapFramework = false;

		static ParallelRegionTraverser()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
		}

		private static void ClearCache()
		{
			while (pool.TryTake(out _))
			{
			}
		}

		public static void BreadthFirstTraverse(Region root, RegionEntryPredicate entryCondition,
			RegionProcessor regionProcessor, int maxRegions = 999999,
			RegionType traversableRegionTypes = RegionType.Set_Passable)
		{
			if (root == null)
			{
				Log.Error("ParallelBFS: BreadthFirstTraverse with null root region.");
				return;
			}

			if (!pool.TryTake(out var worker))
				worker = new ParallelBFSWorker();

			try
			{
				worker.BreadthFirstTraverseWork(root, entryCondition, regionProcessor, maxRegions, traversableRegionTypes);
			}
			catch (Exception ex)
			{
				Log.Error($"ParallelBFS: Exception in BreadthFirstTraverse: {ex}");
			}
			finally
			{
				worker.Clear();
				pool.Add(worker);
			}
		}

		internal class ParallelBFSWorker
		{
			public void Clear()
			{
				open.Clear();
				close.Clear();
			}

			private void QueueNewOpenRegion(Region region)
			{
				if (!close.Add(region.id))
				{
					throw new InvalidOperationException(
						$"ParallelBFS: Region is already closed; you can't open it. Region: {region}");
				}
				open.Enqueue(region);
			}

			private void FinalizeSearch()
			{
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private static bool ValidateRegion(Region from, Region to, HashSet<int> closeSet,
				RegionEntryPredicate entryCondition, RegionType traversableRegionTypes)
			{
				return to != null && !closeSet.Contains(to.id) &&
					   (to.type & traversableRegionTypes) != RegionType.None &&
					   (entryCondition == null || entryCondition(from, to));
			}

			public void BreadthFirstTraverseWork(Region root, RegionEntryPredicate entryCondition, RegionProcessor regionProcessor, int maxRegions, RegionType traversableRegionTypes)
			{
				if ((root.type & traversableRegionTypes) == RegionType.None)
				{
					return;
				}
				var vmf = HasVehicleMapFramework;
				Clear();
				numRegionsProcessed = 0;
				QueueNewOpenRegion(root);
				while (open.Count > 0)
				{
					var region = open.Dequeue();
					if (DebugViewSettings.drawRegionTraversal)
					{
						region.Debug_Notify_Traversed();
					}
					if (regionProcessor != null && regionProcessor(region))
					{
						FinalizeSearch();
						return;
					}
					if (RegionTraverser.ShouldCountRegion(region))
					{
						numRegionsProcessed++;
					}
					if (numRegionsProcessed >= maxRegions)
					{
						FinalizeSearch();
						return;
					}

					if (vmf)
					{
						VMFPrefix(region, entryCondition, traversableRegionTypes);
					}

					for (var i = 0; i < region.links.Count; i++)
					{
						var regionLink = region.links[i];
						for (var j = 0; j < 2; j++)
						{
							var region2 = regionLink.regions[j];
							if (ValidateRegion(region, region2, close, entryCondition, traversableRegionTypes))
							{
								this.QueueNewOpenRegion(region2);
							}
						}
					}

					if (vmf)
					{
						VMFPostFix(region, entryCondition, traversableRegionTypes);
					}
				}
				this.FinalizeSearch();
			}

			#region VehicleMapFramework

			internal void VMFPrefix(Region region, RegionEntryPredicate entryCondition, RegionType traversableRegionTypes)
			{
				foreach (var item in region.ListerThings.ThingsInGroup(ThingRequestGroup.Pawn))
				{
					if (item is VehicleMapFrameworkCompatibility.VehiclePawnWithMapStub item2)
					{
						var region2 = item2.VehicleMap.regionGrid.AllRegions_NoRebuild_InvalidAllowed.FirstOrDefault(r =>
							ValidateRegion(region, r, close, entryCondition, traversableRegionTypes));
						if (region2 != null)
						{
							QueueNewOpenRegion(region2);
						}
					}
				}
			}

			internal void VMFPostFix(Region region, RegionEntryPredicate entryCondition, RegionType traversableRegionTypes)
			{
				if (!VehicleMapFrameworkCompatibility.IsVehicleMapOfStub(
						region.Map, out var vehicle))
				{
					return;
				}

				if (vehicle.Spawned)
				{
					var regionTo = (vehicle).Position.GetRegion(vehicle.Map, traversableRegionTypes);
					if (ValidateRegion(region, regionTo, close, entryCondition, traversableRegionTypes))
					{
						QueueNewOpenRegion(regionTo);
						return;
					}
				}

				var ziplineDefs = VehicleMapFrameworkCompatibility.ZiplineDefsStub;
				foreach (var item2 in ziplineDefs.SelectMany(def => region.ListerThings.ThingsOfDef(def)))
				{
					var flag = !item2.TryGetComp(out VehicleMapFrameworkCompatibility.CompZiplineStub comp);
					if (!flag)
					{
						var pair = comp.Pair;
						flag = pair == null || !pair.Spawned;
					}
					if (!flag)
					{
						var pair2 = comp.Pair;
						var regionTo = pair2.Position.GetRegion(pair2.Map);
						if (ValidateRegion(region, regionTo, close, entryCondition, traversableRegionTypes))
						{
							QueueNewOpenRegion(regionTo);
						}
					}
				}
			}

			#endregion

			private Queue<Region> open = new Queue<Region>();

			private HashSet<int> close = new HashSet<int>();

			private int numRegionsProcessed;

			private const int skippableRegionSize = 4;
		}
	}
}