using HarmonyLib;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(GenAdj))]
	[HarmonyPatch(nameof(GenAdj.TryFindRandomAdjacentCell8WayWithRoom))]
	[HarmonyPatch(new[] { typeof(IntVec3), typeof(Rot4), typeof(IntVec2), typeof(Map), typeof(IntVec3) })]
	internal static class Verse_GenAdj_TryFindRandomAdjacentCell8WayWithRoom
	{
		private static GreedySpinLock _spinLock = new GreedySpinLock();

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
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