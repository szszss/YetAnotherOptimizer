using HarmonyLib;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Verse;
using Verse.AI;
using YaOpt.Helpers.ThreadSafe;

namespace YaOpt.Helpers.ThreadLocal
{
	internal class ThreadLocalReachability
	{
		static ThreadLocalReachability()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
			QueueNewOpenRegion =
				AccessTools.MethodDelegate<QueueNewOpenRegionDelegate>(
					AccessTools.Method(typeof(Reachability), "QueueNewOpenRegion"), null, false, null);
		}

		public delegate void QueueNewOpenRegionDelegate(Region region);

		public static readonly QueueNewOpenRegionDelegate QueueNewOpenRegion;

		public static ThreadLocal<ThreadLocalReachability> Reachabilities =
			new ThreadLocal<ThreadLocalReachability>(() => new ThreadLocalReachability());

		private static GreedySpinLock _spinLock = new GreedySpinLock();

		public Queue<Region> OpenQueue = new Queue<Region>();

		public List<Region> StartingRegions = new List<Region>();

		public List<Region> DestRegions = new List<Region>();

		public HashSet<int> ReachedRegions = new HashSet<int>();

		public PathGrid PathGrid;

		public RegionGrid RegionGrid;

		private static void ClearCache()
		{
			Reachabilities.Dispose();
			Reachabilities = new ThreadLocal<ThreadLocalReachability>(() => new ThreadLocalReachability());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ThreadLocalReachability Get()
		{
			return Reachabilities.Value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsRegionAlreadyReached(int regionReachId, int reachId,
			bool isMainThread, HashSet<int> reachedRegions, Region region)
		{
			return isMainThread ? regionReachId == reachId : reachedRegions.Contains(region.id);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void EnterLock(ref bool lockTaken)
		{
			lockTaken = true;
			_spinLock.Enter();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ExitLock(bool lockTaken)
		{
			if (lockTaken)
				_spinLock.Exit();
		}

		public void Clear()
		{
			OpenQueue.Clear();
			StartingRegions.Clear();
			DestRegions.Clear();
			ReachedRegions.Clear();
			PathGrid = null;
			RegionGrid = null;
		}

		public void Setup(Map map, in TraverseParms traverseParams)
		{
			OpenQueue.Clear();
			StartingRegions.Clear();
			DestRegions.Clear();
			ReachedRegions.Clear();
			PathGrid = map.pathing.For(traverseParams).pathGrid;
			RegionGrid = map.regionGrid;
		}
	}
}