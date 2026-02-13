using HarmonyLib;
using System.Threading;
using Verse;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(RegionTraverser))]
	[HarmonyPatch(nameof(RegionTraverser.MarkRegionsBFS))]
	internal static class Verse_RegionTraverser_MarkRegionsBFS
	{
		private static readonly object lockObj = new object();

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
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