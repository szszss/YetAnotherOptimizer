using Gilzoide.ManagedJobs;
using LudeonTK;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Unity.Jobs;
using Verse;

namespace YaOpt.Helpers
{
	/// <summary>
	/// Manages parallel pawn ticking, provides multi-threaded processing for
	/// pre-calculates whether pawn jobs will fail or need attention.
	/// </summary>
	public static class ParallelPawnTickManager
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
		/// Current game tick, cached to avoid race conditions during parallel processing.
		/// </summary>
		private static int _gameTick;

		static ParallelPawnTickManager()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
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
		/// Performs parallel tick processing for all tracked pawns.
		/// </summary>
		/// <seealso cref="JobPredictor.ProcessPawn"/>
		public static void ParellellyTickPawns()
		{
			foreach (var map in Find.Maps)
			{
				// Rebuilding any dirty region.
				map.regionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms();
				// Ensure factions lists init in main thread.
				map.mapPawns.SpawnedPawnsInFaction(null);
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
			YaOptGlobal.IsParallelRunningInTick = true;
			JobHandle handle = default;
			_finishedJobCount = 0;
			handle = new ManagedJobFor(new ParallelPawnJob(_jobQueue, _pawns, _gameTick))
				.ScheduleParallel(jobCount, jobCount / (int)_parellellyTickPawnsWorkerCount);
			JobHandle.ScheduleBatchedJobs();
			while (_finishedJobCount != pawnCount && !handle.IsCompleted)
			{
			}
			YaOptGlobal.IsParallelRunningInTick = false;
		}

		/// <summary>
		/// Clears all cached data and resets state.
		/// </summary>
		private static void ClearCache()
		{
			_pawns.Clear();
			JobPredictor.CleanCache();
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
	}
}