using Gilzoide.ManagedJobs;
using LudeonTK;
using RimWorld;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using Verse;

namespace YaOpt.Helpers
{
	/// <summary>
	/// Manages parallel execution of tick-based game logic to improve TPS (ticks per second).
	/// </summary>
	/// <remarks>
	/// <para>
	/// This class provides multi-threaded processing for:
	/// <list type="bullet">
	/// <item>Pawn tick prediction - Pre-calculates whether pawn jobs will fail or need attention.</item>
	/// <item>Map post-tick processing - Runs environment effects and gas grid updates in parallel.</item>
	/// </list>
	/// </para>
	/// </remarks>
	public static class ParallelTickManager
	{
		/// <summary>
		/// Controls the number of worker threads for parallel pawn tick processing.
		/// </summary>
		// TODO: remove this from release
		[TweakValue("exampleCategory", 1f, 16f)]
		private static float _parellellyTickPawnsWorkerCount = 5f;

		/// <summary>
		/// Thread-safe queue distributing pawn indices to worker threads.
		/// </summary>
		private static readonly ConcurrentQueue<int> _jobQueue = new ConcurrentQueue<int>();

		/// <summary>
		/// List of all pawns that need parallel tick processing.
		/// </summary>
		/// <remarks>
		/// Not thread-safe for writes; only modify from main thread via <see cref="AddThings"/> and <see cref="RemoveThings"/>.
		/// </remarks>
		private static readonly List<Pawn> _pawns = new List<Pawn>();

		/// <summary>
		/// Counter tracking completed pawn tick jobs for synchronization.
		/// </summary>
		private static int _finishedJobCount;

		/// <summary>
		/// Handle for the parallel map post-tick job.
		/// </summary>
		private static JobHandle _postMapTickJobHandle = default;

		/// <summary>
		/// Reusable array for job handles when processing multiple maps.
		/// </summary>
		private static NativeArray<JobHandle> _tmpJobHandles = default;

		/// <summary>
		/// Current game tick, cached to avoid race conditions during parallel processing.
		/// </summary>
		private static int _gameTick;

		/// <summary>
		/// Static constructor that registers callbacks with the update system.
		/// </summary>
		/// <remarks>
		/// Registers:
		/// <list type="bullet">
		/// <item>Clear cache callback - Called when loading saves.</item>
		/// <item>Pre-render callback - Completes pending post-map-tick jobs before rendering.</item>
		/// <item>Pre-tick callback - Completes pending post-map-tick jobs before the next tick.</item>
		/// </list>
		/// </remarks>
		static ParallelTickManager()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
			UpdateCallbackHelper.RegisterPreRenderCallback(FinishPostMapTick);
			UpdateCallbackHelper.RegisterPreTickCallback(FinishPostMapTick);
		}

		/// <summary>
		/// Adds pawns to the parallel tick processing list.
		/// </summary>
		public static void AddThings(List<Thing> things)
		{
			foreach (var thing in things)
			{
				if (thing is Pawn pawn)
				{
					_pawns.Add(pawn);
					JobPredictor.AddPawn(pawn);
				}
			}
		}

		/// <summary>
		/// Removes pawns from the parallel tick processing list.
		/// </summary>
		public static void RemoveThings(List<Thing> things)
		{
			foreach (var thing in things)
			{
				if (thing is Pawn pawn)
				{
					_pawns.Remove(pawn);
					JobPredictor.RemovePawn(pawn);
				}
			}
		}

		/// <summary>
		/// Performs parallel pre-tick processing for all maps.
		/// </summary>
		/// <remarks>
		/// Currently a placeholder for future pre-tick parallelization opportunities.
		/// </remarks>
		public static void ParellellyPreTickMaps(List<Map> maps)
		{
		}

		/// <summary>
		/// Schedules parallel post-tick processing for all maps.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Runs the following in parallel for each map:
		/// <list type="bullet">
		/// <item><see cref="SteadyEnvironmentEffects.SteadyEnvironmentEffectsTick"/> - Environmental effects like temperature.</item>
		/// <item><see cref="GasGrid.Tick"/> - Gas simulation.</item>
		/// </list>
		/// </para>
		/// </remarks>
		public static void ParellellyPostTickMaps()
		{
			var maps = Find.Maps;
			_postMapTickJobHandle = default;
			if (!_tmpJobHandles.IsCreated || _tmpJobHandles.Length != maps.Count * 2)
			{
				_tmpJobHandles.Dispose();
				_tmpJobHandles = new NativeArray<JobHandle>(maps.Count * 2, Allocator.Persistent);
			}
			var i = 0;
			foreach (var map in maps)
			{
				// Rebuilding any dirty region.
				map.regionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms();
				_tmpJobHandles[i++] = new ManagedJob(new SteadyEnvironmentEffectsJob(map.steadyEnvironmentEffects)).Schedule();
				//_tmpJobHandles[i++] = new ManagedJob(new TempTerrainManagerJob(map.tempTerrain)).Schedule();
				_tmpJobHandles[i++] = new ManagedJob(new GasGridJob(map.gasGrid)).Schedule();
			}
			_postMapTickJobHandle = JobHandle.CombineDependencies(_tmpJobHandles);
		}

