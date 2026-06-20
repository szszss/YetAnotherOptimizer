using FacialAnimation;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.Jobs;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.OtherMod.FacialAnimation.Helpers
{
	/// <summary>
	/// Moves facial animation updates from the main thread to the parallel render preparation phase.
	/// <br/>
	/// This helper manages the thread-safe execution of facial animation updates, ensuring that
	/// multiple pawns can be processed concurrently without race conditions.
	/// </summary>
	internal static class ParallelUpdateHelper
	{
		public static bool Enabled;

		private static JobHandle _jobHandle = default;

		private static volatile bool _jobRunning = false;

		private static readonly List<Pawn> _pendingPawns = new List<Pawn>();

		private static int _lastClearTick = -1;

		// This is actually a concurrent hashset since there is not ConcurrentSet in .net
		// Key is thingIdNumber and value is the tick of its last update.
		// Used to prevent updating the same pawn multiple times in the same frame/tick.
		private static readonly ConcurrentDictionary<int, int> _updatedPawns
			= new ConcurrentDictionary<int, int>();

		static ParallelUpdateHelper()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
			UpdateCallbackHelper.RegisterPreRenderCallback(PreRender);
			UpdateCallbackHelper.RegisterPreDynamicDrawCallback(CompleteJob);
			UpdateCallbackHelper.RegisterPostRenderCallback(CompleteJob);
		}

		private static void ClearCache()
		{
			_jobHandle = default;
			_pendingPawns.Clear();
			_updatedPawns.Clear();
			_lastClearTick = -1;
		}

		private static void PreRender(int tick)
		{
			if (!Enabled)
				return;

			if (_pendingPawns.Count > 0)
			{
				_jobRunning = true;
				_jobHandle = new YaOptManagedJobs.JobFor(new UpdateFacialAnimationJob(_pendingPawns))
					.ScheduleParallel(_pendingPawns.Count, 1);
			}
		}

		private static void CompleteJob(int tick)
		{
			if (_jobRunning)
			{
				_jobHandle.CompleteWithSpinWait();
				_jobRunning = false;
				_pendingPawns.Clear();
			}

			if (_lastClearTick == -1)
			{
				_lastClearTick = tick;
			}
			if (tick - _lastClearTick > 18000)
			{
				_lastClearTick = tick;
				_updatedPawns.Clear();
			}
		}

		private static bool ShouldPawnUpdate(Pawn pawn, int currentTick)
		{
			var key = pawn.thingIDNumber;
			if (_updatedPawns.TryGetValue(key, out var lastUpdateTick))
			{
				if (lastUpdateTick == currentTick)
					return false;
				if (!_updatedPawns.TryUpdate(key, currentTick, lastUpdateTick))
					return false;
			}
			else if (!_updatedPawns.TryAdd(key, currentTick))
			{
				return false;
			}
			return true;
		}

		/// <summary>
		/// Manually triggers a facial animation update for a specific pawn.
		/// <br/>
		/// Primarily used by <see cref="Patches.RimWorld_PortraitsCache_SetDirty"/> to ensure
		/// that pawns in portraits (which might not be part of the regular map update loop)
		/// have their facial animations correctly updated when the cache is dirtied.
		/// </summary>
		public static void AddPendingPawn(Pawn pawn)
		{
			if (_jobRunning)
			{
				YaOptMod.Error("Try to add a pending pawn while the updating facial animation job is running.\n" +
							   $"Pawn: {pawn}");
				return;
			}
			_pendingPawns.Add(pawn);
		}

		public static void UpdateFacialAnimation(Pawn pawn)
		{
			if (pawn.TryGetComp<FacialAnimationControllerComp>(out var comp))
			{
				var currentTick = Find.TickManager.TicksGame;
				if (!ShouldPawnUpdate(pawn, currentTick))
					return;
				if (!comp.CheckUpdateableInitial())
					return;
				comp.UpdateStatus(currentTick);
				comp.UpdateAnimation();
			}
		}

		public static void UpdateFacialAnimation(Corpse corpse)
		{
			UpdateFacialAnimation(corpse.InnerPawn);
		}

		private class UpdateFacialAnimationJob : IJobFor
		{
			private readonly List<Pawn> _pawns;

			public UpdateFacialAnimationJob(List<Pawn> _pendingPawns)
			{
				_pawns = _pendingPawns;
			}

			public void Execute(int index)
			{
				UpdateFacialAnimation(_pawns[index]);
			}
		}
	}
}