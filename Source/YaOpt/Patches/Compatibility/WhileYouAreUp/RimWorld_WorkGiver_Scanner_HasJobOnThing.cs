using HarmonyLib;
using RimWorld;
using Verse;

namespace YaOpt.Patches.Compatibility.WhileYouAreUp
{
	[HarmonyPatch(typeof(WorkGiver_Scanner))]
	[HarmonyPatch(nameof(WorkGiver_Scanner.HasJobOnThing))]
	internal static class RimWorld_WorkGiver_Scanner_HasJobOnThing
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelWorkGiver.Enabled &&
				   YaOptGlobal.HasType("PickUpAndHaul.WorkGiver_HaulToInventory") &&
				   YaOptGlobal.HasType("WhileYoureUp.Mod");
		}

		static void Postfix(Pawn pawn)
		{
			if (YaOptGlobal.IsInMainThread)
				WhileYouAreUpAccess.ClearTempDetour(pawn);
		}
	}
}