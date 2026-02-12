using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.OtherMod.FacialAnimation.Helpers;

namespace YaOpt.OtherMod.FacialAnimation.Patches
{
	/// <summary>
	/// <seealso cref="SubMod.OptFAParallelUpdate"/>
	/// </summary>
	[HarmonyPatch(typeof(Pawn))]
	[HarmonyPatch(nameof(Pawn.DynamicDrawPhaseAt))]
	internal static class Verse_Pawn_DynamicDrawPhaseAt
	{
		static bool Prepare(MethodBase original)
		{
			return SubMod.OptFAParallelUpdate.Enabled;
		}

		/*
		 * if (phase == ParallelPreDraw) ParallelUpdateHelper.UpdateFacialAnimation(this)
		 */
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var label = generator.DefineLabel();
			yield return CodeInstruction.LoadArgument(1);
			yield return new CodeInstruction(OpCodes.Ldc_I4_1);
			yield return new CodeInstruction(OpCodes.Bne_Un_S, label);
			yield return CodeInstruction.LoadArgument(0);
			yield return CodeInstruction.Call(
				typeof(ParallelUpdateHelper),
				nameof(ParallelUpdateHelper.UpdateFacialAnimation),
				new[] { typeof(Pawn) });
			yield return new CodeInstruction(OpCodes.Nop).WithLabels(label);
			foreach (var instruction in instructions)
			{
				yield return instruction;
			}
		}
	}
}