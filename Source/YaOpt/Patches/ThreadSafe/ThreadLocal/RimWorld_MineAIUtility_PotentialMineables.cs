using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using YaOpt.Helpers;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch]
	internal static class RimWorld_MineAIUtility_PotentialMineables
	{
		static MethodBase TargetMethod()
		{
			foreach (var nestedType in typeof(MineAIUtility).GetNestedTypes(
						 BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic))
			{
				var method = nestedType.GetMethod("MoveNext",
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (method != null)
				{
					YaOptMod.Debug($"RimWorld_MineAIUtility_PotentialMineables found a method from MineAIUtility: {method.FullName()}");
					return method;
				}
			}
			return null;
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelWorkGiver.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(
			IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			return ThreadLocalHelper.ThreadLocalTranspiler(
				instructions, generator, "tmpDesignations");
		}
	}
}
