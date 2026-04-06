using HarmonyLib;
using System.Reflection;
using YaOpt.Helpers;

namespace YaOpt.Patches.Compatibility.OberoniaAurea
{
	[HarmonyPatch]
	internal static class OberoniaAureaGene_OAGeneUtility_OAGeneMapComp
	{
		private static GreedySpinLock _spinLock = new GreedySpinLock();

		static MethodBase TargetMethod()
		{
			return AccessTools.Method(
				AccessTools.TypeByName("OberoniaAureaGene.OAGeneUtility"),
				"OAGeneMapComp");
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelPawnTick.Enabled &&
				   YaOptGlobal.Settings.ParallelPawnMoodUpdate &&
				   YaOptGlobal.HasType("OberoniaAureaGene.OAGeneUtility") &&
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