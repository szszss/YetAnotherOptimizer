using HarmonyLib;
using PerformanceOptimizer;
using RimWorld.Planet;
using System.Runtime.CompilerServices;
using System.Threading;
using Verse;
using YaOpt.Helpers.ThirdParty;
using YaOpt.Helpers.ThreadSafe;
using static PerformanceOptimizer.ComponentCache;
// ReSharper disable InvokeAsExtensionMethod

namespace YaOpt.OtherMod.PerformanceOptimizer
{
	public static class ThreadSafeGetCompReplacements
	{
		public static bool Enabled = true;

		public static void Init()
		{
			// for TryGetHediffCompFastWithThreadSafe
			Optimization_FasterGetCompReplacement.typesToSkip.Add(
				"YaOpt.OtherMod.PerformanceOptimizer.ThreadSafeGetCompReplacements");

			Optimization_FasterGetCompReplacement.genericMapGetComp = AccessTools.Method(
				typeof(ThreadSafeGetCompReplacements), nameof(GetMapComponentFastWrapper));
			Optimization_FasterGetCompReplacement.genericWorldGetComp = AccessTools.Method(
				typeof(ThreadSafeGetCompReplacements), nameof(GetWorldComponentFastWrapper));
			Optimization_FasterGetCompReplacement.genericGameGetComp = AccessTools.Method(
				typeof(ThreadSafeGetCompReplacements), nameof(GetGameComponentFastWrapper));
			Optimization_FasterGetCompReplacement.genericHediffTryGetComp = AccessTools.Method(
				typeof(ThreadSafeGetCompReplacements), nameof(TryGetHediffCompFastWrapper));
			Optimization_FasterGetCompReplacement.genericWorldObjectGetComp = AccessTools.Method(
				typeof(ThreadSafeGetCompReplacements), nameof(GetWorldObjectCompFastWrapper));
			Optimization_FasterGetCompReplacement.genericThingDefCompProps = AccessTools.Method(
				typeof(ThreadSafeGetCompReplacements), nameof(GetThingDefPropsFastWrapper));
			Optimization_FasterGetCompReplacement.genericHediffDefCompProps = AccessTools.Method(
				typeof(ThreadSafeGetCompReplacements), nameof(GetHediffDefPropsFastWrapper));
		}

