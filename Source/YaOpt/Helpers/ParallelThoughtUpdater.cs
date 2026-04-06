using Gilzoide.ManagedJobs;
using HarmonyLib;
using RimWorld;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.Jobs;
using Verse;

namespace YaOpt.Helpers
{
	public static class ParallelThoughtUpdater
	{
		private static readonly HashSet<ThoughtDef> _createdThoughts = new HashSet<ThoughtDef>();

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
				var handleRecalculating =
					new ManagedJobFor(new ParallelThoughtRecalculatingJob(cachedThoughts)).ScheduleParallel(
						cachedThoughts.Count, UnityData.GetIdealBatchCount(cachedThoughts.Count));

				foreach (var thought in cachedThoughts)
				{
					_createdThoughts.Add(thought.def);
				}

				if (ModsConfig.IdeologyActive && pawn.Ideo != null)
				{
					handleRecalculating = new ManagedJob(new ParallelPreceptThoughtCreatingJob(pawn,
						cachedThoughts, pawn.Ideo.PreceptsListForReading)).Schedule(handleRecalculating);
				}

				var situationalNonSocialThoughtDefs = ThoughtUtility.situationalNonSocialThoughtDefs;
				var handleCreating = new ManagedJobFor(new ParallelThoughtCreatingJob(thoughtHandler,
					situationalNonSocialThoughtDefs)).ScheduleParallel(situationalNonSocialThoughtDefs.Count, 1);

				var waitHandle = new ManagedJob(new ParallelThoughtAddingJob(cachedThoughts)).Schedule(
					JobHandle.CombineDependencies(handleRecalculating, handleCreating));

				waitHandle.Complete();
			}
			finally
			{
				YaOptGlobal.IsParallelRunningInTick = false;
				_createdThoughts.Clear();
				_thoughtsToAdd.Clear();
			}
		}

		private readonly struct ParallelThoughtRecalculatingJob : IJobFor
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
				var thoughtDef = _situationalNonSocialThoughtDefs[index];
				if (!_createdThoughts.Contains(thoughtDef))
				{
					var thought = _tryCreateThought(_thoughtHandler, thoughtDef);
					if (thought != null)
					{
						_thoughtsToAdd.Enqueue(thought);
					}
				}
			}
		}

		private readonly struct ParallelPreceptThoughtCreatingJob : IJob
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
		}

		private readonly struct ParallelThoughtAddingJob : IJob
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
		}
	}
}