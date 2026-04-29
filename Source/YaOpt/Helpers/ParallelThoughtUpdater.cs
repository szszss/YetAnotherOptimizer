using HarmonyLib;
using LudeonTK;
using RimWorld;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
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

		private delegate Thought_Situational TryCreateThoughtDelegate(
			SituationalThoughtHandler instance, ThoughtDef def);

		private static readonly TryCreateThoughtDelegate _tryCreateThought =
			AccessTools.MethodDelegate<TryCreateThoughtDelegate>(
				AccessTools.Method(typeof(SituationalThoughtHandler), "TryCreateThought"),
				null, false, null);

		public static void Update(SituationalThoughtHandler thoughtHandler, List<Thought_Situational> cachedThoughts)
		{
			try
			{
				YaOptGlobal.IsParallelRunningInTick = true;
				var pawn = thoughtHandler.pawn;

				foreach (var thought in cachedThoughts)
				{
					_createdThoughtDefs.Add(thought.def);
				}

				var situationalNonSocialThoughtDefs = ThoughtUtility.situationalNonSocialThoughtDefs;
				var batchSize = ParallelThoughtUpdaterManualBatch
					? ParallelThoughtUpdaterBatchSize
					: UnityData.GetIdealBatchCount(situationalNonSocialThoughtDefs.Count);
				var handleCreating = new YaOptManagedJobs.JobFor(
						new ParallelThoughtCreatingJob(thoughtHandler,situationalNonSocialThoughtDefs))
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

				while (_thoughtsToAdd.TryDequeue(out var thought))
				{
					cachedThoughts.Add(thought);
				}
			}
			finally
			{
				YaOptGlobal.IsParallelRunningInTick = false;
				_createdThoughtDefs.Clear();
				_thoughtsToAdd.Clear();
			}
		}

		/*private readonly struct ParallelThoughtRecalculatingJob : IJobFor
		{
			private readonly List<Thought_Situational> _cachedThoughts;

			public ParallelThoughtRecalculatingJob(List<Thought_Situational> cachedThoughts)
			{
				_cachedThoughts = cachedThoughts;
			}

			public void Execute(int index)
			{
				_cachedThoughts[index].RecalculateState();
			}
		}*/

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
		}

		/*private readonly struct ParallelPreceptThoughtCreatingJob : IJob
		{
			private readonly Pawn _pawn;

			private readonly List<Thought_Situational> _cachedThoughts;

			private readonly List<Precept> _pawnPrecepts;

			public ParallelPreceptThoughtCreatingJob(Pawn pawn, List<Thought_Situational> cachedThoughts,
				List<Precept> pawnPrecepts)
			{
				_pawn = pawn;
				_cachedThoughts = cachedThoughts;
				_pawnPrecepts = pawnPrecepts;
			}

			public void Execute()
			{
				foreach (var precept in _pawnPrecepts)
				{
					var newThoughts = precept.SituationThoughtsToAdd(_pawn, _cachedThoughts);
					if (newThoughts.Count > 0)
					{
						_cachedThoughts.AddRange(newThoughts);
					}
				}
			}
		}*/

		/*private readonly struct ParallelThoughtAddingJob : IJob
		{
			private readonly List<Thought_Situational> _cachedThoughts;

			public ParallelThoughtAddingJob(List<Thought_Situational> cachedThoughts)
			{
				_cachedThoughts = cachedThoughts;
			}

			public void Execute()
			{
				while (_thoughtsToAdd.TryDequeue(out var thought))
				{
					_cachedThoughts.Add(thought);
				}
			}
		}*/
	}
}