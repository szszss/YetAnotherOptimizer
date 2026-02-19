using FacialAnimation;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using YaOpt.Helpers;
using YaOpt.OtherMod.FacialAnimation.Helpers;

namespace YaOpt.OtherMod.FacialAnimation.Patches
{
	/// <summary>
	/// Moves pawn parameter gathering to the parallel update phase.
	/// </summary>
	/// <seealso cref="SubMod.OptFAParallelUpdate"/>
	[HarmonyPatch(typeof(FacialAnimationControllerComp))]
	[HarmonyPatch("GatherPawnParam")]
	internal static class FacialAnimation_FacialAnimationControllerComp_GatherPawnParam
	{
		static bool Prepare(MethodBase original)
		{
			if (original != null)
			{
				MiscHelper.CheckHash(original, Hash128.Parse("391a7cf07b711f5dc30e09017a237260"));
			}
			return SubMod.OptFAParallelUpdate.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				yield return instruction;

				if (instruction.opcode == OpCodes.Stfld && instruction.operand is FieldInfo field &&
					field.Name == "enableHighlight")
				{
					// ThoughtsHelper.TryUpdateThoughts(this.animationParam, this.pawn, false);
					yield return CodeInstruction.LoadArgument(0);
					yield return CodeInstruction.LoadField(
						typeof(FacialAnimationControllerComp),
						"animationParam");
					yield return CodeInstruction.LoadArgument(0);
					yield return CodeInstruction.LoadField(
						typeof(FacialAnimationControllerComp),
						"pawn");
					yield return new CodeInstruction(OpCodes.Ldc_I4_0);
					yield return CodeInstruction.Call(
						typeof(ThoughtsHelper), nameof(ThoughtsHelper.TryUpdateThoughts));
					yield return new CodeInstruction(OpCodes.Ret);
					break;
				}
			}
		}
	}
}