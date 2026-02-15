using Gilzoide.ManagedJobs;
using LudeonTK;
using RimWorld;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Verse;

namespace YaOpt.Helpers
{
	public static class ParallelTickManager
	{
		[TweakValue("exampleCategory", 1f, 16f)]
		private static float _parellellyTickPawnsWorkerCount = 5f;

		private static readonly ConcurrentQueue<int> _jobQueue = new ConcurrentQueue<int>();

		private static readonly List<Pawn> _pawns = new List<Pawn>();

		private static int _finishedJobCount;

		private static JobHandle _postMapTickJobHandle = default;

		private static NativeArray<JobHandle> _tmpJobHandles = default;

		private static int _gameTick;

		/*private static float debugTime;
		private static int debugCount;
		private static Stopwatch debugStopwatch = new Stopwatch();*/

		static ParallelTickManager()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
			UpdateCallbackHelper.RegisterPreRenderCallback(FinishPostMapTick);
			UpdateCallbackHelper.RegisterPreTickCallback(FinishPostMapTick);
		}

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

		public static void ParellellyPreTickMaps(List<Map> maps)
		{
		}

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

		public static void FinishPostMapTick(int tick)
		{
			_postMapTickJobHandle.Complete();
			_postMapTickJobHandle = default;
		}

		public static void ParellellyTickPawns()
		{
			foreach (var map in Find.Maps)
			{
				// Rebuilding any dirty region.
				map.regionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms();
			}
			//debugStopwatch.Restart();
			_gameTick = GenTicks.TicksGame;
			var pawnCount = _pawns.Count;
			var jobCount = (int)(pawnCount + 6);
			//var parallel = math.clamp(Environment.ProcessorCount, 1, 4);
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
			/*var workerCount = math.clamp((int)math.round(_parellellyTickPawnsWorkerCount), 1, 16);
			for (var i = 0; i < workerCount; i++)
			{
				handle = JobHandle.CombineDependencies(handle,
					new ManagedJob(new ParallelPawnJob(
						_jobQueue, _pawns, _gameTick)).Schedule());

			}*/
			JobHandle.ScheduleBatchedJobs();
			//handle.Complete();
			while (_finishedJobCount != pawnCount && !handle.IsCompleted)
			{
			}
			//SpinWait.SpinUntil(() => _finishedJobCount == pawnCount || handle.IsCompleted);
			/*debugStopwatch.Stop();
			debugTime += (float) debugStopwatch.Elapsed.TotalMilliseconds;
			if (++debugCount >= 60)
			{
				var avgTime = debugTime / debugCount * 1000;
				YaOptMod.Warning($"ParallelTick: {avgTime:0}us");
				debugTime = 0;
				debugCount = 0;
			}*/
		}

		public static void SingleTickPawns()
		{
			_gameTick = GenTicks.TicksGame;
			var result = Parallel.ForEach(_pawns, new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount }, TickPawn);
		}

		private static void TickPawn(Pawn pawn)
		{
			JobPredictor.ProcessPawn(pawn, _gameTick);
		}

		private static void ClearCache()
		{
			_pawns.Clear();
			JobPredictor.CleanCache();
			_postMapTickJobHandle = default;
		}

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
		/// It has a race condition with FishShadowComponent because they will modify WaterBody at the same time.
		/// </summary>
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