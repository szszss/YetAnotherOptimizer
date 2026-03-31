using HarmonyLib;
using RimWorld;
using System.Threading;
using Verse;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(CostListCalculator))]
	[HarmonyPatch(nameof(CostListCalculator.CostListAdjusted))]
	[HarmonyPatch(new[] { typeof(BuildableDef), typeof(ThingDef), typeof(bool) })]
	internal static class RimWorld_CostListCalculator_CostListAdjusted
	{
		private static SpinLock _spinLock = new SpinLock();

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelJobGiver.Enabled;
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