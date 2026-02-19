using HarmonyLib;
using System;
using System.Linq.Expressions;
using System.Reflection;
using Verse;
using Verse.AI;

namespace YaOpt.Helpers
{
	/// <summary>
	/// Cached delegates for fast access to private/internal RimWorld members.
	/// </summary>
	/// <remarks>
	/// Uses compiled expression trees and delegate creation to avoid reflection overhead in hot paths.
	/// </remarks>
	internal static class AccessHelper
	{
		private static FieldInfo _fieldTickDelta;

		private static MethodInfo _getterMinTickIntervalRate;

		private static MethodInfo _getterMaxTickIntervalRate;

		private static MethodInfo _getterUpdateRateTickOffset;

		private static MethodInfo _methodShouldStartJobFromThinkTree;

		private static MethodInfo _getterCurToil;

		private static Func<Thing, int> _delegateTickDelta;

		private static Func<Thing, int> _delegateMinTickIntervalRate;

		private static Func<Thing, int> _delegateMaxTickIntervalRate;

		private static Func<Thing, int> _delegateUpdateRateTickOffset;

		private static Func<Pawn_JobTracker, ThinkResult, bool> _delegateShouldStartJobFromThinkTree;

		private static Func<JobDriver, Toil> _delegateCurToil;

		public static void Init()
		{
			_fieldTickDelta = AccessTools.Field(typeof(Thing), "tickDelta");
			_getterMinTickIntervalRate = AccessTools.PropertyGetter(typeof(Thing), "MinTickIntervalRate");
			_getterMaxTickIntervalRate = AccessTools.PropertyGetter(typeof(Thing), "MaxTickIntervalRate");
			_getterUpdateRateTickOffset = AccessTools.PropertyGetter(typeof(Thing), "UpdateRateTickOffset");
			_methodShouldStartJobFromThinkTree = AccessTools.Method(typeof(Pawn_JobTracker), "ShouldStartJobFromThinkTree");
			_getterCurToil = AccessTools.PropertyGetter(typeof(JobDriver), "CurToil");

			var paramExp = Expression.Parameter(typeof(Thing));
			_delegateTickDelta = Expression.Lambda<Func<Thing, int>>(Expression.Field(paramExp, _fieldTickDelta), paramExp).Compile();
			_delegateMinTickIntervalRate = (Func<Thing, int>)Delegate.CreateDelegate(typeof(Func<Thing, int>), _getterMinTickIntervalRate);
			_delegateMaxTickIntervalRate = (Func<Thing, int>)Delegate.CreateDelegate(typeof(Func<Thing, int>), _getterMaxTickIntervalRate);
			_delegateUpdateRateTickOffset = (Func<Thing, int>)Delegate.CreateDelegate(typeof(Func<Thing, int>), _getterUpdateRateTickOffset);
			_delegateShouldStartJobFromThinkTree = (Func<Pawn_JobTracker, ThinkResult, bool>)Delegate.CreateDelegate(
				typeof(Func<Pawn_JobTracker, ThinkResult, bool>), _methodShouldStartJobFromThinkTree);
			_delegateCurToil = (Func<JobDriver, Toil>)Delegate.CreateDelegate(typeof(Func<JobDriver, Toil>), _getterCurToil);
		}

		public static int Thing_TickDelta(Thing thing)
		{
			if (_fieldTickDelta == null || thing == null)
				return 0;
			return _delegateTickDelta(thing);
		}

		public static int Thing_MinTickIntervalRate(Thing thing)
		{
			if (_getterMinTickIntervalRate == null || thing == null)
				return 1;
			return _delegateMinTickIntervalRate(thing);
		}

		public static int Thing_MaxTickIntervalRate(Thing thing)
		{
			if (_getterMaxTickIntervalRate == null || thing == null)
				return 15;
			return _delegateMaxTickIntervalRate(thing);
		}

		public static int Thing_UpdateRateTickOffset(Thing thing)
		{
			if (_getterUpdateRateTickOffset == null || thing == null)
				return 0;
			return _delegateUpdateRateTickOffset(thing);
		}

		public static void Thing_TickDeltaAndIntervalRate(Thing thing,
			out int tickDelta, out int minTickIntervalRate, out int maxTickIntervalRate, out int updateRateTickOffset)
		{
			tickDelta = 0;
			minTickIntervalRate = 1;
			maxTickIntervalRate = 15;
			updateRateTickOffset = 0;
			if (thing == null || _fieldTickDelta == null ||
				_getterMinTickIntervalRate == null || _getterMaxTickIntervalRate == null)
				return;
			tickDelta = _delegateTickDelta(thing);
			minTickIntervalRate = _delegateMinTickIntervalRate(thing);
			maxTickIntervalRate = _delegateMaxTickIntervalRate(thing);
			updateRateTickOffset = _delegateUpdateRateTickOffset(thing);
		}

		public static bool Pawn_JobTracker_ShouldStartJobFromThinkTree(Pawn_JobTracker jobTracker,
			ThinkResult thinkResult)
		{
			if (_methodShouldStartJobFromThinkTree == null || jobTracker == null)
				return false;
			return _delegateShouldStartJobFromThinkTree(jobTracker, thinkResult);
		}

		public static Toil JobDriver_CurToil(JobDriver jobDriver)
		{
			if (_getterCurToil == null || jobDriver == null)
				return null;
			return _delegateCurToil(jobDriver);
		}
	}
}