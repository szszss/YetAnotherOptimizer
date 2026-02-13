using HarmonyLib;
using System.Threading;
using Verse;

namespace YaOpt.Patches.Compatibility.PerformanceOptimizer
{
	[HarmonyPatch(typeof(HediffSet))]
	[HarmonyPatch(nameof(HediffSet.HasImmunizableNotImmuneHediff))]
	internal static class Verse_HediffSet_HasImmunizableNotImmuneHediff
	{
		private static readonly object lockObj = new object();

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe && YaOptGlobal.HasMod("Taranchuk.PerformanceOptimizer");
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