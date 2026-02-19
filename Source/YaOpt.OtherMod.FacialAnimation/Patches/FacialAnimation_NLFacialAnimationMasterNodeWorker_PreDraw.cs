using FacialAnimation;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using YaOpt.Helpers;

namespace YaOpt.OtherMod.FacialAnimation.Patches
{
	/// <summary>
	/// Skips original PreDraw since updates are handled in the parallel phase.
	/// </summary>
	/// <seealso cref="SubMod.OptFAParallelUpdate"/>
	[HarmonyPatch(typeof(NLFacialAnimationMasterNodeWorker))]
	[HarmonyPatch(nameof(NLFacialAnimationMasterNodeWorker.PreDraw))]
	internal static class FacialAnimation_NLFacialAnimationMasterNodeWorker_PreDraw
	{
		static bool Prepare(MethodBase original)
		{
			if (original != null)
			{
				MiscHelper.CheckHash(original, Hash128.Parse("c4d55baeb854aa6ef8fbf1295c5d0e88"));
			}
			return SubMod.OptFAParallelUpdate.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldarga_S)
				{
					yield return new CodeInstruction(OpCodes.Ret);
					break;
				}
				yield return instruction;
			}
		}
	}
}