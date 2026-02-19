using HarmonyLib;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;
using Verse.AI;

namespace YaOpt.Helpers
{
	/// <summary>
	/// Helper methods for Thing and JobDriver operations used in parallel tick processing.
	/// </summary>
	internal static class ThingHelper
	{
		private delegate Toil GetCurToilDelegate(JobDriver jobDriver);
		private delegate bool ShouldStartJobFromThinkTreeDelegate(Pawn_JobTracker jobTracker, ThinkResult thinkResult);

		private static GetCurToilDelegate getCurToil;
		private static ShouldStartJobFromThinkTreeDelegate shouldStartJobFromThinkTree;

		/// <summary>
		/// Initializes delegate caches for reflection-based access.
		/// </summary>
		public static void Init()
		{
			getCurToil = AccessTools.MethodDelegate<GetCurToilDelegate>(
				AccessTools.PropertyGetter(typeof(JobDriver), "CurToil"), null, false, null);

			shouldStartJobFromThinkTree = AccessTools.MethodDelegate<ShouldStartJobFromThinkTreeDelegate>(
				AccessTools.Method(typeof(Pawn_JobTracker), "ShouldStartJobFromThinkTree"), null, false, null);
		}

		/// <summary>
		/// Checks if a thing should tick at the current interval based on its update rate.
		/// </summary>
		public static bool ShouldTickInterval(Thing thing, out int tickDeltaPlusOne)
		{
			AccessHelper.Thing_TickDeltaAndIntervalRate(thing,
				out tickDeltaPlusOne, out var minTickIntervalRate, out var maxTickIntervalRate, out var updateRateTickOffset);
			tickDeltaPlusOne++;
			int num = Mathf.Min(Mathf.Max(thing.UpdateRateTicks, minTickIntervalRate), maxTickIntervalRate);
			return tickDeltaPlusOne >= num || GenTicks.IsTickInterval(updateRateTickOffset, num);
		}

		/// <summary>
		/// Gets the current toil from a JobDriver via cached delegate.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Toil GetCurToil(JobDriver jobDriver)
		{
			return getCurToil(jobDriver);
		}

		/// <summary>
		/// Checks if a job should be started from think tree results via cached delegate.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ShouldStartJobFromThinkTree(Pawn_JobTracker jobTracker, ThinkResult thinkResult)
		{
			return shouldStartJobFromThinkTree(jobTracker, thinkResult);
		}
	}
}