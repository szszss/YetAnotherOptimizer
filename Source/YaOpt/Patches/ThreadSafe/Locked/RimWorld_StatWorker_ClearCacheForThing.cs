using HarmonyLib;
using RimWorld;
using System.Threading;

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

		static void Prefix(StatWorker __instance, out ReaderWriterLockSlim __state)
		{
			__state = RimWorld_StatWorker_GetValue.GetLock(__instance);
			__state.EnterWriteLock();
		}

		static void Finalizer(ReaderWriterLockSlim __state)
		{
			if (__state != null)
				__state.ExitWriteLock();
		}
	}
}