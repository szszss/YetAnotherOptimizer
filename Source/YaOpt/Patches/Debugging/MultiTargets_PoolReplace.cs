using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Verse.AI;
using YaOpt.Helpers;

namespace YaOpt.Patches.Debugging
{
	[HarmonyPatch]
	internal static class MultiTargets_ReservationManager
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(ReservationManager), nameof(ReservationManager.Reserve));
			yield return AccessTools.Method(typeof(ReservationManager), nameof(ReservationManager.Release));
			yield return AccessTools.Method(typeof(ReservationManager), nameof(ReservationManager.ReleaseAllForTarget));
			yield return AccessTools.Method(typeof(ReservationManager), nameof(ReservationManager.ReleaseClaimedBy));
			yield return AccessTools.Method(typeof(ReservationManager), nameof(ReservationManager.ReleaseAllClaimedBy));
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelJobGiver.Enabled;
		}

		static void Prefix()
		{
			if (ParallelJobGiver.WorkerThreadsRunning)
			{
				YaOptMod.Error("A Reservation operation was detected during the execution of " +
							   "the ParallelJobGiver, which is strictly prohibited. " +
							   "You can analyze the call stack to determine which WorkerGiver " +
							   "performed this operation. " +
							   "Please report this compatibility issue to the developers.");
			}
		}
	}
}