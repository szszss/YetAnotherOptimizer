using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Verse;

namespace YaOpt.Patches.Compatibility.PerformanceOptimizer
{
	[HarmonyPatch]
	internal static class MultiTargets_CachedResults
	{
		// I was too lazy to prepare a separate lock for each patched method,
		// so I choose partition locks.
		private const int PARTITION_COUNT = 16;
		private static readonly object[] _partitionLockObjs = new object[PARTITION_COUNT];

		static IEnumerable<MethodBase> TargetMethods()
		{
			// Optimization_PawnUtility_IsInvisible
			yield return AccessTools.Method(typeof(InvisibilityUtility),
				nameof(InvisibilityUtility.IsPsychologicallyInvisible));

			// Optimization_HediffDef_PossibleToDevelopImmunityNaturally
			yield return AccessTools.Method(typeof(HediffSet),
				nameof(HediffSet.HasImmunizableNotImmuneHediff));

			// Optimization_QuestUtility_IsQuestLodger
			yield return AccessTools.Method(typeof(QuestUtility),
				nameof(QuestUtility.IsQuestLodger));

			// Don't patch Optimization_JobGiver_ConfigurableHostilityResponse
			// Because it's already been unpatched
			// yield return AccessTools.Method(typeof(JobGiver_ConfigurableHostilityResponse), "TryGiveJob");
		}

		static MultiTargets_CachedResults()
		{
			for (var i = 0; i < _partitionLockObjs.Length; i++)
			{
				_partitionLockObjs[i] = new object();
			}
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe && YaOptGlobal.HasMod("Taranchuk.PerformanceOptimizer");
		}

		[HarmonyBefore("PerformanceOptimizer.Main")]
		static void Prefix(MethodBase __originalMethod, out bool __state)
		{
			var lockObj = _partitionLockObjs[Math.Abs(__originalMethod.GetHashCode()) % PARTITION_COUNT];
			__state = false;
			Monitor.Enter(lockObj, ref __state);
		}

		[HarmonyAfter("PerformanceOptimizer.Main")]
		static void Finalizer(MethodBase __originalMethod, bool __state)
		{
			var lockObj = _partitionLockObjs[Math.Abs(__originalMethod.GetHashCode()) % PARTITION_COUNT];
			if (__state)
				Monitor.Exit(lockObj);
		}
	}
}