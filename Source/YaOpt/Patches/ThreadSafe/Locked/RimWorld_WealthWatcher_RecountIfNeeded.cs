using HarmonyLib;
using RimWorld;
using YaOpt.Helpers;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(WealthWatcher))]
	[HarmonyPatch("RecountIfNeeded")]
	internal static class RimWorld_WealthWatcher_RecountIfNeeded
	{
		private static GreedySpinLock _spinLock = new GreedySpinLock();

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelThoughtUpdate.Enabled;
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