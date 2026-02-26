using HarmonyLib;
using RimWorld;
using Verse;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe
{
	[HarmonyPatch(typeof(Pawn_MeleeVerbs))]
	[HarmonyPatch(nameof(Pawn_MeleeVerbs.PawnMeleeVerbsStaticUpdate))]
	internal static class RimWorld_Pawn_MeleeVerbs_PawnMeleeVerbsStaticUpdate
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelPawnTick.Enabled;
		}

		static void Cleanup()
		{
			ThreadLocalTmpList<Pawn_MeleeVerbs, VerbEntry>.TrackAllValues = true;
			ThreadLocalTmpList<Pawn_MeleeVerbs, Verb>.TrackAllValues = true;
		}

		static void Postfix()
		{
			if (GenTicks.TicksGame % 3600 != 1)
				return;

			foreach (var list in ThreadLocalTmpList<Pawn_MeleeVerbs, VerbEntry>.Values)
			{
				list.Clear();
			}
			foreach (var list in ThreadLocalTmpList<Pawn_MeleeVerbs, Verb>.Values)
			{
				list.Clear();
			}
		}
	}
}