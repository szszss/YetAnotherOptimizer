using Gilzoide.ManagedJobs;
using System.Collections.Generic;
using Unity.Jobs;
using Verse;

namespace YaOpt.Helpers
{
	internal static class ConnectivityCellCache
	{
		public static HashSet<IntVec3> CurrentSet = null;
		private static readonly Dictionary<Map, HashSet<IntVec3>> perMapCellSet = new Dictionary<Map, HashSet<IntVec3>>();
		private static readonly List<Map> removeList = new List<Map>();
		private static bool needClear;
		private static JobHandle jobHandle = default;

		static ConnectivityCellCache()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
		}

		private static void ClearCache()
		{
			CurrentSet = null;
			perMapCellSet.Clear();
			removeList.Clear();
			needClear = false;
			jobHandle = default;
		}

		public static void SetupCurrentSet(Map map)
		{
			if (!perMapCellSet.TryGetValue(map, out var set))
			{
				var mapSize = map.Size.x * map.Size.z;
				set = new HashSet<IntVec3>(mapSize);
				perMapCellSet[map] = set;
			}
			CurrentSet = set;
			needClear = true;
		}

		public static void EnsureCleared()
		{
			jobHandle.CompleteWithSpinWait();
		}

		public static void StartClearJob()
		{
			if (needClear)
			{
				jobHandle = new ManagedJob(new ClearJob()).Schedule();
				needClear = false;
			}
			else
			{
				jobHandle = default;
			}
		}

		private struct ClearJob : IJob
		{
			public void Execute()
			{
				foreach (var (map, set) in perMapCellSet)
				{
					if (map.Disposed)
					{
						removeList.Add(map);
					}
					else
					{
						set.Clear();
					}
				}
				foreach (var map in removeList)
				{
					perMapCellSet.Remove(map);
				}
			}
		}
	}
}