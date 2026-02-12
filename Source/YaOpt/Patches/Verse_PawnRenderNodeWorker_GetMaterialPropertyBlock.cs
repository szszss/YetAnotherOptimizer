using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptParallelMaterialUpdate"/>
	/// </summary>
	[HarmonyPatch(typeof(PawnRenderNodeWorker))]
	[HarmonyPatch(nameof(PawnRenderNodeWorker.GetMaterialPropertyBlock))]
	internal static class Verse_PawnRenderNodeWorker_GetMaterialPropertyBlock
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelMaterialUpdate.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// if (CanParallelMaterialUpdate(this))
			//     return node.MatPropBlock
			var label = generator.DefineLabel();
			yield return CodeInstruction.LoadArgument(0);
			yield return CodeInstruction.Call(typeof(ParallelPreDrawHelper),
				nameof(ParallelPreDrawHelper.CanParallelMaterialUpdate));
			yield return new CodeInstruction(OpCodes.Brfalse_S, label);
			yield return CodeInstruction.LoadArgument(1);
			yield return new CodeInstruction(OpCodes.Callvirt,
				AccessTools.PropertyGetter(typeof(PawnRenderNode), nameof(PawnRenderNode.MatPropBlock)));
			yield return new CodeInstruction(OpCodes.Ret);
			yield return new CodeInstruction(OpCodes.Nop).WithLabels(label);
			foreach (var instruction in instructions)
			{
				yield return instruction;
			}
		}
	}
}