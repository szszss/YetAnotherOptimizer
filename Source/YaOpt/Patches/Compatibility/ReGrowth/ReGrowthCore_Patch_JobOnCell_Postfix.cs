using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using YaOpt.Patches.ThreadSafe.ThreadStatic;

namespace YaOpt.Patches.Compatibility.ReGrowth
{
	[HarmonyPatch]
	internal static class ReGrowthCore_Patch_JobOnCell_Postfix
	{
		static MethodBase TargetMethod()
		{
			return AccessTools.Method(
				AccessTools.TypeByName("ReGrowthCore.Patch_JobOnCell"),
				"Postfix");
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelJobGiver.Enabled &&
				   YaOptGlobal.HasType("ReGrowthCore.Patch_JobOnCell");
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if ((instruction.opcode == OpCodes.Ldsfld || instruction.opcode == OpCodes.Stsfld) &&
					instruction.operand is FieldInfo fieldInfo && fieldInfo.Name == "wantedPlantDef")
				{
					instruction.operand = AccessTools.Field(
						typeof(MultiTargets_WorkGiverGrower),
						nameof(MultiTargets_WorkGiverGrower.WantedPlantDef));
				}
				yield return instruction;
			}
		}
	}
}