using HarmonyLib;
using System.Threading;
using Verse;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(PawnCapacitiesHandler))]
	[HarmonyPatch(nameof(PawnCapacitiesHandler.GetLevel))]
	internal static class Verse_PawnCapacitiesHandler_GetLevel
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelThoughtUpdate.Enabled;
		}

		static void Prefix(PawnCapacitiesHandler __instance, out bool __state)
		{
			__state = false;
			Monitor.Enter(__instance, ref __state);
		}

		static void Finalizer(PawnCapacitiesHandler __instance, bool __state)
		{
			if (__state)
				Monitor.Exit(__instance);
		}
	}
}