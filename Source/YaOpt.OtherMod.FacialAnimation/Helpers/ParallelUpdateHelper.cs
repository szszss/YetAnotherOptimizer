using FacialAnimation;
using Gilzoide.ManagedJobs;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.Jobs;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.OtherMod.FacialAnimation.Helpers
{
	internal static class ParallelUpdateHelper
	{
		public static bool Enabled;

		private static JobHandle jobHandle = default;

		private static bool jobRunning = false;

		private static readonly List<Pawn> pendingPawns = new List<Pawn>();

		private static readonly ConcurrentDictionary<Pawn, int> lastUpdateTicks = new ConcurrentDictionary<Pawn, int>();

		static ParallelUpdateHelper()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
			UpdateCallbackHelper.RegisterPreRenderCallback(PreRender);
			UpdateCallbackHelper.RegisterPostRenderCallback(PostRender);
		}

		private static void ClearCache()
		{
			jobHandle = default;
			pendingPawns.Clear();
			lastUpdateTicks.Clear();
		}

		private static void PreRender(int tick)
		{
			if (!Enabled)
				return;

			// Clear cache per 3600 ticks
			if (tick % 3600 == 0)
				lastUpdateTicks.Clear();
			if (pendingPawns.Count > 0)
			{
				jobRunning = true;
				jobHandle = new ManagedJobFor(new UpdateFacialAnimationJob(pendingPawns))
					.ScheduleParallel(pendingPawns.Count, 1);
			}
		}

		private static void PostRender(int tick)
		{
			if (jobRunning)
			{
				jobHandle.CompleteWithSpinWait();
				jobRunning = false;
				pendingPawns.Clear();
			}
		}

		public static void AddPendingPawn(Pawn pawn)
		{
			if (jobRunning)
			{
				YaOptMod.Error("Try to add a pending pawn while the updating facial animation job is running.\n" +
				               $"Pawn: {pawn}");
				return;
			}
			pendingPawns.Add(pawn);
		}

		public static void UpdateFacialAnimation(Pawn pawn)
		{
			if (pawn.TryGetComp<FacialAnimationControllerComp>(out var comp))
			{
				var tick = Find.TickManager.TicksGame;
				if (lastUpdateTicks.TryGetValue(pawn, out var lastTick))
				{
					if (lastTick == tick)
						return;
					if (!lastUpdateTicks.TryUpdate(pawn, tick, lastTick))
						return;
				}
				else if (!lastUpdateTicks.TryAdd(pawn, tick))
				{
					return;
				}

				if (!comp.CheckUpdateableInitial())
					return;
				
				comp.UpdateStatus(Find.TickManager.TicksGame);
				comp.UpdateAnimation();
			}
		}

		public static void UpdateFacialAnimation(Corpse corpse)
		{
			UpdateFacialAnimation(corpse.InnerPawn);
		}

		private class UpdateFacialAnimationJob : IJobFor
		{
			private readonly List<Pawn> _pawns;

			public UpdateFacialAnimationJob(List<Pawn> pendingPawns)
			{
				_pawns = pendingPawns;
			}

			public void Execute(int index)
			{
				UpdateFacialAnimation(_pawns[index]);
			}
		}
	}
}