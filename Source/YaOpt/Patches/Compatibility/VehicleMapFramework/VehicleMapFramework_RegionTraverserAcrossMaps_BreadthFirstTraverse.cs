using HarmonyLib;
using System.Reflection;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.Compatibility.VehicleMapFramework
{
	[HarmonyPatch]
	internal static class VehicleMapFramework_RegionTraverserAcrossMaps_BreadthFirstTraverse
	{
		static MethodBase TargetMethod()
		{
			return AccessTools.Method(
				AccessTools.TypeByName("VehicleMapFramework.RegionTraverserAcrossMaps"),
				"BreadthFirstTraverse",
				new[] { typeof(Region), typeof(RegionEntryPredicate), typeof(RegionProcessor), typeof(int), typeof(RegionType) });
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe &&
				   YaOptGlobal.HasMod("oels.vehiclemapframework");
		}

		static bool Prefix(Region root, RegionEntryPredicate entryCondition, RegionProcessor regionProcessor, int maxRegions, RegionType traversableRegionTypes)
		{
			if (YaOptGlobal.IsInMainThread)
				return true;
			ParallelRegionTraverser.BreadthFirstTraverse(root, entryCondition, regionProcessor, maxRegions, traversableRegionTypes);
			return false;
		}
	}
}