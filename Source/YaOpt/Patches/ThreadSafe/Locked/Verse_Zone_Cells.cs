using HarmonyLib;
using System.Threading;
using Verse;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	/// <summary>
	/// This is an attempt to fix a very rare but serious bug.
	/// When two threads access Cells simultaneously and perform a shuffle on it,
	/// elements in the internal array can become corrupted due to non-atomic reads
	/// and writes, causing the entire Zone to be corrupted permanently,
	/// which can only be repaired by reading an old save.
	/// </summary>
	[HarmonyPatch(typeof(Zone))]
	[HarmonyPatch(nameof(Zone.Cells), MethodType.Getter)]
	internal static class Verse_Zone_Cells
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		static void Prefix(Zone __instance, out bool __state)
		{
			__state = false;
			Monitor.Enter(__instance, ref __state);
		}

		static void Finalizer(Zone __instance, bool __state)
		{
			if (__state)
				Monitor.Exit(__instance);
		}
	}
}