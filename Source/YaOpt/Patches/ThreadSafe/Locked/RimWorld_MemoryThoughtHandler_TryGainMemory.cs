using HarmonyLib;
using RimWorld;
using System.Threading;
using Verse;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(MemoryThoughtHandler))]
	[HarmonyPatch(nameof(MemoryThoughtHandler.TryGainMemory), typeof(Thought_Memory), typeof(Pawn))]
	internal static class RimWorld_MemoryThoughtHandler_TryGainMemory
	{
		// Must be reentrant. Don't use spin lock.

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelThoughtUpdate.Enabled;
		}

		[HarmonyPriority(Priority.VeryHigh)]
		static void Prefix(MemoryThoughtHandler __instance, out bool __state)
		{
			__state = false;
			Monitor.Enter(__instance, ref __state);
		}

		[HarmonyPriority(Priority.VeryLow)]
		static void Finalizer(MemoryThoughtHandler __instance, bool __state)
		{
			if (__state)
				Monitor.Exit(__instance);
		}
	}
}