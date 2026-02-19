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
	/// Uses cached animation lists during animation updates to reduce allocations.
	/// </summary>
	/// <seealso cref="SubMod.OptFAAnimCache"/>
	[HarmonyPatch(typeof(FacialAnimationControllerComp))]
	[HarmonyPatch(nameof(FacialAnimationControllerComp.UpdateAnimation))]
	internal static class FacialAnimation_FacialAnimationControllerComp_UpdateAnimation
	{
		static bool Prepare(MethodBase original)
		{
			if (original != null)
			{
				MiscHelper.CheckHash(original, Hash128.Parse("727596cdd274d024937db37275594998"));
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
						YaOptMod.Log("FacialAnimationControllerComp_UpdateAnimation:ChangeAnimationListWithReset");
						yield return CodeInstruction.LoadArgument(0);
						yield return CodeInstruction.LoadField(typeof(FacialAnimationControllerComp),
							"currentJobAnimationList");
					}
					else if (methodInfo.Name == "FilterAnimationListWithCurrentStatus")
					{
						instruction.operand = AccessTools.Method(
							typeof(JobAnimationHelper), nameof(JobAnimationHelper.FilterAnimationListWithCurrentStatus));
						YaOptMod.Log("FacialAnimationControllerComp_UpdateAnimation:FilterAnimationListWithCurrentStatus");
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