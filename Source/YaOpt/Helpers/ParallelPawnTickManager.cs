using System;
using Gilzoide.ManagedJobs;
using LudeonTK;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Unity.Jobs;
using UnityEngine;
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
		private static float _parellellyTickPawnsWorkerCount = 8f;

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

#if DEBUG
		private static Stopwatch _stopwatch;

		private static ConcurrentQueue<string> _debugOutputs;

		private static bool _debugLog = false;

		[DebugAction("YaOpt", "Record next job prediction",
			actionType = DebugActionType.Action,
			allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void RecordNextRun()
		{
			_debugLog = true;
		}
#endif

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
#if DEBUG
			if (_debugLog)
			{
				_stopwatch = new Stopwatch();
				_stopwatch.Start();
				_debugOutputs = new ConcurrentQueue<string>();
				_debugOutputs.Enqueue("Begin ParellellyTickPawns");
			}
#endif

			foreach (var map in Find.Maps)
			{
				// Rebuilding any dirty region.
				map.regionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms();
#if DEBUG
				if (_debugLog)
				{
					_debugOutputs.Enqueue("Finish rebuilding dirty regions for map " +
					                      $"{map.uniqueID} at {_stopwatch.GetElapsedMicrosecondLong()} μs");
				}
#endif
				// Ensure factions lists init in main thread.
				map.mapPawns.SpawnedPawnsInFaction(null);

#if DEBUG
				if (_debugLog)
				{
					_debugOutputs.Enqueue("Finish factions lists init for map " +
					                      $"{map.uniqueID} at {_stopwatch.GetElapsedMicrosecondLong()} μs");
				}
#endif
			}

			_gameTick = GenTicks.TicksGame;
			var pawnCount = _pawns.Count;
			var jobCount = Math.Clamp(Mathf.FloorToInt(_parellellyTickPawnsWorkerCount), 1, 16);
			while (_jobQueue.TryDequeue(out _))
			{
			}
			for (var i = 0; i < pawnCount; i++)
			{
				_jobQueue.Enqueue(i);
			}

#if DEBUG
			if (_debugLog)
			{
				_debugOutputs.Enqueue($"Job queue populated at {_stopwatch.GetElapsedMicrosecondLong()} μs");
			}
#endif

			YaOptGlobal.IsParallelRunningInTick = true;
			JobHandle handle = default;
			_finishedJobCount = 0;
			for (var i = 0; i < jobCount; i++)
			{
				new ManagedJob(new ParallelPawnJob()).Schedule();
			}
#if DEBUG
			if (_debugLog)
			{
				_debugOutputs.Enqueue($"{jobCount} works started at {_stopwatch.GetElapsedMicrosecondLong()} μs");
			}
#endif
			JobHandle.ScheduleBatchedJobs();
#if DEBUG
			if (_debugLog)
			{
				_debugOutputs.Enqueue($"Batched jobs scheduled at {_stopwatch.GetElapsedMicrosecondLong()} μs");
			}
#endif
			while (_finishedJobCount != pawnCount)
			{
			}

#if DEBUG
			if (_debugLog)
			{
				_debugLog = false;
				_debugOutputs.Enqueue($"All jobs were finished at {_stopwatch.GetElapsedMicrosecondLong()} μs.");
				while (_debugOutputs.TryDequeue(out var str))
				{
					YaOptMod.Log(str);
				}
			}
#endif
			//handle.Complete();
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
		private readonly struct ParallelPawnJob : IJob
		{
			public void Execute()
			{
#if DEBUG
				var threadName = 0;
				if (_debugLog)
				{
					threadName = Thread.CurrentThread.ManagedThreadId;
					_debugOutputs.Enqueue($"Thread {threadName} woke at " +
					                      $"{_stopwatch.GetElapsedMicrosecondLong()} μs.");
				}
#endif
				while (_jobQueue.TryDequeue(out var jobIndex))
				{
#if DEBUG
					if (_debugLog)
					{
						var str = $"Thread {threadName} dequeue {_pawns[jobIndex]} (Job {jobIndex}) at " +
						          $"{_stopwatch.GetElapsedMicrosecondLong()} μs.";
						_debugOutputs.Enqueue(str);
					}
#endif

					JobPredictor.ProcessPawn(_pawns[jobIndex], _gameTick);

#if DEBUG
					if (_debugLog)
					{
						var str = $"Thread {threadName} finished {_pawns[jobIndex]} at " +
						          $"{_stopwatch.GetElapsedMicrosecondLong()} μs.";
						_debugOutputs.Enqueue(str);
					}
#endif
					Interlocked.Increment(ref _finishedJobCount);
				}
			}
		}
	}
}