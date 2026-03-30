using HarmonyLib;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace YaOpt.Patches
{
	[HarmonyPatch(typeof(GenClosest))]
	[HarmonyPatch(nameof(GenClosest.ClosestThingReachable))]
	internal static class Verse_GenClosest_ClosestThingReachable
	{
		private struct Vail
		{
			public IntVec3 Root;
			private Map Map;
			private PathEndMode peMode;
			private TraverseParms traverseParams;
		}

		static bool Prepare()
		{
			return true;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				yield return instruction;
			}
		}
	}
}