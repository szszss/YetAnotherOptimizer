using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch]
	[HarmonyAfter("OskarPotocki.VEF", "bs.performance")]
	internal static class MultiTargets_WorkGiverDoBill
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(WorkGiver_DoBill),
				"StartOrResumeBillJob");
			yield return AccessTools.Method(typeof(WorkGiver_DoBill),
				"TryFindBestIngredientsHelper");
			yield return AccessTools.Method(typeof(WorkGiver_DoBill),
				"AddEveryMedicineToRelevantThings");
			yield return AccessTools.Method(typeof(WorkGiver_DoBill),
				"TryFindBestIngredientsInSet_NoMixHelper");
			foreach (var nestedType in typeof(WorkGiver_DoBill).GetNestedTypes(
						 BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic))
			{
				var method = AccessTools.FirstMethod(nestedType, methodInfo =>
				{
					var param = methodInfo.GetParameters();
					return param?.Length == 1 && param[0].ParameterType == typeof(Region) &&
						   (methodInfo.Name.Contains("<TryFindBestIngredientsHelper>b__4"));
				});
				if (method != null)
				{
					YaOptMod.Debug($"MultiTargets_WorkGiverDoBill found a method from WorkGiver_DoBill: {method.FullName()}");
					yield return method;
				}
			}
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelWorkGiver.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
			ILGenerator generator, MethodBase methodBase)
		{
			if (methodBase.Name == "StartOrResumeBillJob")
			{
				instructions = ThreadLocalHelper.ThreadLocalTranspiler(instructions, generator, "missingIngredients");
				return ThreadLocalHelper.ThreadLocalTranspiler(instructions, generator, "tmpMissingUniqueIngredients");
			}
			if (methodBase.Name.Contains("TryFindBestIngredientsHelper"))
			{
				instructions = ThreadLocalHelper.ThreadLocalTranspiler(instructions, generator, "relevantThings");
				// PerformanceFish will rewrite this and remove processedThings.
				instructions = ThreadLocalHelper.ThreadLocalTranspiler(instructions, generator,
					AccessTools.Field(typeof(WorkGiver_DoBill), "processedThings"));
				return ThreadLocalHelper.ThreadLocalTranspiler(instructions, generator, "newRelevantThings");
			}
			if (methodBase.Name == "AddEveryMedicineToRelevantThings")
			{
				return ThreadLocalHelper.ThreadLocalTranspiler(instructions, generator, "tmpMedicine");
			}
			if (methodBase.Name == "TryFindBestIngredientsInSet_NoMixHelper")
			{
				return ThreadLocalHelper.ThreadLocalTranspiler(instructions, generator, "availableCounts");
			}
			throw new ArgumentException($"Unknown method: {methodBase}", nameof(methodBase));
		}
	}
}