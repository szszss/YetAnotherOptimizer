using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch]
	internal static class MultiTargets_RegionGrid
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			foreach (var nestedType in typeof(RegionGrid).GetNestedTypes(
				BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic))
			{
				foreach (var method in nestedType.GetMethods(
					BindingFlags.Instance | BindingFlags.Static |
					BindingFlags.Public | BindingFlags.NonPublic))
				{
					if (nestedType.Name.Contains("<get_AllRegions_NoRebuild_InvalidAllowed>") &&
						method.Name == "MoveNext")
					{
						yield return method;
					}

					if (nestedType.Name.Contains("<get_AllRegions>") &&
						method.Name == "MoveNext")
					{
						yield return method;
					}
				}
			}
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
			ILGenerator generator)
		{
			return ThreadLocalHelper.ThreadLocalTranspiler(instructions, generator, "allRegionsYielded");
		}
	}
}
