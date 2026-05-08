using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using YaOpt.Helpers;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.Compatibility.ReGrowth
{
	[HarmonyPatch]
	internal static class ReGrowthCore_MineAIUtility_PotentialMineables_Patch_Postfix
	{
		static MethodBase TargetMethod()
		{
			var type = AccessTools.TypeByName("ReGrowthCore.MineAIUtility_PotentialMineables_Patch");
			foreach (var nestedType in type.GetNestedTypes(
						 BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic))
			{
				var method = nestedType.GetMethod("MoveNext",
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (method != null)
				{
					YaOptMod.Debug($"ReGrowthCore_MineAIUtility_PotentialMineables_Patch_Postfix found a method from MineAIUtility: {method.FullName()}");
					return method;
				}
			}
			return null;
		}

		static bool Prepare()
		{
			return (YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelWorkGiver.Enabled) &&
				   YaOptGlobal.HasType("ReGrowthCore.MineAIUtility_PotentialMineables_Patch");
		}

		static IEnumerable<CodeInstruction> Transpiler(
			IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			return ThreadLocalHelper.ThreadLocalTranspiler(
				instructions, generator, "tmpDesignations");
		}
	}
}
