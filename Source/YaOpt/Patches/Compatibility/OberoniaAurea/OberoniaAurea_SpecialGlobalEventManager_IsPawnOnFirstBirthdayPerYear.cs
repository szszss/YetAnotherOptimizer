using HarmonyLib;
using System.Reflection;
using YaOpt.Helpers;

namespace YaOpt.Patches.Compatibility.OberoniaAurea
{
	[HarmonyPatch]
	internal static class OberoniaAurea_SpecialGlobalEventManager_IsPawnOnFirstBirthdayPerYear
	{
		private static GreedySpinLock _spinLock = new GreedySpinLock();

		static MethodBase TargetMethod()
		{
			return AccessTools.Method(
				AccessTools.TypeByName("OberoniaAurea.SpecialGlobalEventManager"),
				"IsPawnOnFirstBirthdayPerYear");
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelThoughtUpdate.Enabled &&
				   YaOptGlobal.HasType("OberoniaAurea.SpecialGlobalEventManager") &&
				   YaOptGlobal.HasMod("Taranchuk.PerformanceOptimizer");
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