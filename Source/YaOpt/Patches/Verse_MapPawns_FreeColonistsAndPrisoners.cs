using HarmonyLib;
using System.Collections.Generic;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <seealso cref="YaOptSettings.OptGetMapPawns"/>
	[HarmonyPatch(typeof(MapPawns))]
	[HarmonyPatch(nameof(MapPawns.FreeColonistsAndPrisoners), MethodType.Getter)]
	internal static class Verse_MapPawns_FreeColonistsAndPrisoners
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptGetMapPawns.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.Calls("get_FreeColonists"))
				{
					yield return CodeInstruction.Call(
						typeof(MapPawnsHelper),
						nameof(MapPawnsHelper.FreeColonistsAndPrisoners));
					continue;
				}
				else if (instruction.Calls("get_PrisonersOfColony"))
				{
					yield return CodeInstruction.Call(
						typeof(MapPawnsHelper),
						nameof(MapPawnsHelper.Nothing),
						new[] { typeof(MapPawns) });
					continue;
				}
				yield return instruction;
			}
		}
	}
}