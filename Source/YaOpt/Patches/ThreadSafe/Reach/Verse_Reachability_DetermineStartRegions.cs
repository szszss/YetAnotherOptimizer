using HarmonyLib;
using Verse;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.Reach
{
	[HarmonyPatch(typeof(Reachability))]
	[HarmonyPatch("DetermineStartRegions")]
	internal static class Verse_Reachability_DetermineStartRegions
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelJobGiver.Enabled;
		}

		static bool Prefix(Map ___map, IntVec3 start)
		{
			if (UnityData.IsInMainThread)
				return true;

			var tlr = ThreadLocalReachability.Reachabilities.Value;
			tlr.StartingRegions.Clear();
			if (tlr.PathGrid.WalkableFast(start))
			{
				var validRegionAt = tlr.RegionGrid.GetValidRegionAt(start);
				ThreadLocalReachability.QueueNewOpenRegion(validRegionAt);
				tlr.StartingRegions.Add(validRegionAt);
				return false;
			}
			for (var i = 0; i < 8; i++)
			{
				var intVec = start + GenAdj.AdjacentCells[i];
				if (intVec.InBounds(___map) && tlr.PathGrid.WalkableFast(intVec))
				{
					var validRegionAt2 = tlr.RegionGrid.GetValidRegionAt(intVec);
					if (validRegionAt2 != null && !tlr.ReachedRegions.Contains(validRegionAt2.id))
					{
						ThreadLocalReachability.QueueNewOpenRegion(validRegionAt2);
						tlr.StartingRegions.Add(validRegionAt2);
					}
				}
			}
			return false;
		}
	}
}