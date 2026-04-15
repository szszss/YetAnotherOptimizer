using HarmonyLib;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(CellFinder))]
	[HarmonyPatch(nameof(CellFinder.TryFindRandomCellInsideWith))]
	internal static class Verse_CellFinder_TryFindRandomCellInsideWith
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