using HarmonyLib;
using System.Threading;
using YaOpt.Helpers.ThirdParty;

namespace YaOpt.Patches.Compatibility.PerformanceOptimizer
{
	[ManualPatch]
	internal static class MultiTargets_CachedResults
	{
		private static UnfairRwLock _rwLockIsImmunityNaturally = new UnfairRwLock();

		private static SpinLock _spinLockIsInvisible = new SpinLock();

		private static SpinLock _spinLockIsQuestLodger = new SpinLock();

		static void Patch(Harmony harmony)
		{
			if (YaOptGlobal.NeedThreadSafe && YaOptGlobal.HasMod("Taranchuk.PerformanceOptimizer"))
			{
				// Optimization_HediffDef_PossibleToDevelopImmunityNaturally
				var poType = AccessTools.TypeByName(
					"PerformanceOptimizer.Optimization_HediffDef_PossibleToDevelopImmunityNaturally");
				var helperType = typeof(MultiTargets_CachedResults);
				harmony.Patch(AccessTools.Method(poType, "Prefix"),
					prefix: new HarmonyMethod(helperType, nameof(EnterReadLock)),
					finalizer: new HarmonyMethod(helperType, nameof(ExitReadLock)));
				harmony.Patch(AccessTools.Method(poType, "Postfix"),
					prefix: new HarmonyMethod(helperType, nameof(EnterWriteLock)),
					finalizer: new HarmonyMethod(helperType, nameof(ExitWriteLock)));

				// Optimization_PawnUtility_IsInvisible
				poType = AccessTools.TypeByName(
					"PerformanceOptimizer.Optimization_PawnUtility_IsInvisible");
				harmony.Patch(AccessTools.Method(poType, "Prefix"),
					prefix: new HarmonyMethod(helperType, nameof(EnterIsInvisibleLock)),
					finalizer: new HarmonyMethod(helperType, nameof(ExitIsInvisibleLock)));

				// Optimization_QuestUtility_IsQuestLodger
				poType = AccessTools.TypeByName(
					"PerformanceOptimizer.Optimization_QuestUtility_IsQuestLodger");
				harmony.Patch(AccessTools.Method(poType, "Prefix"),
					prefix: new HarmonyMethod(helperType, nameof(EnterIsQuestLodgerLock)),
					finalizer: new HarmonyMethod(helperType, nameof(ExitIsQuestLodgerLock)));

				// Don't patch Optimization_JobGiver_ConfigurableHostilityResponse
				// Because it's already been unpatched
				// yield return AccessTools.Method(typeof(JobGiver_ConfigurableHostilityResponse), "TryGiveJob");
			}
		}

		public static void EnterReadLock(out bool __state)
		{
			__state = true;
			_rwLockIsImmunityNaturally.EnterReadLock();
		}

		public static void ExitReadLock(bool __state)
		{
			if (__state)
				_rwLockIsImmunityNaturally.ExitReadLock();
		}

		public static void EnterWriteLock(out bool __state)
		{
			__state = true;
			_rwLockIsImmunityNaturally.EnterWriteLock();
		}

		public static void ExitWriteLock(bool __state)
		{
			if (__state)
				_rwLockIsImmunityNaturally.ExitWriteLock();
		}

		public static void EnterIsInvisibleLock(out bool __state)
		{
			__state = false;
			_spinLockIsInvisible.Enter(ref __state);
		}

		public static void ExitIsInvisibleLock(bool __state)
		{
			if (__state)
				_spinLockIsInvisible.Exit();
		}

		public static void EnterIsQuestLodgerLock(out bool __state)
		{
			__state = false;
			_spinLockIsQuestLodger.Enter(ref __state);
		}

		public static void ExitIsQuestLodgerLock(bool __state)
		{
			if (__state)
				_spinLockIsQuestLodger.Exit();
		}
	}
}