		/// <summary>
		/// Completes the pending post-map-tick job.
		/// </summary>
		/// <remarks>
		/// Called before rendering and before the next tick to ensure all parallel work is complete.
		/// </remarks>
		public static void FinishPostMapTick(int tick)
		{
			_postMapTickJobHandle.Complete();
			_postMapTickJobHandle = default;
		}

		/// <summary>
		/// Performs parallel tick processing for all tracked pawns.
		/// </summary>
		/// <seealso cref="JobPredictor.ProcessPawn"/>
		public static void ParellellyTickPawns()
		{
			foreach (var map in Find.Maps)
			{
				// Rebuilding any dirty region.
				map.regionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms();
			}
			_gameTick = GenTicks.TicksGame;
			var pawnCount = _pawns.Count;
			var jobCount = (int)(pawnCount + 6);
			while (_jobQueue.TryDequeue(out _))
			{
			}
			for (var i = 0; i < pawnCount; i++)
			{
				_jobQueue.Enqueue(i);
			}
			JobHandle handle = default;
			_finishedJobCount = 0;
			handle = new ManagedJobFor(new ParallelPawnJob(_jobQueue, _pawns, _gameTick))
				.ScheduleParallel(jobCount, jobCount / (int)_parellellyTickPawnsWorkerCount);
			JobHandle.ScheduleBatchedJobs();
			while (_finishedJobCount != pawnCount && !handle.IsCompleted)
			{
			}
		}

		/// <summary>
		/// Clears all cached data and resets state.
		/// </summary>
		private static void ClearCache()
		{
			_pawns.Clear();
			JobPredictor.CleanCache();
			_postMapTickJobHandle = default;
		}

		/// <summary>
		/// Unity Job that processes pawns from a shared queue using work-stealing.
		/// </summary>
		/// <remarks>
		/// Each job instance repeatedly dequeues pawn indices from the shared queue
		/// until no work remains. This allows automatic load balancing across threads.
		/// </remarks>
		private readonly struct ParallelPawnJob : IJobFor
		{
			private readonly ConcurrentQueue<int> _jobQueue;
			private readonly List<Pawn> _pawns;
			private readonly int _gameTick;

			public ParallelPawnJob(ConcurrentQueue<int> jobQueue, List<Pawn> pawns, int gameTick)
			{
				_jobQueue = jobQueue;
				_pawns = pawns;
				_gameTick = gameTick;
			}

			public void Execute(int _)
			{
				if (_jobQueue.TryDequeue(out var jobIndex))
				{
					JobPredictor.ProcessPawn(_pawns[jobIndex], _gameTick);
					Interlocked.Increment(ref _finishedJobCount);
				}
			}
		}

		/// <summary>
		/// Unity Job that runs <see cref="SteadyEnvironmentEffects.SteadyEnvironmentEffectsTick"/> on a worker thread.
		/// </summary>
		private readonly struct SteadyEnvironmentEffectsJob : IJob
		{
			private readonly SteadyEnvironmentEffects _steadyEnvironmentEffects;

			public SteadyEnvironmentEffectsJob(SteadyEnvironmentEffects steadyEnvironmentEffects)
			{
				_steadyEnvironmentEffects = steadyEnvironmentEffects;
			}

			public void Execute()
			{
				try
				{
					_steadyEnvironmentEffects.SteadyEnvironmentEffectsTick();
				}
				catch (Exception ex)
				{
					Log.Error(ex.ToString());
				}
			}
		}

		/// <summary>
		/// Unity Job that runs <see cref="TempTerrainManager.Tick"/> on a worker thread.
		/// </summary>
		/// <remarks>
		/// <b>Obsolete:</b> Has a race condition with FishShadowComponent which modifies WaterBody concurrently.
		/// </remarks>
		[Obsolete]
		private readonly struct TempTerrainManagerJob : IJob
		{
			private readonly TempTerrainManager _tempTerrainManager;

			public TempTerrainManagerJob(TempTerrainManager tempTerrainManager)
			{
				_tempTerrainManager = tempTerrainManager;
			}

			public void Execute()
			{
				try
				{
					_tempTerrainManager.Tick();
				}
				catch (Exception ex)
				{
					Log.Error(ex.ToString());
				}
			}
		}

		/// <summary>
		/// Unity Job that runs <see cref="GasGrid.Tick"/> on a worker thread.
		/// </summary>
		private readonly struct GasGridJob : IJob
		{
			private readonly GasGrid _gasGrid;

			public GasGridJob(GasGrid gasGrid)
			{
				_gasGrid = gasGrid;
			}

			public void Execute()
			{
				try
				{
					_gasGrid.Tick();
				}
				catch (Exception ex)
				{
					Log.Error(ex.ToString());
				}
			}
		}
	}
}