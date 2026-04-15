using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <seealso cref="YaOptSettings.OptGetMapPawns"/>
	[HarmonyPatch(typeof(PawnsFinder))]
	[HarmonyPatch(nameof(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonistsAndPrisoners), MethodType.Getter)]
	internal static class RimWorld_PawnsFinder_AllAlive_FreeColonistsAndPrisoners
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptGetMapPawns.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.Calls("get_AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists"))
				{
					yield return CodeInstruction.Call(
						typeof(MapPawnsHelper),
						nameof(MapPawnsHelper.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonistsAndPrisoners));
					continue;
				}
				else if (instruction.Calls("get_AllMapsCaravansAndTravellingTransporters_Alive_PrisonersOfColony"))
				{
					yield return CodeInstruction.Call(
						typeof(MapPawnsHelper),
						nameof(MapPawnsHelper.Nothing),
						Type.EmptyTypes);
					continue;
				}
				yield return instruction;
			}
		}
	}
}