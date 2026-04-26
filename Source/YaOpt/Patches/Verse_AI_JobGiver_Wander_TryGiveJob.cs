using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	[HarmonyPatch(typeof(JobGiver_Wander))]
	[HarmonyPatch("TryGiveJob")]
	internal static class Verse_AI_JobGiver_Wander_TryGiveJob
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptIdleThrottle.Enabled;
		}

		static void Postfix(JobGiver_Wander __instance, Pawn pawn, ref Job __result)
		{
			if (__result != null && __result.expiryInterval > 0 && __instance is JobGiver_WanderColony && pawn.IsFreeColonist)
			{
				__result.expiryInterval = IdleHelper.StopWanderInterval(__result.expiryInterval);
			}
		}
	}
}