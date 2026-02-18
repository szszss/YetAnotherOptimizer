using FacialAnimation;
using HarmonyLib;
using RimWorld;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.OtherMod.FacialAnimation.Helpers
{
	/// <summary>
	/// Handles thread-safe updates for Pawn thoughts/moods required by Facial Animation.
	/// <br/>
	/// Calculating thoughts is not thread-safe. When a worker thread detects that thoughts need updating (dirty),
	/// it queues the request instead of calculating immediately. The main thread then processes this queue
	/// to safely update thoughts.
	/// </summary>
	[StaticConstructorOnStartup]
	internal static class ThoughtsHelper
	{
		private delegate bool ThoughtsDirtyDelegate(SituationalThoughtHandler thought);

		private static readonly AccessTools.FieldRef<SituationalThoughtHandler, bool> _thoughtsDirtyRef;

		private static readonly ConcurrentBag<(FacialAnimationParam, Pawn)> _pendingQueue =
			new ConcurrentBag<(FacialAnimationParam, Pawn)>();

		private static readonly HashSet<(FacialAnimationParam, Pawn)> _pullBackSet =
			new HashSet<(FacialAnimationParam, Pawn)>();

		static ThoughtsHelper()
		{
			UpdateCallbackHelper.RegisterPostTickCallback(UpdateThoughts);
			_thoughtsDirtyRef = AccessTools.FieldRefAccess<bool>(typeof(SituationalThoughtHandler), "thoughtsDirty");
		}

		private static void UpdateThoughts(int tick)
		{
			while (_pendingQueue.TryTake(out var result))
			{
				TryUpdateThoughts(result.Item1, result.Item2, true);
			}
			if (_pullBackSet.Count > 0)
			{
				foreach (var element in _pullBackSet)
				{
					_pendingQueue.Add(element);
				}
				_pullBackSet.Clear();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool GetThoughtsDirty(SituationalThoughtHandler situational)
		{
			return _thoughtsDirtyRef(situational);
		}

		public static void TryUpdateThoughts(FacialAnimationParam param, Pawn pawn, bool fromMainThread)
		{
			var thoughtHandler = pawn?.needs?.mood?.thoughts;
			if (thoughtHandler != null)
			{
				if (!fromMainThread)
				{
					if (GetThoughtsDirty(thoughtHandler.situational))
					{
						_pendingQueue.Add((param, pawn));
						return;
					}
				}
				else
				{
					// In multiplayer games, updating thoughts affects game state (mood) and can cause desyncs.
					// Therefore, we must NEVER proactively update thoughts, even on the main thread.
					// We only wait for the vanilla game logic to naturally trigger thought updates.
					// If we find that thoughts are dirty, we put the request back into the queue (PullBack)
					// to check again in the next update cycle, hoping vanilla logic has updated them by then.
					if (YaOptGlobal.IsMultiplayer && GetThoughtsDirty(thoughtHandler.situational))
					{
						_pullBackSet.Add((param, pawn));
						return;
					}
					param.needUpdateFilterOnly = true;
				}
				thoughtHandler.GetAllMoodThoughts(param.currentThoughts);
			}
		}
	}
}