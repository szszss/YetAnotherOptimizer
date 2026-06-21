using HarmonyLib;
using RimWorld;

namespace YaOpt.Patches.Compatibility.MoveInteractionCell
{
	[HarmonyPatch(typeof(GenConstruct))]
	[HarmonyPatch(nameof(GenConstruct.CanPlaceBlueprintAt))]
	internal static class RimWorld_GenConstruct_CanPlaceBlueprintAt
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe &&
				   YaOptGlobal.HasType("MoveInteractionCell.GridsUtility_GetThingList");
		}

		public static void Prefix()
		{
			MoveInteractionCell_GridsUtility_GetThingList_Postfix.InterceptThingList = true;
		}

		public static void Postfix()
		{
			MoveInteractionCell_GridsUtility_GetThingList_Postfix.InterceptThingList = false;
		}
	}
}