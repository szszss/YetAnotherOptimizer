using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <seealso cref="YaOptSettings.OptGetMapPawns"/>
	[HarmonyPatch(typeof(ChildcareUtility))]
	[HarmonyPatch(nameof(ChildcareUtility.FindAutofeedBaby))]
	internal static class RimWorld_ChildcareUtility_FindAutofeedBaby
	{
		public const int RATIO = 3;

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptGetMapPawns.Enabled;
		}

		private static List<Pawn> ThrottleFreeHumanlikesOfFaction(MapPawns mapPawns, Faction faction)
		{
			if (Find.TickManager.TicksGame % RATIO == 0)
				return mapPawns.FreeHumanlikesOfFaction(faction);
			return mapPawns.FreeHumanlikesSpawnedOfFaction(faction);
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.Calls("FreeHumanlikesOfFaction"))
				{
					yield return CodeInstruction.Call(
						typeof(RimWorld_ChildcareUtility_FindAutofeedBaby),
						nameof(ThrottleFreeHumanlikesOfFaction));
					continue;
				}
				yield return instruction;
			}
		}
	}
}