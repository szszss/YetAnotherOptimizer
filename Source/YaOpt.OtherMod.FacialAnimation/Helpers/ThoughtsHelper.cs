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
	[StaticConstructorOnStartup]
	internal static class ThoughtsHelper
	{
		private delegate bool ThoughtsDirtyDelegate(SituationalThoughtHandler thought);

		private static readonly AccessTools.FieldRef<SituationalThoughtHandler, bool> thoughtsDirtyRef;

		private static readonly ConcurrentBag<(FacialAnimationParam, Pawn)> pendingQueue =
			new ConcurrentBag<(FacialAnimationParam, Pawn)>();

		private static readonly HashSet<(FacialAnimationParam, Pawn)> pullBackSet =
			new HashSet<(FacialAnimationParam, Pawn)>();

		static ThoughtsHelper()
		{
			UpdateCallbackHelper.RegisterPostTickCallback(UpdateThoughts);
			thoughtsDirtyRef = AccessTools.FieldRefAccess<bool>(typeof(SituationalThoughtHandler), "thoughtsDirty");
		}

		private static void UpdateThoughts(int tick)
		{
			while (pendingQueue.TryTake(out var result))
			{
				TryUpdateThoughts(result.Item1, result.Item2, true);
			}
			if (pullBackSet.Count > 0)
			{
				foreach (var element in pullBackSet)
				{
					pendingQueue.Add(element);
				}
				pullBackSet.Clear();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool GetThoughtsDirty(SituationalThoughtHandler situational)
		{
			return thoughtsDirtyRef(situational);
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
						pendingQueue.Add((param, pawn));
						return;
					}
				}
				else
				{
					// In multiplayer games, we will never trigger thoughts updates.
					// If we find that thoughts are dirty, we will put this request
					// back into PullBack so they can be checked again in the next update.
					if (YaOptGlobal.IsMultiplay && GetThoughtsDirty(thoughtHandler.situational))
					{
						pullBackSet.Add((param, pawn));
						return;
					}
					param.needUpdateFilterOnly = true;
				}
				thoughtHandler.GetAllMoodThoughts(param.currentThoughts);
			}
		}
	}
}