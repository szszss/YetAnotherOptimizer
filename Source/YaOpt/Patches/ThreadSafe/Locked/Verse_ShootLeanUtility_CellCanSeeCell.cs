using HarmonyLib;
using System.Threading;
using Verse;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(ShootLeanUtility))]
	[HarmonyPatch(nameof(ShootLeanUtility.CellCanSeeCell))]
	internal static class Verse_ShootLeanUtility_CellCanSeeCell
	{
		// In fact, there are currently no known multithreading conflict yet. This is just a precaution.
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