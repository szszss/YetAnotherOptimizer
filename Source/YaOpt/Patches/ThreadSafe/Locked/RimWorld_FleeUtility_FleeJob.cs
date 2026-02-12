using HarmonyLib;
using RimWorld;
using System.Threading;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(FleeUtility))]
	[HarmonyPatch(nameof(FleeUtility.FleeJob))]
	internal static class RimWorld_FleeUtility_FleeJob
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