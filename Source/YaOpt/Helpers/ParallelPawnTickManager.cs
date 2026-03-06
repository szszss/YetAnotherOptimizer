using Gilzoide.ManagedJobs;
using LudeonTK;
using System;
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
		[TweakValue("YaOpt", 1f, 128f)]
		private static float _parellellyTickPawnsBatchSize = 4f;

		/// <summary>
		/// List of all pawns that need parallel tick processing.
		/// </summary>
		/// <remarks>
		/// Not thread-safe for writes; only modify from main thread via <see cref="AddThings"/> and <see cref="RemoveThings"/>.
		/// </remarks>
		private static readonly List<Pawn> _pawns = new List<Pawn>();

		/// <summary>
		/// Current game tick, cached to avoid race conditions during parallel processing.
		/// </summary>
		private static int _gameTick;

		private static int _stride;

		private static int _lastCount = -1;

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
			var batchSize = Math.Clamp(Mathf.FloorToInt(_parellellyTickPawnsBatchSize), 1, 16);
			if (_lastCount != pawnCount)
			{
				_lastCount = pawnCount;
				_stride = batchSize + 1;
				while (MiscHelper.GetGCD(_stride, pawnCount) != 1)
				{
					_stride++;
				}
			}

#if DEBUG
			if (_debugLog)
			{
				_debugOutputs.Enqueue($"Batch size and stride were setup at {_stopwatch.GetElapsedMicrosecondLong()} μs");
			}
#endif

			YaOptGlobal.IsParallelRunningInTick = true;
			JobHandle handle = new ManagedJobFor(new ParallelPawnJob()).ScheduleParallel(pawnCount, batchSize);

#if DEBUG
			if (_debugLog)
			{
				_debugOutputs.Enqueue($"Batched jobs scheduled at {_stopwatch.GetElapsedMicrosecondLong()} μs");
			}
#endif
			handle.Complete();
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
		/// Unity Job that processes pawns.
		/// </summary>
		private readonly struct ParallelPawnJob : IJobFor
		{
			public void Execute(int index)
			{
#if DEBUG
				var time = 0L;
				if (_debugLog)
				{
					time = _stopwatch.GetElapsedMicrosecondLong();
				}
#endif
				index = (int)((_stride * index) % _pawns.Count);
				JobPredictor.ProcessPawn(_pawns[index], _gameTick);
#if DEBUG
				if (_debugLog)
				{
					var current = _stopwatch.GetElapsedMicrosecondLong();
					var str = $"Thread {Thread.CurrentThread.ManagedThreadId} " +
							  $"finished {_pawns[index]} (Job {index}) at " +
							  $"{current} μs. Cost: {current - time}μs.";
					_debugOutputs.Enqueue(str);
				}
#endif
			}
		}
	}
}