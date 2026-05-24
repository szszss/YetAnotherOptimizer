using HarmonyLib;
using LudeonTK;
using RimWorld;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.Jobs;
using Verse;

namespace YaOpt.Helpers
{
	public static class ParallelThoughtUpdater
	{
		[TweakValue("YaOpt")]
		private static bool ParallelThoughtUpdaterManualBatch = false;

		[TweakValue("YaOpt", 1f, 256f)]
		private static int ParallelThoughtUpdaterBatchSize = 16;

		private static readonly HashSet<ThoughtDef> _createdThoughtDefs = new HashSet<ThoughtDef>();

		private static readonly ConcurrentQueue<Thought_Situational> _thoughtsToAdd =
			new ConcurrentQueue<Thought_Situational>();

		private static readonly ConcurrentBag<Exception> _exceptionInWorkers =
			new ConcurrentBag<Exception>();

		private delegate Thought_Situational TryCreateThoughtDelegate(
			SituationalThoughtHandler instance, ThoughtDef def);

		private static readonly TryCreateThoughtDelegate _tryCreateThought =
			AccessTools.MethodDelegate<TryCreateThoughtDelegate>(
				AccessTools.Method(typeof(SituationalThoughtHandler), "TryCreateThought"),
				null, false, null);

		public static void Update(SituationalThoughtHandler thoughtHandler, List<Thought_Situational> cachedThoughts)
		{
			JobHandle handleCreating = default;
			try
			{
				YaOptGlobal.IsParallelRunningInTick = true;
				var pawn = thoughtHandler.pawn;

				foreach (var thought in cachedThoughts)
				{
					_createdThoughtDefs.Add(thought.def);
				}

				foreach (var map in Find.Maps)
				{
					// Ensure factions lists init in main thread.
					map.mapPawns.SpawnedPawnsInFaction(null);
				}

				var situationalNonSocialThoughtDefs = ThoughtUtility.situationalNonSocialThoughtDefs;
				var batchSize = ParallelThoughtUpdaterManualBatch
					? ParallelThoughtUpdaterBatchSize
					: UnityData.GetIdealBatchCount(situationalNonSocialThoughtDefs.Count);
				handleCreating = new YaOptManagedJobs.JobFor(
						new ParallelThoughtCreatingJob(thoughtHandler, situationalNonSocialThoughtDefs))
					.ScheduleParallel(situationalNonSocialThoughtDefs.Count, batchSize);

				JobHandle.ScheduleBatchedJobs();

				foreach (var thought in cachedThoughts)
				{
					thought.RecalculateState();
				}

				if (ModsConfig.IdeologyActive && pawn.Ideo != null)
				{
					foreach (var precept in pawn.Ideo.PreceptsListForReading)
					{
						var newThoughts = precept.SituationThoughtsToAdd(pawn, cachedThoughts);
						if (newThoughts.Count > 0)
						{
							cachedThoughts.AddRange(newThoughts);
						}
					}
				}

				handleCreating.Complete();
				handleCreating = default;

				while (_thoughtsToAdd.TryDequeue(out var thought))
				{
					cachedThoughts.Add(thought);
				}
			}
			catch
			{
				// If there are exceptions, try stop jobs firstly.
				if (!handleCreating.IsCompleted)
				{
					handleCreating.Complete();
				}
				throw; // No exceptions handling here, just re-throw them.
			}
			finally
			{
				YaOptGlobal.IsParallelRunningInTick = false;
				_createdThoughtDefs.Clear();
				_thoughtsToAdd.Clear();

				while (_exceptionInWorkers.TryTake(out var ex))
				{
					Log.Error(ex.ToString());
				}
			}
		}

		private readonly struct ParallelThoughtCreatingJob : IJobFor
		{
			private readonly SituationalThoughtHandler _thoughtHandler;

			private readonly List<ThoughtDef> _situationalNonSocialThoughtDefs;

			public ParallelThoughtCreatingJob(SituationalThoughtHandler thoughtHandler,
				List<ThoughtDef> situationalNonSocialThoughtDefs)
			{
				_thoughtHandler = thoughtHandler;
				_situationalNonSocialThoughtDefs = situationalNonSocialThoughtDefs;
			}


			public void Execute(int index)
			{
				try
				{
					var thoughtDef = _situationalNonSocialThoughtDefs[index];
					if (!_createdThoughtDefs.Contains(thoughtDef))
					{
						var thought = _tryCreateThought(_thoughtHandler, thoughtDef);
						if (thought != null)
						{
							_thoughtsToAdd.Enqueue(thought);
						}
					}
				}
				catch (Exception ex)
				{
					_exceptionInWorkers.Add(ex);
				}
			}
		}
	}
}