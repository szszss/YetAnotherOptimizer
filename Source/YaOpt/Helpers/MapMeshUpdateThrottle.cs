using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace YaOpt.Helpers
{
	[StaticConstructorOnStartup]
	internal static class MapMeshUpdateThrottle
	{
		private static readonly Dictionary<Map, CacheEntry> nextUpdateDict =
			new Dictionary<Map, CacheEntry>();

		private static readonly List<Map> removeList = new List<Map>();

		private static int currentTime;

		private class CacheEntry
		{
			public long LastUpdateTime;

			public readonly Dictionary<ulong, HashSet<IntVec2>> UpdatingSectionCoords =
				new Dictionary<ulong, HashSet<IntVec2>>();

			public bool HasAnyUpdate;
		}

		static MapMeshUpdateThrottle()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
			UpdateCallbackHelper.RegisterPreTickCallback(UpdateTime);
			UpdateCallbackHelper.RegisterPreRenderCallback(CheckUpdate);
		}

		private static void ClearCache()
		{
			nextUpdateDict.Clear();
			removeList.Clear();
		}

		private static void UpdateTime(int tick)
		{
			currentTime = Environment.TickCount;
		}

		public static void CheckUpdate(int _)
		{
			ParallelMapTickManager.FinishPostMapTick(_);
			currentTime = Environment.TickCount;
			if (nextUpdateDict.Count > 0)
			{
				var paused = Find.TickManager.Paused;
				var interval = YaOptGlobal.Settings.MapMeshUpdateInterval;
				foreach (var (map, cache) in nextUpdateDict)
				{
					if (map.Disposed)
					{
						removeList.Add(map);
						continue;
					}

					if (cache.HasAnyUpdate &&
						(currentTime - cache.LastUpdateTime >= interval || paused))
					{
						foreach (var (mapMeshFlagDef, hashSet) in cache.UpdatingSectionCoords)
						{
							if (hashSet.Count > 0)
							{
								foreach (var section in hashSet)
								{
									map.mapDrawer.MapMeshDirty(
										SectionToLocation(section), mapMeshFlagDef, true, false);
								}
								hashSet.Clear();
							}
						}
						cache.LastUpdateTime = currentTime;
						cache.HasAnyUpdate = false;

					}
				}

				if (removeList.Count > 0)
				{
					foreach (var map in removeList)
					{
						nextUpdateDict.Remove(map);
					}
				}
			}
		}

		public static void MarkMapDirty(Map map, IntVec3 loc, ulong mapMeshFlagDef, bool _1, bool _2)
		{
			if (!nextUpdateDict.TryGetValue(map, out var cache))
			{
				cache = new CacheEntry();
				nextUpdateDict[map] = cache;
			}
			cache.HasAnyUpdate = true;
			if (!cache.UpdatingSectionCoords.TryGetValue(mapMeshFlagDef, out var set))
			{
				set = new HashSet<IntVec2>();
				cache.UpdatingSectionCoords[mapMeshFlagDef] = set;
			}
			var section = LocationToSection(loc);
			set.Add(section);
		}

		[SuppressMessage("ReSharper", "PossibleLossOfFraction")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static IntVec2 LocationToSection(in IntVec3 loc)
		{
			return new IntVec2(Mathf.FloorToInt(loc.x / 17), Mathf.FloorToInt(loc.z / 17));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static IntVec3 SectionToLocation(in IntVec2 section)
		{
			return new IntVec3(section.x * 17 + 8, 0, section.z * 17 + 8);
		}
	}
}