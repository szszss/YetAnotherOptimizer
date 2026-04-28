using System;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.Compatibility.PerformanceFish
{
	[HarmonyPatch]
	internal static class MultiTargets_PFWorkGiverDoBill
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			var type = AccessTools.TypeByName("PerformanceFish.JobSystem.WorkGiver_DoBillPrepatches/TryFindBestIngredientsHelper_InnerDelegate_Patch");
			yield return AccessTools.Method(type, "ActualLoop");
			yield return AccessTools.Method(type, "GetList");
			yield return AccessTools.Method(type, "InsertAtCorrectPosition");
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelJobGiver.Enabled && YaOptGlobal.HasMod("bs.performance");
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
			ILGenerator generator, MethodBase methodBase)
		{
			if (methodBase.Name == "GetList")
			{
				instructions = ThreadLocalHelper.ThreadLocalTranspiler(instructions, generator, "_tempSetForIngredientDefs");
				return ThreadLocalHelper.ThreadLocalTranspiler(instructions, generator, "_tempListForIngredients");
			}
			if (methodBase.Name == "InsertAtCorrectPosition")
			{
				return ThreadLocalHelper.ThreadLocalTranspiler(instructions, generator, "_insertAtCorrectPositionComparer");
			}
			if (methodBase.Name == "ActualLoop")
			{
				instructions = ThreadLocalHelper.ThreadLocalTranspiler(instructions, generator, "processedThings");
				return ThreadLocalHelper.ThreadLocalTranspiler(instructions, generator, "newRelevantThings");
			}
			throw new ArgumentException($"Unknown method: {methodBase}", nameof(methodBase));
		}
	}
}