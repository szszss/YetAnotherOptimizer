using HarmonyLib;
using RimWorld;
using YaOpt.Helpers;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(WorkGiver_TakeToPen))]
	[HarmonyPatch(nameof(WorkGiver_TakeToPen.JobOnThing))]
	internal static class RimWorld_WorkGiver_TakeToPen_JobOnThing
	{
		private static GreedySpinLock _spinLock = new GreedySpinLock();

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelJobGiver.Enabled;
		}

		static void Prefix(out bool __state)
		{
			__state = false;
			_spinLock.Enter(ref __state);
		}

		static void Finalizer(bool __state)
		{
			if (__state)
				_spinLock.Exit();
		}
	}
}