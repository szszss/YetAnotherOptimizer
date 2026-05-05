using HarmonyLib;
using Verse;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.Reach
{
	[HarmonyPatch(typeof(Reachability))]
	[HarmonyPatch("QueueNewOpenRegion")]
	internal static class Verse_Reachability_QueueNewOpenRegion
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelWorkGiver.Enabled;
		}

		static bool Prefix(Region region)
		{
			if (YaOptGlobal.IsInMainThread)
				return true;

			if (region == null)
			{
				Log.ErrorOnce("Tried to queue null region.", 881121);
				return false;
			}
			var tlr = ThreadLocalReachability.Reachabilities.Value;
			if (!tlr.ReachedRegions.Add(region.id))
			{
				Log.ErrorOnce("Region is already reached; you can't open it. Region: " + region.ToString(), 719991);
				return false;
			}
			tlr.OpenQueue.Enqueue(region);
			return false;
		}
	}
}