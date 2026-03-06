using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.Compatibility.PerformanceOptimizer
{
	[ManualPatch]
	internal static class MultiTargets_CachedResults
	{
		// I was too lazy to prepare a separate lock for each patched method,
		// so I choose partition locks.
		private const int PARTITION_COUNT = 16;
		private static readonly object[] _partitionLockObjs = new object[PARTITION_COUNT];

		private class DummyClass1
		{
		}

		private class DummyClass2
		{
		}

		private class DummyClass3
		{
		}

		static void Patch(Harmony harmony)
		{
			if (YaOptGlobal.NeedThreadSafe && YaOptGlobal.HasMod("Taranchuk.PerformanceOptimizer"))
			{
				// Optimization_HediffDef_PossibleToDevelopImmunityNaturally
				var poType = AccessTools.TypeByName(
					"PerformanceOptimizer.Optimization_HediffDef_PossibleToDevelopImmunityNaturally");
				var helperType = typeof(LockBoilerplate.UnfairReadWrite<DummyClass1>);
				harmony.Patch(AccessTools.Method(poType, "Prefix"),
					new HarmonyMethod(helperType, LockBoilerplate.ENTER_READ),
					new HarmonyMethod(helperType, LockBoilerplate.EXIT_READ));
				harmony.Patch(AccessTools.Method(poType, "Postfix"),
					new HarmonyMethod(helperType, LockBoilerplate.ENTER_WRITE),
					new HarmonyMethod(helperType, LockBoilerplate.EXIT_WRITE));

				// Optimization_PawnUtility_IsInvisible
				poType = AccessTools.TypeByName(
					"PerformanceOptimizer.Optimization_PawnUtility_IsInvisible");
				helperType = typeof(LockBoilerplate.Spin<DummyClass2>);
				harmony.Patch(AccessTools.Method(poType, "Prefix"),
					new HarmonyMethod(helperType, LockBoilerplate.ENTER),
					new HarmonyMethod(helperType, LockBoilerplate.EXIT));

				// Optimization_QuestUtility_IsQuestLodger
				poType = AccessTools.TypeByName(
					"PerformanceOptimizer.Optimization_QuestUtility_IsQuestLodger");
				helperType = typeof(LockBoilerplate.Spin<DummyClass3>);
				harmony.Patch(AccessTools.Method(poType, "Prefix"),
					new HarmonyMethod(helperType, LockBoilerplate.ENTER),
					new HarmonyMethod(helperType, LockBoilerplate.EXIT));

				// Don't patch Optimization_JobGiver_ConfigurableHostilityResponse
				// Because it's already been unpatched
				// yield return AccessTools.Method(typeof(JobGiver_ConfigurableHostilityResponse), "TryGiveJob");
			}
		}
	}
}