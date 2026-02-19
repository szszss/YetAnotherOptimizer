using FacialAnimation;
using HarmonyLib;
using System.Collections.Generic;
using Verse;
using YaOpt.OtherMod.FacialAnimation.Helpers;
using static YaOpt.OtherMod.FacialAnimation.Helpers.JobAnimationHelper;

namespace YaOpt.OtherMod.FacialAnimation.Patches
{
	/// <summary>
	/// Caches animation dictionaries by race to reduce pawn spawn stuttering.
	/// </summary>
	/// <seealso cref="SubMod.OptFAPawnSpawn"/>
	[HarmonyPatch("FacialAnimation.FAHelper", "CreateAnimationDict")]
	internal static class FacialAnimation_FAHelper_CreateAnimationDict
	{
		private static ulong cacheVersion = 0;

		private static readonly List<FaceAnimationCache> emptyList = new List<FaceAnimationCache>(0);

		private static void FillAnimationByCachedDefs(List<FaceAnimation> anims, List<FaceAnimationCache> cache,
			ulong version, int initialTick)
		{
			foreach (var animCache in cache)
			{
				if (animCache.Version != version)
				{
					animCache.Version = version;
					animCache.TmpAnimation = new FaceAnimation(animCache.Def, initialTick);
				}
				anims.Add(animCache.TmpAnimation);
			}
		}

		private static List<FaceAnimation> CreateJobAnimation(Dictionary<string, List<FaceAnimationCache>> animDefDict,
			List<FaceAnimationCache> constantJob,
			ulong version, int initialTick, string job)
		{
			List<FaceAnimation> list;
			if (animDefDict.TryGetValue(job, out var jobAnimDefs))
			{
				list = new List<FaceAnimation>(jobAnimDefs.Count + constantJob.Count);
				FillAnimationByCachedDefs(list, jobAnimDefs, version, initialTick);
				FillAnimationByCachedDefs(list, constantJob, version, initialTick);
			}
			else
			{
				list = new List<FaceAnimation>(constantJob.Count);
				FillAnimationByCachedDefs(list, constantJob, version, initialTick);
			}
			return list;
		}

		static bool Prepare()
		{
			return SubMod.OptFAPawnSpawn.Enabled;
		}

		static bool Prefix(Pawn pawn, int initialTick, out Dictionary<string, List<FaceAnimation>> animationDict)
		{
			animationDict = new Dictionary<string, List<FaceAnimation>>();
			var pawnRaceName = pawn.def.defName;
			var version = ++cacheVersion;
			var animDefDict = JobAnimationHelper.GetRaceAnimation(pawnRaceName);
			var constantJobAnimDefs = animDefDict.GetValueOrDefault("ConstantJob", emptyList);
			foreach (var jobDef in DefDatabase<JobDef>.AllDefs)
			{
				var list = CreateJobAnimation(animDefDict, constantJobAnimDefs, version, initialTick, jobDef.defName);
				animationDict.Add(jobDef.defName, list);
			}
			var defaultAnim = new List<FaceAnimation>(constantJobAnimDefs.Count);
			FillAnimationByCachedDefs(defaultAnim, constantJobAnimDefs, version, initialTick);
			animationDict.Add("", defaultAnim);
			foreach (var pair in animationDict)
			{
				pair.Value.Sort((FaceAnimation a, FaceAnimation b) => a.animationDef.priority - b.animationDef.priority);
			}
			return false;
		}
	}
}