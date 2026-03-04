using System;
using HarmonyLib;
using System.Threading;
using Verse;
using YaOpt.Patches.Compatibility.WhileYouAreUp;

namespace YaOpt.Patches.Compatibility.PickUpAndHaul
{
	//[HarmonyPatch("PickUpAndHaul.WorkGiver_HaulToInventory", "JobOnThing")]
	//[HarmonyPatch(typeof(WorkGiver_Scanner))]
	//[HarmonyPatch(nameof(WorkGiver_Scanner.HasJobOnThing))]
	[Obsolete]
	internal static class PickUpAndHaul_WorkGiver_HaulToInventory_JobOnThing
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelJobGiver.Enabled &&
				   YaOptGlobal.HasType("PickUpAndHaul.WorkGiver_HaulToInventory") &&
				   YaOptGlobal.HasType("WhileYoureUp.Mod");
		}

		[HarmonyPriority(Priority.High)]
		static void Prefix(out bool __state)
		{
			__state = false;
			Monitor.Enter(WhileYouAreUpAccess.GlobalLock, ref __state);
		}

		[HarmonyPriority(Priority.Low)]
		static void Finalizer(bool __state, Pawn pawn)
		{
			if (__state)
			{
				//WhileYouAreUpAccess.ClearTempDetour(pawn);
				Monitor.Exit(WhileYouAreUpAccess.GlobalLock);
			}
		}
	}
}