using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Verse;

namespace YaOpt.Helpers
{
	internal static class ParallelRegionTraverser
	{
		private static readonly ConcurrentBag<ParallelBFSWorker> pool = new ConcurrentBag<ParallelBFSWorker>();

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

		private class ParallelBFSWorker
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

			public void BreadthFirstTraverseWork(Region root, RegionEntryPredicate entryCondition, RegionProcessor regionProcessor, int maxRegions, RegionType traversableRegionTypes)
			{
				if ((root.type & traversableRegionTypes) == RegionType.None)
				{
					return;
				}
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
					for (var i = 0; i < region.links.Count; i++)
					{
						var regionLink = region.links[i];
						for (var j = 0; j < 2; j++)
						{
							var region2 = regionLink.regions[j];
							if (region2 != null && !close.Contains(region2.id) && 
							    (region2.type & traversableRegionTypes) != RegionType.None && 
							    (entryCondition == null || entryCondition(region, region2)))
							{
								this.QueueNewOpenRegion(region2);
							}
						}
					}
				}
				this.FinalizeSearch();
			}

			private Queue<Region> open = new Queue<Region>();

			private HashSet<int> close = new HashSet<int>();

			private int numRegionsProcessed;

			private const int skippableRegionSize = 4;
		}
	}
}