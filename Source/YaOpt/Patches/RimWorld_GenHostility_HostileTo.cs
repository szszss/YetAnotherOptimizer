using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <seealso cref="YaOptSettings.OptAttackTargetFinder"/>
	[HarmonyPatch(typeof(GenHostility))]
	[HarmonyPatch(nameof(GenHostility.HostileTo), typeof(Thing), typeof(Thing))]
	internal static class RimWorld_GenHostility_HostileTo
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptAttackTargetFinder.Enabled;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool IsShamblerFast(Pawn pawn)
		{
			return ModsConfig.AnomalyActive && pawn.IsMutant && pawn.mutant.Def == MutantDefOf.Shambler;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.Calls("get_IsShambler"))
				{
					yield return CodeInstruction.Call(
						typeof(RimWorld_GenHostility_HostileTo),
						nameof(IsShamblerFast));
					continue;
				}
				yield return instruction;
			}
		}
	}
}