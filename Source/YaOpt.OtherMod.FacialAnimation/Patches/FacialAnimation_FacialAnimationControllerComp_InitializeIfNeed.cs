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
	/// <seealso cref="SubMod.OptFAAnimCache"/>
	/// </summary>
	[HarmonyPatch(typeof(FacialAnimationControllerComp))]
	[HarmonyPatch("InitializeIfNeed")]
	internal static class  FacialAnimation_FacialAnimationControllerComp_InitializeIfNeed
	{
		static bool Prepare(MethodBase original)
		{
			if (original != null)
			{
				MiscHelper.CheckHash(original, Hash128.Parse("f358239192413eae6a1edf2f65ae9ff4"));
			}
			return SubMod.OptFAAnimCache.Enabled;
		}

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo methodInfo)
				{
					if (methodInfo.Name == "ChangeAnimationListWithReset")
					{
						instruction.operand = AccessTools.Method(
							typeof(JobAnimationHelper), nameof(JobAnimationHelper.ChangeAnimationListWithReset));
						YaOptMod.Log("FacialAnimationControllerComp_InitializeIfNeed:ChangeAnimationListWithReset");
						yield return CodeInstruction.LoadArgument(0);
						yield return CodeInstruction.LoadField(typeof(FacialAnimationControllerComp),
							"currentJobAnimationList");
					}
					else if (methodInfo.Name == "FilterAnimationListWithCurrentStatus")
					{
						instruction.operand = AccessTools.Method(
							typeof(JobAnimationHelper), nameof(JobAnimationHelper.FilterAnimationListWithCurrentStatus));
						YaOptMod.Log("FacialAnimationControllerComp_InitializeIfNeed:FilterAnimationListWithCurrentStatus");
						yield return CodeInstruction.LoadArgument(0);
						yield return CodeInstruction.LoadField(typeof(FacialAnimationControllerComp),
							"currentJobAnimationList");
					}
				}
				yield return instruction;
			}
		}
	}
}