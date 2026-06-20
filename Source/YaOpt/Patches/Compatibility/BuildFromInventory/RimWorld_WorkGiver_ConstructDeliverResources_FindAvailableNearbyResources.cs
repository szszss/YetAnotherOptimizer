using HarmonyLib;
using RimWorld;
using Verse;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.Compatibility.BuildFromInventory
{
	[HarmonyPatch(typeof(WorkGiver_ConstructDeliverResources))]
	[HarmonyPatch("FindAvailableNearbyResources")]
	[HarmonyPriority(Priority.HigherThanNormal)]
	[HarmonyBefore("Uuugggg.rimworld.Build_From_Inventory.main")]
	internal static class RimWorld_WorkGiver_ConstructDeliverResources_FindAvailableNearbyResources
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe && YaOptGlobal.HasType("Build_From_Inventory.NothingNearbyDummy");
		}

		public static bool Prefix(Thing firstFoundResource, ref int resTotalAvailable)
		{
			if (firstFoundResource.Spawned)
			{
				return true;
			}
			var resourcesAvailable = ThreadLocalConstructDeliverResources.ResourcesAvailable.Value;
			resourcesAvailable.Clear();
			resourcesAvailable.Add(firstFoundResource);
			resTotalAvailable = firstFoundResource.stackCount;
			return false;
		}
	}
}