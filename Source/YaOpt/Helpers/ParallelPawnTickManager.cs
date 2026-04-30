using LudeonTK;
using System;
using System.Collections.Generic;
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
		private static float _parellellyTickPawnsBatchSize = 8f;

		private static readonly List<Pawn> _humanPawns = new List<Pawn>();

		private static readonly List<Pawn> _nonhumanPawns = new List<Pawn>();

		public static List<int> PawnsWhoShouldSkipMoodUpdate { get; private set; } = new List<int>();

		/// <summary>
		/// Current game tick, cached to avoid race conditions during parallel processing.
		/// </summary>
		private static int _gameTick;

		static ParallelPawnTickManager()
		{
			UpdateCallbackHelper.RegisterPreTickCallback(PreTick);
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
					if (pawn.RaceProps.ToolUser)
						_humanPawns.Add(pawn);
					else
						_nonhumanPawns.Add(pawn);
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
					if (pawn.RaceProps.ToolUser)
						_humanPawns.Remove(pawn);
					else
						_nonhumanPawns.Remove(pawn);
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
			var batchSize = Math.Clamp(Mathf.FloorToInt(_parellellyTickPawnsBatchSize), 1, 16);
			var predictJobFailure = YaOptGlobal.Settings.ParallelPawnJobFailurePrediction;
			var predictConstantJob = YaOptGlobal.Settings.ParallelPawnConstantJobPrediction;

#if DEBUG
			if (_debugLog)
			{
				_debugOutputs.Enqueue($"Batch size and stride were setup at {_stopwatch.GetElapsedMicrosecondLong()} μs");
			}
#endif

			YaOptGlobal.IsParallelRunningInTick = true;
			JobHandle handle = JobHandle.CombineDependencies(
				new YaOptManagedJobs.JobFor(new ParallelPawnJob(_humanPawns,
						predictJobFailure, predictConstantJob))
					.ScheduleParallel(_humanPawns.Count, 1),
				new YaOptManagedJobs.JobFor(new ParallelPawnJob(_nonhumanPawns,
						predictJobFailure, false, _humanPawns.Count))
					.ScheduleParallel(_nonhumanPawns.Count, batchSize));

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
		/// Clears the skipping list before new tick.
		/// </summary>
		private static void PreTick(int _)
		{
			PawnsWhoShouldSkipMoodUpdate.Clear();
		}

		/// <summary>
		/// Clears all cached data and resets state.
		/// </summary>
		private static void ClearCache()
		{
			_humanPawns.Clear();
			_nonhumanPawns.Clear();
			PawnsWhoShouldSkipMoodUpdate.Clear();
			JobPredictor.CleanCache();
		}

		/// <summary>
		/// Unity Job that processes pawns.
		/// </summary>
		private readonly struct ParallelPawnJob : IJobFor
		{
			private readonly List<Pawn> _list;
			private readonly int _debugJobIndexOffset;
			private readonly bool _predictJobFailure;
			private readonly bool _predictConstantJob;

			public ParallelPawnJob(List<Pawn> list,
				bool predictJobFailure, bool predictConstantJob,
				int debugJobIndexOffset = 0)
			{
				_list = list;
				_predictJobFailure = predictJobFailure;
				_predictConstantJob = predictConstantJob;
				_debugJobIndexOffset = debugJobIndexOffset;
			}

			public void Execute(int index)
			{
#if DEBUG
				var time = 0L;
				if (_debugLog)
				{
					time = _stopwatch.GetElapsedMicrosecondLong();
				}
#endif
				var pawn = _list[index];
				var shouldTickInterval = false;
				var tickDeltaPlusOne = -1;
				if (_predictConstantJob)
				{
					shouldTickInterval = ThingHelper.ShouldTickInterval(pawn, out tickDeltaPlusOne);
				}
				if (pawn.Spawned)
				{
					JobPredictor.ProcessPawn(pawn, _gameTick, tickDeltaPlusOne,
						_predictJobFailure, shouldTickInterval);
				}
#if DEBUG
				if (_debugLog)
				{
					var current = _stopwatch.GetElapsedMicrosecondLong();
					var str = $"Thread {Thread.CurrentThread.ManagedThreadId} " +
							  $"finished {pawn} (Job {index + _debugJobIndexOffset}) at " +
							  $"{current} μs. Cost: {current - time}μs.";
					_debugOutputs.Enqueue(str);
				}
#endif
			}
		}
	}
}