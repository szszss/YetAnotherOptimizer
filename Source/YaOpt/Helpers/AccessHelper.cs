using HarmonyLib;
using System;
using System.Linq.Expressions;
using System.Reflection;
using Verse;
using Verse.AI;

namespace YaOpt.Helpers
{
	internal static class AccessHelper
	{
		private static FieldInfo fieldTickDelta;

		private static MethodInfo getterMinTickIntervalRate;

		private static MethodInfo getterMaxTickIntervalRate;

		private static MethodInfo getterUpdateRateTickOffset;

		private static MethodInfo methodShouldStartJobFromThinkTree;

		private static MethodInfo getterCurToil;

		private static Func<Thing, int> delegateTickDelta;

		private static Func<Thing, int> delegateMinTickIntervalRate;

		private static Func<Thing, int> delegateMaxTickIntervalRate;

		private static Func<Thing, int> delegateUpdateRateTickOffset;

		private static Func<Pawn_JobTracker, ThinkResult, bool> delegateShouldStartJobFromThinkTree;

		private static Func<JobDriver, Toil> delegateCurToil;

		public static void Init()
		{
			fieldTickDelta = AccessTools.Field(typeof(Thing), "tickDelta");
			getterMinTickIntervalRate = AccessTools.PropertyGetter(typeof(Thing), "MinTickIntervalRate");
			getterMaxTickIntervalRate = AccessTools.PropertyGetter(typeof(Thing), "MaxTickIntervalRate");
			getterUpdateRateTickOffset = AccessTools.PropertyGetter(typeof(Thing), "UpdateRateTickOffset");
			methodShouldStartJobFromThinkTree = AccessTools.Method(typeof(Pawn_JobTracker), "ShouldStartJobFromThinkTree");
			getterCurToil = AccessTools.PropertyGetter(typeof(JobDriver), "CurToil");

			var paramExp = Expression.Parameter(typeof(Thing));
			delegateTickDelta = Expression.Lambda<Func<Thing, int>>(Expression.Field(paramExp, fieldTickDelta), paramExp).Compile();
			delegateMinTickIntervalRate = (Func<Thing, int>)Delegate.CreateDelegate(typeof(Func<Thing, int>), getterMinTickIntervalRate);
			delegateMaxTickIntervalRate = (Func<Thing, int>)Delegate.CreateDelegate(typeof(Func<Thing, int>), getterMaxTickIntervalRate);
			delegateUpdateRateTickOffset = (Func<Thing, int>)Delegate.CreateDelegate(typeof(Func<Thing, int>), getterUpdateRateTickOffset);
			delegateShouldStartJobFromThinkTree = (Func<Pawn_JobTracker, ThinkResult, bool>)Delegate.CreateDelegate(
				typeof(Func<Pawn_JobTracker, ThinkResult, bool>), methodShouldStartJobFromThinkTree);
			delegateCurToil = (Func<JobDriver, Toil>)Delegate.CreateDelegate(typeof(Func<JobDriver, Toil>), getterCurToil);
		}

		public static int Thing_TickDelta(Thing thing)
		{
			if (fieldTickDelta == null || thing == null)
				return 0;
			return delegateTickDelta(thing);
		}

		public static int Thing_MinTickIntervalRate(Thing thing)
		{
			if (getterMinTickIntervalRate == null || thing == null)
				return 1;
			return delegateMinTickIntervalRate(thing);
		}

		public static int Thing_MaxTickIntervalRate(Thing thing)
		{
			if (getterMaxTickIntervalRate == null || thing == null)
				return 15;
			return delegateMaxTickIntervalRate(thing);
		}

		public static int Thing_UpdateRateTickOffset(Thing thing)
		{
			if (getterUpdateRateTickOffset == null || thing == null)
				return 0;
			return delegateUpdateRateTickOffset(thing);
		}

		public static void Thing_TickDeltaAndIntervalRate(Thing thing,
			out int tickDelta, out int minTickIntervalRate, out int maxTickIntervalRate, out int updateRateTickOffset)
		{
			tickDelta = 0;
			minTickIntervalRate = 1;
			maxTickIntervalRate = 15;
			updateRateTickOffset = 0;
			if (thing == null || fieldTickDelta == null ||
			    getterMinTickIntervalRate == null || getterMaxTickIntervalRate == null)
				return;
			tickDelta = delegateTickDelta(thing);
			minTickIntervalRate = delegateMinTickIntervalRate(thing);
			maxTickIntervalRate = delegateMaxTickIntervalRate(thing);
			updateRateTickOffset = delegateUpdateRateTickOffset(thing);
		}

		public static bool Pawn_JobTracker_ShouldStartJobFromThinkTree(Pawn_JobTracker jobTracker,
			ThinkResult thinkResult)
		{
			if (methodShouldStartJobFromThinkTree == null || jobTracker == null)
				return false;
			return delegateShouldStartJobFromThinkTree(jobTracker, thinkResult);
		}

		public static Toil JobDriver_CurToil(JobDriver jobDriver)
		{
			if (getterCurToil == null || jobDriver == null)
				return null;
			return delegateCurToil(jobDriver);
		}
	}
}