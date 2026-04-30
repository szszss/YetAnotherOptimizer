using HarmonyLib;
using RimWorld;
using System.Threading;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(WorkGiver_FeedBabyManually))]
	[HarmonyPatch("AutoBreastfeederAvailable")]
	internal static class RimWorld_WorkGiver_FeedBabyManually_AutoBreastfeederAvailable
	{
		private static readonly object lockObj = new object();

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelWorkGiver.Enabled;
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