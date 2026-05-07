using HarmonyLib;
using System.Runtime.CompilerServices;
using Verse;
using Verse.AI;
using YaOpt.Helpers;

namespace YaOpt.Patches.ThreadSafe
{
	[HarmonyPatch(typeof(ReservationManager))]
	[HarmonyPatch(nameof(ReservationManager.Reserve))]
	[HarmonyPriority(Priority.First)]
	internal static class Verse_AI_ReservationManager_Reserve
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelWorkGiver.Enabled;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static bool Prefix(ReservationManager __instance, ref bool __result,
			Pawn claimant, Job job, LocalTargetInfo target, int maxPawns, int stackCount,
			ReservationLayerDef layer, bool errorOnFailed, bool ignoreOtherReservations, bool canReserversStartJobs)
		{
			if (ReservationPromiser.Working)
			{
				__result = __instance.CanReserve(claimant, target, maxPawns, stackCount, layer,
					ignoreOtherReservations);
				if (__result)
				{
					ReservationPromiser.Promise(__instance, claimant, job, target, maxPawns,
						stackCount, layer, errorOnFailed, ignoreOtherReservations, canReserversStartJobs);
				}
				return false;
			}
			return true;
		}
	}
}