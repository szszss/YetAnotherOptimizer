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

		static RegionDangerHelper()
		{
			UpdateCallbackHelper.RegisterPreTickCallback(ClearCache);
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
		}

		private static void ClearCache(int tick)
		{
			_currentTick = tick;
		}

		private static void ClearCache()
		{
			_cachedSafeTemperatureRanges.Clear();
		}

		/// <summary>
		/// Gets the safe temperature range for a pawn, cached per tick.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FloatRange GetSafeTemperatureRange(Pawn pawn, int cacheLifespan)
		{
			if (!_cachedSafeTemperatureRanges.TryGetValue(pawn, out var entry) ||
			    _currentTick - entry.CreatedTick >= cacheLifespan)
			{
				entry = new CacheEntry()
				{
					Range = pawn.SafeTemperatureRange(),
					CreatedTick = _currentTick
				};
				_cachedSafeTemperatureRanges.TryAdd(pawn, entry);
			}
			return entry.Range;
		}
	}
}