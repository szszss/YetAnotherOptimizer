using HarmonyLib;
using System.Threading;
using Unity.Mathematics;
using Verse;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(GasUtility))]
	[HarmonyPatch(nameof(GasUtility.DoSteadyEffects))]
	internal static class Verse_GasUtility_DoSteadyEffects
	{
		private static readonly object lockObj = new object();

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelPostMapTick.Enabled;
		}

		static bool Prefix(IntVec3 __0, Map __1, out bool __state)
		{
			__state = false;
			if (!ModsConfig.AnomalyActive || __0.GasDensity(__1, GasType.DeadlifeDust) < math.EPSILON)
				return false;
			Monitor.Enter(lockObj, ref __state);
			return true;
		}

		static void Finalizer(bool __state)
		{
			if (__state)
				Monitor.Exit(lockObj);
		}
	}
}