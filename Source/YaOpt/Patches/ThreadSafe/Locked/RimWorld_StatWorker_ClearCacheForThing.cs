using HarmonyLib;
using RimWorld;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(StatWorker))]
	[HarmonyPatch(nameof(StatWorker.ClearCacheForThing))]
	internal static class RimWorld_StatWorker_ClearCacheForThing
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		static void Prefix(StatWorker __instance, out int __state)
		{
			__state = -1;
			__state = RimWorld_StatWorker_GetValue.GetLockPartition(__instance);
			RimWorld_StatWorker_GetValue.StatLocks[__state].EnterWriteLock();
		}

		static void Finalizer(int __state)
		{
			if (__state >= 0)
				RimWorld_StatWorker_GetValue.StatLocks[__state].ExitWriteLock();
		}
	}
}