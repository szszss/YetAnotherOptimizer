using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using YaOpt.Helpers.ThreadSafe;

namespace YaOpt.Patches.Compatibility.PerformanceFish
{
	[HarmonyPatch]
	internal static class MultiTargets_RecipeIngredientCache
	{
		internal static GreedySpinLock SpinLock = new GreedySpinLock();

		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(
				AccessTools.TypeByName("PerformanceFish.JobSystem.WorkGiver_DoBillOptimization/TryFindBestIngredientsHelper_Patch"),
				"Postfix");
			yield return AccessTools.Method(
				AccessTools.TypeByName("PerformanceFish.JobSystem.WorkGiver_DoBillOptimization/TryFindBestIngredientsInSet_NoMixHelper_Patch"),
				"MarkIngredientCountAsFound");
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelWorkGiver.Enabled && YaOptGlobal.HasMod("bs.performance");
		}

		static void Prefix(out bool __state)
		{
			__state = false;
			SpinLock.Enter(ref __state);
		}

		static void Finalizer(bool __state)
		{
			if (__state)
				SpinLock.Exit();
		}
	}
}