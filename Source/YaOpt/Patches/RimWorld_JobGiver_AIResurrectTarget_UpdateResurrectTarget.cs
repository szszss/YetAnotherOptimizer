using HarmonyLib;
using RimWorld;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <seealso cref="YaOptSettings.OptFastListerRemove"/>
	[HarmonyPatch(typeof(JobGiver_AIResurrectTarget))]
	[HarmonyPatch("UpdateResurrectTarget")]
	internal static class RimWorld_JobGiver_AIResurrectTarget_UpdateResurrectTarget
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptFastListerRemove.Enabled;
		}

		static void Postfix(Pawn pawn)
		{
			ListerThingsHelper.RebuildIndex(pawn.Map.listerThings, ThingRequestGroup.Corpse);
		}
	}
}
