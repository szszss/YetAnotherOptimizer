using HarmonyLib;
using Verse;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.Reach
{
	[HarmonyPatch(typeof(Reachability))]
	[HarmonyPatch("CheckCellBasedReachability")]
	// TODO:
	internal static class Verse_Reachability_CheckCellBasedReachability
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelJobGiver.Enabled;
		}

		static void Prefix(out bool __state)
		{
			__state = false;
			ThreadLocalReachability.EnterLock(ref __state);
		}

		static void Finalizer(bool __state)
		{
			ThreadLocalReachability.ExitLock(__state);
		}
	}
}