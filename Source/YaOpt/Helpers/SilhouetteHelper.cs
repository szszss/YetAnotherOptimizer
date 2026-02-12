using RimWorld;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace YaOpt.Helpers
{
	internal static class SilhouetteHelper
	{
		public readonly struct YaSilhouetteCacheKey : IEquatable<YaSilhouetteCacheKey>
		{
			public readonly ThingDef thingDef;

			public readonly LifeStageDef lifeStageDef;

			private readonly int hashcode;

			public readonly int graphicIndex;

			public readonly Gender gender;

			public readonly RotStage rotMode;

			public YaSilhouetteCacheKey(Pawn pawn)
			{
				thingDef = pawn.def;
				lifeStageDef = pawn.ageTracker.CurLifeStage;
				hashcode = 0;
				graphicIndex = pawn.GetGraphicIndex();
				gender = pawn.gender;
				var mutant = pawn.mutant;
				rotMode = ((mutant != null) ? mutant.rotStage : RotStage.Fresh);
				hashcode = CacheHashCode();
			}

			public YaSilhouetteCacheKey(Thing thing)
			{
				thingDef = thing.def;
				lifeStageDef = null;
				hashcode = 0;
				graphicIndex = thing.OverrideGraphicIndex ?? (-1);
				gender = Gender.None;
				rotMode = RotStage.Fresh;
				hashcode = CacheHashCode();
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private int CacheHashCode()
			{
				var num = thingDef.GetHashCode();
				if (lifeStageDef != null)
				{
					num = Gen.HashCombineInt(num, lifeStageDef.GetHashCode());
				}
				num = Gen.HashCombineInt(num, graphicIndex);
				num = Gen.HashCombineInt(num, gender.GetHashCode());
				return Gen.HashCombineInt(num, rotMode.GetHashCode());
			}

			public override int GetHashCode()
			{
				return hashcode;
			}

			public bool Equals(YaSilhouetteCacheKey other)
			{
				return hashcode == other.hashcode &&
					   Equals(thingDef, other.thingDef) &&
					   Equals(lifeStageDef, other.lifeStageDef) &&
					   graphicIndex == other.graphicIndex &&
					   gender == other.gender &&
					   rotMode == other.rotMode;
			}

			public override bool Equals(object obj)
			{
				return obj is YaSilhouetteCacheKey other && Equals(other);
			}
		}

		public class YaSilhouetteCacheKeyComparer : IEqualityComparer<YaSilhouetteCacheKey>
		{
			public bool Equals(YaSilhouetteCacheKey x, YaSilhouetteCacheKey y)
			{
				return x.Equals(y);
			}

			public int GetHashCode(YaSilhouetteCacheKey obj)
			{
				return obj.GetHashCode();
			}
		}

		public class YaSilhouetteCacheValue //: IDisposable // Don't dispose. The materials are permanent.
		{
			public readonly Material east;

			public readonly Material west;

			internal int _lastUsedTick;

			public YaSilhouetteCacheValue(Material east, Material west)
			{
				this.east = east;
				this.west = west;
			}
		}

		/// <summary>
		/// Remove a silhouette cache if it hasn't been used for 12 hours (in-game time)
		/// </summary>
		private const int REMOVE_UNUSED_CACHE_AFTER = 30000;

		/// <summary>
		/// Check and clean cache per 10 seconds
		/// </summary>
		private const int CACHE_CLEAN_INTERVAL = 600;

		public static Dictionary<YaSilhouetteCacheKey, YaSilhouetteCacheValue> SilhouetteMaterialCache =
			new Dictionary<YaSilhouetteCacheKey, YaSilhouetteCacheValue>(512, new YaSilhouetteCacheKeyComparer());

		private static List<YaSilhouetteCacheKey> removingKeys = new List<YaSilhouetteCacheKey>();

		private static int currentTick;

		static SilhouetteHelper()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
			UpdateCallbackHelper.RegisterPreRenderCallback(PreRender);
			UpdateCallbackHelper.RegisterPostRenderCallback(PostRender);
		}

		private static void ClearCache()
		{
			SilhouetteMaterialCache.Clear();
		}

		private static void PreRender(int tick)
		{
			currentTick = tick;
		}

		public static void PostRender(int tick)
		{
			if (tick % CACHE_CLEAN_INTERVAL == 0)
			{
				foreach (var pair in SilhouetteMaterialCache)
				{
					var time = currentTick - pair.Value._lastUsedTick;
					if (time > REMOVE_UNUSED_CACHE_AFTER)
					{
						removingKeys.Add(pair.Key);
					}
				}
				foreach (var key in removingKeys)
				{
					SilhouetteMaterialCache.Remove(key);
				}
				removingKeys.Clear();
			}
		}

		public static bool TryGetCache(in YaSilhouetteCacheKey key, out YaSilhouetteCacheValue cacheValue)
		{
			var result = SilhouetteMaterialCache.TryGetValue(key, out cacheValue);
			if (result)
				cacheValue._lastUsedTick = currentTick;
			return result;
		}

		public static YaSilhouetteCacheValue AddCache(in YaSilhouetteCacheKey key, Material east, Material west)
		{
			var cache = new YaSilhouetteCacheValue(east, west)
			{
				_lastUsedTick = currentTick
			};
			SilhouetteMaterialCache[key] = cache;
			return cache;
		}

		public static void RemoveCache(in YaSilhouetteCacheKey key)
		{
			SilhouetteMaterialCache.Remove(key);
		}

		public static YaSilhouetteCacheKey GetKey(Thing thing)
		{
			if (thing is Pawn pawn)
			{
				return new YaSilhouetteCacheKey(pawn);
			}
			else
			{
				return new YaSilhouetteCacheKey(thing);
			}
		}
	}
}