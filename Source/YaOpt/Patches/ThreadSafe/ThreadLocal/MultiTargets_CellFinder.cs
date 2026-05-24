using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch]
	internal static class MultiTargets_CellFinder
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(CellFinder),
				nameof(CellFinder.TryFindRandomReachableNearbyCell));
			yield return AccessTools.Method(typeof(CellFinder),
				nameof(CellFinder.TryFindRandomReachableCellNearPosition));
			yield return AccessTools.Method(typeof(CellFinder),
				nameof(CellFinder.RandomRegionNear));
			foreach (var nestedType in typeof(CellFinder).GetNestedTypes(
						 BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic))
			{
				foreach (var method in nestedType.GetMethods(
					BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
				{
					var param = method.GetParameters();
					if (param?.Length == 1 && param[0].ParameterType == typeof(Region) &&
						method.ReturnType == typeof(bool) &&
						(method.Name.Contains("<RandomRegionNear>") ||
						 method.Name.Contains("<TryFindRandomReachableNearbyCell>") ||
						 method.Name.Contains("<TryFindRandomReachableCellNearPosition>")))
					{
						YaOptMod.Debug($"MultiTargets_CellFinder found a method from CellFinder: {method.FullName()}");
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
			ILGenerator generator, MethodBase methodBase)
		{
			return ThreadLocalHelper.ThreadLocalTranspiler(instructions, generator, "workingRegions");
		}
	}
}