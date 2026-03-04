using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Verse;

namespace YaOpt.Helpers
{
	/// <summary>
	/// Replace vanilla safe temperature cache with a thread safe one.
	/// </summary>
	/// <seealso cref="YaOpt.Patches.ThreadSafe.Locked.Verse_Region_DangerFor"/>
	internal static class RegionDangerHelper
	{
		private struct CacheEntry
		{
			public int CreatedTick;
			public FloatRange Range;
		}

		private static readonly ConcurrentDictionary<Pawn, CacheEntry> _cachedSafeTemperatureRanges =
			new ConcurrentDictionary<Pawn, CacheEntry>();

		private static int _currentTick;

		private static int _cachedCacheLifespan = 1;

		private static bool _isCacheLifespanCached;

		static RegionDangerHelper()
		{
			UpdateCallbackHelper.RegisterPreTickCallback(ClearCache);
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
		}

		private static void ClearCache(int tick)
		{
			_currentTick = tick;
			if (!_isCacheLifespanCached)
			{
				_isCacheLifespanCached = true;
				_cachedCacheLifespan = YaOptGlobal.Settings.OptStatCache.Enabled ? 20 : 1;
			}
			if (_currentTick % 18000 == 1)
			{
				_cachedSafeTemperatureRanges.Clear();
			}
		}

		private static void ClearCache()
		{
			_cachedSafeTemperatureRanges.Clear();
			_isCacheLifespanCached = false;
		}

		/// <summary>
		/// Gets the safe temperature range for a pawn, cached per tick.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FloatRange GetSafeTemperatureRange(Pawn pawn)
		{
			if (!_cachedSafeTemperatureRanges.TryGetValue(pawn, out var entry) ||
				_currentTick - entry.CreatedTick >= _cachedCacheLifespan)
			{
				entry = new CacheEntry()
				{
					Range = pawn.SafeTemperatureRange(),
					CreatedTick = _currentTick
				};
				_cachedSafeTemperatureRanges[pawn] = entry;
			}
			return entry.Range;
		}
	}
}