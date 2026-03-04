using System;
using HarmonyLib;
using RimWorld;
using System.Threading;
using Verse;

namespace YaOpt.Patches.Compatibility.WhileYouAreUp
{
	[HarmonyPatch(typeof(WorkGiver_Haul))]
	[HarmonyPatch(nameof(WorkGiver_Haul.JobOnThing))]
	internal static class RimWorld_WorkGiver_Haul_JobOnThing
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelJobGiver.Enabled && YaOptGlobal.HasType("WhileYoureUp.Mod");
		}

		static void Postfix(Pawn pawn)
		{
			if (YaOptGlobal.IsInMainThread)
				WhileYouAreUpAccess.ClearTempDetour(pawn);
		}
	}
}