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

		private delegate int MinTickIntervalRateDelegate(Thing thing);

		private delegate int MaxTickIntervalRateDelegate(Thing thing);

		private delegate int UpdateRateTickOffsetDelegate(Thing thing);

		private static readonly GetCurToilDelegate getCurToil =
			AccessTools.MethodDelegate<GetCurToilDelegate>(
				AccessTools.PropertyGetter(typeof(JobDriver), "CurToil"), null, false, null);

		private static readonly ShouldStartJobFromThinkTreeDelegate shouldStartJobFromThinkTree =
			AccessTools.MethodDelegate<ShouldStartJobFromThinkTreeDelegate>(
				AccessTools.Method(typeof(Pawn_JobTracker), "ShouldStartJobFromThinkTree"), null, false, null);

		private static readonly MinTickIntervalRateDelegate _minTickIntervalRate =
			AccessTools.MethodDelegate<MinTickIntervalRateDelegate>(
				AccessTools.PropertyGetter(typeof(Thing), "MinTickIntervalRate"));

		private static readonly MaxTickIntervalRateDelegate _maxTickIntervalRate =
			AccessTools.MethodDelegate<MaxTickIntervalRateDelegate>(
				AccessTools.PropertyGetter(typeof(Thing), "MaxTickIntervalRate"));

		private static readonly UpdateRateTickOffsetDelegate _updateRateTickOffset =
			AccessTools.MethodDelegate<UpdateRateTickOffsetDelegate>(
				AccessTools.PropertyGetter(typeof(Thing), "UpdateRateTickOffset"));

		private static readonly AccessTools.FieldRef<Thing, int> _tickDelta =
			AccessTools.FieldRefAccess<int>(typeof(Thing), "tickDelta");

		/// <summary>
		/// Checks if a thing should tick at the current interval based on its update rate.
		/// </summary>
		public static bool ShouldTickInterval(Thing thing, out int tickDeltaPlusOne)
		{
			tickDeltaPlusOne = _tickDelta(thing) + 1;
			var minTickIntervalRate = _minTickIntervalRate(thing);
			var maxTickIntervalRate = _maxTickIntervalRate(thing);
			var updateRateTickOffset = _updateRateTickOffset(thing);
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