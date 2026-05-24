using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;
using YaOpt.Helpers;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch]
	internal static class Verse_AI_HaulAIUtility_TryFindSpotToPlaceHaulableCloseTo
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			foreach (var nestedType in typeof(HaulAIUtility).GetNestedTypes(
						 BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic))
			{
				foreach (var method in nestedType.GetMethods(
					BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
				{
					var param = method.GetParameters();
					if (param?.Length == 1 && param[0].ParameterType == typeof(Region) &&
						method.ReturnType == typeof(bool) &&
						(method.Name.Contains("<TryFindSpotToPlaceHaulableCloseTo>")))
					{
						YaOptMod.Debug($"Verse_AI_HaulAIUtility found a method from HaulAIUtility: {method.FullName()}");
						yield return method;
					}
				}
			}
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelWorkGiver.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			return ThreadLocalHelper.ThreadLocalTranspiler(instructions, generator, "candidates");
		}
	}
}
