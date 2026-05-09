using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.Compatibility.RPEFramework
{
	[HarmonyPatch]
	internal static class MultiTargets_ConstraintExtension
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			var type = AccessTools.TypeByName("RPEF.ConstraintExtension");
			foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
			{
				if (method.Name == "CheckAllConstraints")
					yield return method;
			}
		}

		static bool Prepare(MethodBase original)
		{
			if (original != null)
				return true;
			if (!YaOptGlobal.Settings.OptParallelWorkGiver.Enabled)
				return false;
			var type = AccessTools.TypeByName("RPEF.ConstraintExtension");
			return type != null && AccessTools.Field(type, "_tmpConstraints") != null;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			return ThreadLocalHelper.ThreadLocalTranspiler(instructions, generator, "_tmpConstraints");
		}
	}
}