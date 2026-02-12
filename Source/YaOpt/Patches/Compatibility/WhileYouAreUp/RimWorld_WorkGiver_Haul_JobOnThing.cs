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

		[HarmonyPriority(Priority.High)]
		static void Prefix(out bool __state)
		{
			__state = false;
			Monitor.Enter(WhileYouAreUpAccess.GlobalLock, ref __state);
		}

		[HarmonyPriority(Priority.LowerThanNormal)]
		static void Finalizer(bool __state, Pawn pawn)
		{
			if (__state)
			{
				WhileYouAreUpAccess.ClearTempDetour(pawn);
				Monitor.Exit(WhileYouAreUpAccess.GlobalLock);
			}
		}
	}
}