using FacialAnimation;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.OtherMod.FacialAnimation.Helpers
{
	internal static class JobAnimationHelper
	{
		private static readonly Dictionary<string, Dictionary<string, List<FaceAnimationCache>>> raceAnimationCache =
			new Dictionary<string, Dictionary<string, List<FaceAnimationCache>>>();

		private static readonly HashSet<string> cachedRaces = new HashSet<string>();

		public class FaceAnimationCache
		{
			public FaceAnimationDef Def;
			public FaceAnimation TmpAnimation;
			public ulong Version;
		}

		static JobAnimationHelper()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
		}

		private static void ClearCache()
		{
			raceAnimationCache.Clear();
			cachedRaces.Clear();
		}

		public static Dictionary<string, List<FaceAnimationCache>> GetRaceAnimation(string pawnDefName)
		{
			if (cachedRaces.Contains(pawnDefName))
			{
				return raceAnimationCache[pawnDefName];
			}
			var dict = new Dictionary<string, List<FaceAnimationCache>>();
			CacheRaceAnimations(dict, pawnDefName);
			if (dict.Count == 0)
			{
				CacheRaceAnimations(dict, string.Empty);
			}
			cachedRaces.Add(pawnDefName);
			raceAnimationCache[pawnDefName] = dict;
			return dict;
		}

		private static void CacheRaceAnimations(Dictionary<string, List<FaceAnimationCache>> dict, string raceName)
		{
			YaOptMod.Log("Cache animation for " + raceName);
			foreach (var animationDef in DefDatabase<FaceAnimationDef>.AllDefs)
			{
				if (animationDef.raceName == raceName)
				{
					var cache = new FaceAnimationCache() { Def = animationDef };
					foreach (var defTargetJob in animationDef.targetJobs)
					{
						if (!dict.TryGetValue(defTargetJob, out var list))
						{
							list = new List<FaceAnimationCache>();
							dict[defTargetJob] = list;
						}
						list.Add(cache);
					}
				}
			}
		}

		public static List<FaceAnimation> ChangeAnimationListWithReset(Dictionary<string, List<FaceAnimation>> jobAnimationListDict, 
			string jobName, int resetTick, IEnumerable<FaceAnimation> output)
		{
			var list = output as List<FaceAnimation>;
			if (list == null)
			{
				list = new List<FaceAnimation>();
				YaOptMod.Error("JobAnimationHelper.ChangeAnimationListWithReset expect a List<FaceAnimation>, " +
				               "but the argument is " + output.GetType().FullName);
			}
			list.Clear();
			if (jobName == null)
			{
				jobName = "";
			}
			if (!jobAnimationListDict.ContainsKey(jobName))
			{
				jobName = "";
			}
			foreach (var faceAnimation in jobAnimationListDict[jobName])
			{
				list.Add(faceAnimation);
				faceAnimation.Reset(resetTick);
			}
			return list;
		}

		public static IEnumerable<FaceAnimation> FilterAnimationListWithCurrentStatus(IEnumerable<FaceAnimation> sourceAnimationList,
			List<Thought> thoughts, float currentMood, float currentPain, IEnumerable<FaceAnimation> output)
		{
			var list = output as List<FaceAnimation>;
			if (list == null)
			{
				list = new List<FaceAnimation>();
				YaOptMod.Error("JobAnimationHelper.FilterAnimationListWithCurrentStatus expect a List<FaceAnimation>, " +
				               "but the argument is " + output.GetType().FullName + " (This is a YaOpt bug. Report to the author of YaOpt instead of the original mod)");
			}
			currentMood = Mathf.Clamp(currentMood, 0f, 1f);
			currentPain = Mathf.Clamp(currentPain, 0f, 1f);
			var tmpList = ThreadLocalTmpList<FacialAnimationControllerComp, FaceAnimation>.Get();
			tmpList.Clear();
			foreach (var animation in sourceAnimationList)
			{
				var animDef = animation.animationDef;
				if (currentMood >= animDef.targetMoodMin && currentMood <= animDef.targetMoodMax &&
				    currentPain >= animDef.targetPainMin && currentPain <= animDef.targetPainMax)
				{
					var targetThoughtDefs = animDef.targetThoughtDefs;
					if (targetThoughtDefs.Count == 0)
					{
						tmpList.Add(animation);
						continue;
					}
					foreach (var thought in thoughts)
					{
						if (targetThoughtDefs.Contains(thought.def.defName))
						{
							tmpList.Add(animation);
							break;
						}
					}
				}
			}
			list.Clear();
			list.AddRange(tmpList);
			tmpList.Clear();
			return list;
		}
	}
}