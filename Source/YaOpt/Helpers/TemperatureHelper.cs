using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Verse;

namespace YaOpt.Helpers
{
	internal static class TemperatureHelper
	{
		private struct CacheEntry
		{
			public int Tick;
			public FloatRange Range;
		}

		private static readonly ConcurrentDictionary<Pawn, CacheEntry> cachedSafeTemperatureRanges = 
			new ConcurrentDictionary<Pawn, CacheEntry>();

		private static int currentTick;

		static TemperatureHelper()
		{
			UpdateCallbackHelper.RegisterPreTickCallback(ClearCache);
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
		}

		private static void ClearCache(int tick)
		{
			currentTick = tick;
		}

		private static void ClearCache()
		{
			cachedSafeTemperatureRanges.Clear();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FloatRange GetSafeTemperatureRange(Pawn pawn)
		{
			if (!cachedSafeTemperatureRanges.TryGetValue(pawn, out var entry) || entry.Tick != currentTick)
			{
				entry = new CacheEntry()
				{
					Range = pawn.SafeTemperatureRange(),
					Tick = currentTick
				};
				cachedSafeTemperatureRanges.TryAdd(pawn, entry);
			}
			return entry.Range;
		}
	}
}