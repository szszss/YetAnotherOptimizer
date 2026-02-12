using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using YaOpt.Helpers;
using YaOpt.OtherMod.FacialAnimation.Helpers;

namespace YaOpt.OtherMod.FacialAnimation.Patches
{
	/// <summary>
	/// <seealso cref="SubMod.OptFAParallelUpdate"/>
	/// </summary>
	[HarmonyPatch(typeof(Corpse))]
	[HarmonyPatch(nameof(Corpse.DynamicDrawPhaseAt))]
	internal static class Verse_Corpse_DynamicDrawPhaseAt
	{
		static bool Prepare(MethodBase original)
		{
			if (original != null)
			{
				MiscHelper.CheckHash(original, Hash128.Parse("dcbb36f4b8f2d28d9c4b0aa0a5e7d63d"));
			}
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
				new[] { typeof(Corpse) });
			yield return new CodeInstruction(OpCodes.Nop).WithLabels(label);
			foreach (var instruction in instructions)
			{
				yield return instruction;
			}
		}
	}
}