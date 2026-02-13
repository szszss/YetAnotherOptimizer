using HarmonyLib;
using RimWorld;
using System.Threading;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(WorkGiver_TakeToPen))]
	[HarmonyPatch(nameof(WorkGiver_TakeToPen.JobOnThing))]
	internal static class RimWorld_WorkGiver_TakeToPen_JobOnThing
	{
		private static readonly object lockObj = new object();

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelJobGiver.Enabled;
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