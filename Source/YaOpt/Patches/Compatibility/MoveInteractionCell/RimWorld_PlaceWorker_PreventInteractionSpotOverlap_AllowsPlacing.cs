using HarmonyLib;
using RimWorld;

namespace YaOpt.Patches.Compatibility.MoveInteractionCell
{
	[HarmonyPatch(typeof(PlaceWorker_PreventInteractionSpotOverlap))]
	[HarmonyPatch(nameof(PlaceWorker_PreventInteractionSpotOverlap.AllowsPlacing))]
	internal static class RimWorld_PlaceWorker_PreventInteractionSpotOverlap_AllowsPlacing
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe &&
				   YaOptGlobal.HasType("MoveInteractionCell.ThingGrid_ThingsListAtFast");
		}

		public static void Prefix()
		{
			MoveInteractionCell_ThingGrid_ThingsListAtFast_Postfix.InterceptThingListFast = true;
		}

		public static void Postfix()
		{
			MoveInteractionCell_ThingGrid_ThingsListAtFast_Postfix.InterceptThingListFast = false;
		}
	}
}