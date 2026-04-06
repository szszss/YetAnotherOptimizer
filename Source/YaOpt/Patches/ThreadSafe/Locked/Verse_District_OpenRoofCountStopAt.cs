using HarmonyLib;
using System.Threading;
using Verse;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(District))]
	[HarmonyPatch(nameof(District.OpenRoofCountStopAt))]
	internal static class Verse_District_OpenRoofCountStopAt
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelPawnTick.Enabled &&
				   YaOptGlobal.Settings.ParallelPawnMoodUpdate;
		}

		static void Prefix(District __instance, out bool __state)
		{
			__state = false;
			Monitor.Enter(__instance, ref __state);
		}

		static void Finalizer(District __instance, bool __state)
		{
			if (__state)
				Monitor.Exit(__instance);
		}
	}
}