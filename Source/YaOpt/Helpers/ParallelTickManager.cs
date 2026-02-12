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
		private static float parellellyTickPawnsWorkerCount = 5f;

		private static readonly ConcurrentQueue<int> jobQueue = new ConcurrentQueue<int>();

		private static readonly List<Pawn> pawns = new List<Pawn>();

		private static int finishedJobCount;

		private static JobHandle postMapTickJobHandle = default;

		private static NativeArray<JobHandle> tmpJobHandles = default;

		private static int gameTick;

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
					pawns.Add(pawn);
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
					pawns.Remove(pawn);
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
			postMapTickJobHandle = default;
			if (!tmpJobHandles.IsCreated || tmpJobHandles.Length != maps.Count * 2)
			{
				tmpJobHandles.Dispose();
				tmpJobHandles = new NativeArray<JobHandle>(maps.Count * 2, Allocator.Persistent);
			}
			var i = 0;
			foreach (var map in maps)
			{
				// Rebuilding any dirty region.
				map.regionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms();
				tmpJobHandles[i++] = new ManagedJob(new SteadyEnvironmentEffectsJob(map.steadyEnvironmentEffects)).Schedule();
				//tmpJobHandles[i++] = new ManagedJob(new TempTerrainManagerJob(map.tempTerrain)).Schedule();
				tmpJobHandles[i++] = new ManagedJob(new GasGridJob(map.gasGrid)).Schedule();
			}
			postMapTickJobHandle = JobHandle.CombineDependencies(tmpJobHandles);
		}

		public static void FinishPostMapTick(int tick)
		{
			postMapTickJobHandle.Complete();
			postMapTickJobHandle = default;
		}

		public static void ParellellyTickPawns()
		{
			foreach (var map in Find.Maps)
			{
				// Rebuilding any dirty region.
				map.regionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms();
			}
			//debugStopwatch.Restart();
			gameTick = GenTicks.TicksGame;
			var pawnCount = pawns.Count;
			var jobCount = (int)(pawnCount + 6);
			//var parallel = math.clamp(Environment.ProcessorCount, 1, 4);
			while (jobQueue.TryDequeue(out _))
			{
			}
			for (var i = 0; i < pawnCount; i++)
			{
				jobQueue.Enqueue(i);
			}
			JobHandle handle = default;
			finishedJobCount = 0;
			handle = new ManagedJobFor(new ParallelPawnJob(jobQueue, pawns, gameTick))
				.ScheduleParallel(jobCount, jobCount / (int)parellellyTickPawnsWorkerCount);
			/*var workerCount = math.clamp((int)math.round(parellellyTickPawnsWorkerCount), 1, 16);
			for (var i = 0; i < workerCount; i++)
			{
				handle = JobHandle.CombineDependencies(handle, 
					new ManagedJob(new ParallelPawnJob(
						jobQueue, pawns, gameTick)).Schedule());

			}*/
			JobHandle.ScheduleBatchedJobs();
			//handle.Complete();
			while (finishedJobCount != pawnCount && !handle.IsCompleted)
			{
			}
			//SpinWait.SpinUntil(() => finishedJobCount == pawnCount || handle.IsCompleted);
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
			gameTick = GenTicks.TicksGame;
			var result = Parallel.ForEach(pawns, new ParallelOptions(){MaxDegreeOfParallelism = Environment.ProcessorCount}, TickPawn);
		}

		private static void TickPawn(Pawn pawn)
		{
			JobPredictor.ProcessPawn(pawn, gameTick);
		}

		private static void ClearCache()
		{
			pawns.Clear();
			JobPredictor.CleanCache();
			postMapTickJobHandle = default;
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
					Interlocked.Increment(ref finishedJobCount);
				}
			}
		}

		private readonly struct SteadyEnvironmentEffectsJob : IJob
		{
			private readonly SteadyEnvironmentEffects steadyEnvironmentEffects;

			public SteadyEnvironmentEffectsJob(SteadyEnvironmentEffects steadyEnvironmentEffects)
			{
				this.steadyEnvironmentEffects = steadyEnvironmentEffects;
			}

			public void Execute()
			{
				try
				{
					steadyEnvironmentEffects.SteadyEnvironmentEffectsTick();
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
			private readonly TempTerrainManager tempTerrainManager;

			public TempTerrainManagerJob(TempTerrainManager tempTerrainManager)
			{
				this.tempTerrainManager = tempTerrainManager;
			}

			public void Execute()
			{
				try
				{
					tempTerrainManager.Tick();
				}
				catch (Exception ex)
				{
					Log.Error(ex.ToString());
				}
			}
		}

		private readonly struct GasGridJob : IJob
		{
			private readonly GasGrid gasGrid;

			public GasGridJob(GasGrid gasGrid)
			{
				this.gasGrid = gasGrid;
			}

			public void Execute()
			{
				try
				{
					gasGrid.Tick();
				}
				catch (Exception ex)
				{
					Log.Error(ex.ToString());
				}
			}
		}
	}
}