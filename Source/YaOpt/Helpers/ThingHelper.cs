using HarmonyLib;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;
using Verse.AI;

namespace YaOpt.Helpers
{
	internal static class ThingHelper
	{
		private delegate Toil GetCurToilDelegate(JobDriver jobDriver);

		private delegate bool ShouldStartJobFromThinkTreeDelegate(Pawn_JobTracker jobTracker, ThinkResult thinkResult);

		private static GetCurToilDelegate getCurToil;

		private static ShouldStartJobFromThinkTreeDelegate shouldStartJobFromThinkTree;

		public static void Init()
		{
			getCurToil = AccessTools.MethodDelegate<GetCurToilDelegate>(
				AccessTools.PropertyGetter(typeof(JobDriver), "CurToil"), null, false, null);

			shouldStartJobFromThinkTree = AccessTools.MethodDelegate<ShouldStartJobFromThinkTreeDelegate>(
				AccessTools.Method(typeof(Pawn_JobTracker), "ShouldStartJobFromThinkTree"), null, false, null);
		}

		public static bool ShouldTickInterval(Thing thing, out int tickDeltaPlusOne)
		{
			AccessHelper.Thing_TickDeltaAndIntervalRate(thing, 
				out tickDeltaPlusOne, out var minTickIntervalRate, out var maxTickIntervalRate, out var updateRateTickOffset);
			tickDeltaPlusOne++;
			int num = Mathf.Min(Mathf.Max(thing.UpdateRateTicks, minTickIntervalRate), maxTickIntervalRate);
			return tickDeltaPlusOne >= num || GenTicks.IsTickInterval(updateRateTickOffset, num);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Toil GetCurToil(JobDriver jobDriver)
		{
			return getCurToil(jobDriver);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ShouldStartJobFromThinkTree(Pawn_JobTracker jobTracker, ThinkResult thinkResult)
		{
			return shouldStartJobFromThinkTree(jobTracker, thinkResult);
		}
	}
}