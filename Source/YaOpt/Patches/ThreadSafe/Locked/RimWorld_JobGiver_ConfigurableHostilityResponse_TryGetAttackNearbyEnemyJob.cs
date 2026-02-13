using HarmonyLib;
using RimWorld;
using System.Threading;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(JobGiver_ConfigurableHostilityResponse))]
	[HarmonyPatch("TryGetAttackNearbyEnemyJob")]
	internal static class RimWorld_JobGiver_ConfigurableHostilityResponse_TryGetAttackNearbyEnemyJob
	{
		private static readonly object lockObj = new object();

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		static void Prefix(out bool __state)
		{
			__state = false;
			Monitor.Enter(lockObj, ref __state);
		}

		static void Finalizer(bool __state)
		{
			if (__state)
				Monitor.Exit(lockObj);
		}
	}
}