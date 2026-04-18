using HarmonyLib;
using RimWorld;
using Verse;
using YaOpt.Helpers.ThreadSafe;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(GenConstruct))]
	[HarmonyPatch(nameof(GenConstruct.CanConstruct))]
	[HarmonyPatch(new[] { typeof(Thing), typeof(Pawn), typeof(bool), typeof(bool), typeof(JobDef) })]
	internal static class RimWorld_GenConstruct_CanConstruct
	{
		private static GreedySpinLock _spinLock = new GreedySpinLock();

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