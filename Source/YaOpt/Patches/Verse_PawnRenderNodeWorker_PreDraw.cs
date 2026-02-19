using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace YaOpt.Patches
{
	/// <summary>
	/// Skips PreDraw for workers with parallel material updates (handled in ParallelPreDraw).
	/// </summary>
	/// <seealso cref="YaOptSettings.OptParallelMaterialUpdate"/>
	[HarmonyPatch(typeof(PawnRenderNodeWorker))]
	[HarmonyPatch(nameof(PawnRenderNodeWorker.PreDraw))]
	internal static class Verse_PawnRenderNodeWorker_PreDraw
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelMaterialUpdate.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			/*
			 * return nothing
			 */
			yield return new CodeInstruction(OpCodes.Ret);
		}
	}
}