		#region GetMapComponentFast

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GetMapComponentFastWrapper<T>(this Map map) where T : MapComponent
		{
			return Enabled ?
				GetMapComponentFastWithThreadSafe<T>(map) :
				GetMapComponentFastWithoutThreadSafe<T>(map);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static T GetMapComponentFastWithoutThreadSafe<T>(this Map map) where T : MapComponent
		{
			return ComponentCache.GetMapComponentFast<T>(map);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static T GetMapComponentFastWithThreadSafe<T>(this Map map) where T : MapComponent
		{
			UnfairRwLock.InstanceOf<T>.Lock.EnterReadLock();
			try
			{
				if (ICache_MapComponent<T>.compsByMap.TryGetValue(map, out var value))
					return value;
			}
			finally
			{
				UnfairRwLock.InstanceOf<T>.Lock.ExitReadLock();
			}
			UnfairRwLock.InstanceOf<T>.Lock.EnterWriteLock();
			try
			{
				return ComponentCache.GetMapComponentFast<T>(map);
			}
			finally
			{
				UnfairRwLock.InstanceOf<T>.Lock.ExitWriteLock();
			}
		}

		#endregion

		#region GetWorldComponentFast

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GetWorldComponentFastWrapper<T>(this World world) where T : WorldComponent
		{
			return Enabled ?
				GetWorldComponentFastWithThreadSafe<T>(world) :
				GetWorldComponentFastWithoutThreadSafe<T>(world);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static T GetWorldComponentFastWithoutThreadSafe<T>(this World world) where T : WorldComponent
		{
			return ComponentCache.GetWorldComponentFast<T>(world);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static T GetWorldComponentFastWithThreadSafe<T>(this World world) where T : WorldComponent
		{
			GreedySpinLock.InstanceOf<T>.Lock.Enter();
			try
			{
				return ComponentCache.GetWorldComponentFast<T>(world);
			}
			finally
			{
				GreedySpinLock.InstanceOf<T>.Lock.Exit();
			}
		}

		#endregion

		#region GetGameComponentFast

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GetGameComponentFastWrapper<T>(this Game game) where T : GameComponent
		{
			return Enabled ?
				GetGameComponentFastWithThreadSafe<T>(game) :
				GetGameComponentFastWithoutThreadSafe<T>(game);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static T GetGameComponentFastWithoutThreadSafe<T>(this Game game) where T : GameComponent
		{
			return ComponentCache.GetGameComponentFast<T>(game);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static T GetGameComponentFastWithThreadSafe<T>(this Game game) where T : GameComponent
		{
			GreedySpinLock.InstanceOf<T>.Lock.Enter();
			try
			{
				return ComponentCache.GetGameComponentFast<T>(game);
			}
			finally
			{
				GreedySpinLock.InstanceOf<T>.Lock.Exit();
			}
		}

		#endregion

		#region TryGetHediffCompFast

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T TryGetHediffCompFastWrapper<T>(this Hediff hd) where T : HediffComp
		{
			return Enabled ?
				TryGetHediffCompFastWithThreadSafe<T>(hd) :
				TryGetHediffCompFastWithoutThreadSafe<T>(hd);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static T TryGetHediffCompFastWithoutThreadSafe<T>(this Hediff hd) where T : HediffComp
		{
			return ComponentCache.TryGetHediffCompFast<T>(hd);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static T TryGetHediffCompFastWithThreadSafe<T>(this Hediff hd) where T : HediffComp
		{
			if (hd == null)
				return null;

			// TryGetHediffCompFast has a very high probability of not triggering the cache
			// (mainly due to HediffComp_Effecter in PawnStatusEffecters.EffectersTick and
			// HediffComp_Invisibility in InvisibilityUtility.GetAlpha).
			// Therefore, if the lock cannot be acquired, it will directly fall back to the original path.
			var hasLock = Monitor.TryEnter(ICache_HediffComp<T>.compsById);
			if (!hasLock)
				return hd.TryGetComp<T>();
			try
			{
				if (ICache_HediffComp<T>.compsById.TryGetValue(hd.loadID, out T value))
					return value;
				if (hd is HediffWithComps hediffWithComps && hediffWithComps.comps != null)
					return ComponentCache.TryGetHediffCompFast<T>(hd);
			}
			finally
			{
				Monitor.Exit(ICache_HediffComp<T>.compsById);
			}
			return null;
		}

		#endregion

		#region GetWorldObjectCompFast

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GetWorldObjectCompFastWrapper<T>(this WorldObject worldObject) where T : WorldObjectComp
		{
			return Enabled ?
				GetWorldObjectCompFastWithThreadSafe<T>(worldObject) :
				GetWorldObjectCompFastWithoutThreadSafe<T>(worldObject);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static T GetWorldObjectCompFastWithoutThreadSafe<T>(this WorldObject worldObject) where T : WorldObjectComp
		{
			return ComponentCache.GetWorldObjectCompFast<T>(worldObject);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static T GetWorldObjectCompFastWithThreadSafe<T>(this WorldObject worldObject) where T : WorldObjectComp
		{
			UnfairRwLock.InstanceOf<T>.Lock.EnterReadLock();
			try
			{
				if (ICache_WorldObjectComp<T>.compsById.TryGetValue(worldObject.ID, out T value))
					return value;
			}
			finally
			{
				UnfairRwLock.InstanceOf<T>.Lock.ExitReadLock();
			}
			UnfairRwLock.InstanceOf<T>.Lock.EnterWriteLock();
			try
			{
				return ComponentCache.GetWorldObjectCompFast<T>(worldObject);
			}
			finally
			{
				UnfairRwLock.InstanceOf<T>.Lock.ExitWriteLock();
			}
		}

		#endregion

		#region GetThingDefPropsFast

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GetThingDefPropsFastWrapper<T>(this ThingDef thingDef) where T : CompProperties
		{
			return Enabled ?
				GetThingDefPropsFastWithThreadSafe<T>(thingDef) :
				GetThingDefPropsFastWithoutThreadSafe<T>(thingDef);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static T GetThingDefPropsFastWithoutThreadSafe<T>(this ThingDef thingDef) where T : CompProperties
		{
			return ComponentCache.GetThingDefPropsFast<T>(thingDef);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static T GetThingDefPropsFastWithThreadSafe<T>(this ThingDef thingDef) where T : CompProperties
		{
			UnfairRwLock.InstanceOf<T>.Lock.EnterReadLock();
			try
			{
				if (ICache_ThingDefProps<T>.compPropsById.TryGetValue(thingDef.shortHash, out var value))
					return value;
			}
			finally
			{
				UnfairRwLock.InstanceOf<T>.Lock.ExitReadLock();
			}
			UnfairRwLock.InstanceOf<T>.Lock.EnterWriteLock();
			try
			{
				return ComponentCache.GetThingDefPropsFast<T>(thingDef);
			}
			finally
			{
				UnfairRwLock.InstanceOf<T>.Lock.ExitWriteLock();
			}
		}

		#endregion

		#region GetHediffDefPropsFast

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GetHediffDefPropsFastWrapper<T>(this HediffDef hediffDef) where T : HediffCompProperties
		{
			return Enabled ?
				GetHediffDefPropsFastWithThreadSafe<T>(hediffDef) :
				GetHediffDefPropsFastWithoutThreadSafe<T>(hediffDef);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static T GetHediffDefPropsFastWithoutThreadSafe<T>(this HediffDef hediffDef) where T : HediffCompProperties
		{
			return ComponentCache.GetHediffDefPropsFast<T>(hediffDef);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static T GetHediffDefPropsFastWithThreadSafe<T>(this HediffDef hediffDef) where T : HediffCompProperties
		{
			if (hediffDef.comps == null)
			{
				return null;
			}

			UnfairRwLock.InstanceOf<T>.Lock.EnterReadLock();
			try
			{
				if (ICache_HediffDefProps<T>.compPropsById.TryGetValue(hediffDef.shortHash, out T value))
					return value;
			}
			finally
			{
				UnfairRwLock.InstanceOf<T>.Lock.ExitReadLock();
			}
			UnfairRwLock.InstanceOf<T>.Lock.EnterWriteLock();
			try
			{
				return ComponentCache.GetHediffDefPropsFast<T>(hediffDef);
			}
			finally
			{
				UnfairRwLock.InstanceOf<T>.Lock.ExitWriteLock();
			}
		}

		#endregion
	}
}