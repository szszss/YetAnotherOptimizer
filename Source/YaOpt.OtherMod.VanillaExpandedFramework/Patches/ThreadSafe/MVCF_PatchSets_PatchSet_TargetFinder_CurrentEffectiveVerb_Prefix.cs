using HarmonyLib;
using MVCF.PatchSets;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using YaOpt.Patches.Early;

namespace YaOpt.OtherMod.VanillaExpandedFramework.Patches.ThreadSafe
{
	// TODO: Dirty hack. Mono JIT inlines Prefix and Finalizer of VEF, so modifications to
	// them must be made before the VEF patcher runs.
	[EarlyPatch]
	[HarmonyPatch(typeof(PatchSet_TargetFinder))]
	[HarmonyPatch(nameof(PatchSet_TargetFinder.CurrentEffectiveVerb_Prefix))]
	internal static class MVCF_PatchSets_PatchSet_TargetFinder_CurrentEffectiveVerb_Prefix
	{
		static bool Prepare()
		{
			return true;
			//return YaOptGlobal.NeedThreadSafe;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldsfld && instruction.operand is FieldInfo field &&
					field.Name == "SearchVerb")
				{
					instruction.operand = AccessTools.Field(
						typeof(MultiTargets_SearchVerb),
						nameof(MultiTargets_SearchVerb.SearchVerb));
				}
				yield return instruction;
			}
		}
	}
}