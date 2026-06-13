using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Threading;
using Verse;
using Verse.AI;

namespace YaOpt.Helpers
{
	internal class RCellFinderCache
	{
		private delegate bool IsGoodDestinationForDelegate(IntVec3 c, Pawn pawn, bool careAboutDanger);

		private static readonly IsGoodDestinationForDelegate IsGoodDestinationFor;

		public static ThreadLocal<RCellFinderCache> _threadLocalCache =
			new ThreadLocal<RCellFinderCache>(() => new RCellFinderCache());

		static RCellFinderCache()
		{
			IsGoodDestinationFor = AccessTools.MethodDelegate<IsGoodDestinationForDelegate>(
				AccessTools.Method(typeof(RCellFinder), "IsGoodDestinationFor"), null, false, null);
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
		}

		private struct Result
		{
			public bool GoodDestinationHasSet;
			public bool GoodDestinationResult;
			public bool CanReachHasSet;
			public bool CanReachResult;
		}

		private Pawn _cachedToucher;

		private int _cachedTick = -1;

		private readonly Dictionary<IntVec3, Result> _resultCache = new Dictionary<IntVec3, Result>(32);

		private void ClearIfNeeded(Pawn toucher)
		{
			int tick = Find.TickManager.TicksGame;
			if (_cachedToucher != toucher || _cachedTick != tick)
			{
				_resultCache.Clear();
				_cachedToucher = toucher;
				_cachedTick = tick;
			}
		}

		internal static RCellFinderCache GetCache(Pawn toucher)
		{
			var cache = _threadLocalCache.Value;
			cache.ClearIfNeeded(toucher);
			return cache;
		}

		internal static bool CachedIsGoodDestinationFor(IntVec3 c, Pawn pawn, bool careAboutDanger,
			RCellFinderCache cache)
		{
			if (cache._resultCache.TryGetValue(c, out var value))
			{
				if (value.GoodDestinationHasSet)
				{
					return value.GoodDestinationResult;
				}
			}
			else
			{
				value = new Result();
			}
			value.GoodDestinationHasSet = true;
			value.GoodDestinationResult = IsGoodDestinationFor(c, pawn, careAboutDanger);
			cache._resultCache[c] = value;
			return value.GoodDestinationResult;
		}

		internal static bool CachedCanReach(Pawn toucher, LocalTargetInfo target, PathEndMode peMode,
			Danger maxDanger, bool canBashDoors, bool canBashFences, TraverseMode mode,
			RCellFinderCache cache)
		{
			IntVec3 cell = target.Cell;
			if (cache._resultCache.TryGetValue(cell, out var value))
			{
				if (value.CanReachHasSet)
				{
					return value.CanReachResult;
				}
			}
			else
			{
				value = new Result();
			}
			value.CanReachHasSet = true;
			value.CanReachResult = toucher.CanReach(target, peMode, maxDanger, canBashDoors, canBashFences, mode);
			cache._resultCache[cell] = value;
			return value.CanReachResult;
		}

		private static void ClearCache()
		{
			_threadLocalCache.Dispose();
			_threadLocalCache = new ThreadLocal<RCellFinderCache>(() => new RCellFinderCache());
		}
	}
}
