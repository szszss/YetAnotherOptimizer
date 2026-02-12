using FacialAnimation;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using UnityEngine;
using YaOpt.Helpers;
using YaOpt.Unity;
using static FacialAnimation.FaceAnimationDef;

namespace YaOpt.OtherMod.FacialAnimation.Patches
{
	/// <summary>
	/// <seealso cref="SubMod.OptFADeLinq"/>
	/// <seealso cref="SubMod.OptFADeLinqBurst"/>
	/// </summary>
	[HarmonyPatch(typeof(AnimationFrameAccumulator))]
	[HarmonyPatch(nameof(AnimationFrameAccumulator.AccumResultFrameAndClear))]
	internal static class FacialAnimation_AnimationFrameAccumulator_AccumResultFrameAndClear
	{
		private static bool useBurst;

		static bool Prepare(MethodBase original)
		{
			if (original != null)
			{
				MiscHelper.CheckHash(original, Hash128.Parse("ddae71ac2f720bf9ee276dbe5c7dee56"));
			}
			useBurst = SubMod.OptFADeLinqBurst.Enabled;
			return SubMod.OptFADeLinq.Enabled;
		}

		static bool Prefix(List<FaceAnimationDef.AnimationFrame> ___accumFrames,
			ref FaceAnimationDef.AnimationFrame __result)
		{
			var burst = useBurst;
			var animationFrame = new AnimationFrame();

			for (var i = ___accumFrames.Count - 1; i >= 0; i--)
			{
				if (___accumFrames[i].headShapeDef == null)
					continue;
				animationFrame.headShapeDef = ___accumFrames[i].headShapeDef;
				break;
			}
			for (var i = ___accumFrames.Count - 1; i >= 0; i--)
			{
				if (___accumFrames[i].browShapeDef == null)
					continue;
				animationFrame.browShapeDef = ___accumFrames[i].browShapeDef;
				break;
			}
			for (var i = ___accumFrames.Count - 1; i >= 0; i--)
			{
				if (___accumFrames[i].lidShapeDef == null)
					continue;
				animationFrame.lidShapeDef = ___accumFrames[i].lidShapeDef;
				break;
			}
			for (var i = ___accumFrames.Count - 1; i >= 0; i--)
			{
				if (___accumFrames[i].lidOptionShapeDef == null)
					continue;
				animationFrame.lidOptionShapeDef = ___accumFrames[i].lidOptionShapeDef;
				break;
			}
			for (var i = ___accumFrames.Count - 1; i >= 0; i--)
			{
				if (___accumFrames[i].eyeballShapeDef == null)
					continue;
				animationFrame.eyeballShapeDef = ___accumFrames[i].eyeballShapeDef;
				break;
			}
			for (var i = ___accumFrames.Count - 1; i >= 0; i--)
			{
				if (___accumFrames[i].mouthShapeDef == null)
					continue;
				animationFrame.mouthShapeDef = ___accumFrames[i].mouthShapeDef;
				break;
			}
			for (var i = ___accumFrames.Count - 1; i >= 0; i--)
			{
				if (___accumFrames[i].emotionShapeDef == null)
					continue;
				animationFrame.emotionShapeDef = ___accumFrames[i].emotionShapeDef;
				break;
			}

			if (burst)
			{
				AccumResultFramesBurst(animationFrame, ___accumFrames);
			}
			else
			{
				AccumResultFramesNoBurst(animationFrame, ___accumFrames);
			}

			___accumFrames.Clear();
			__result = animationFrame;
			return false;
		}

		private static void AccumResultFramesBurst(AnimationFrame animationFrame,
			List<AnimationFrame> accumFrames)
		{
			var frameCount = accumFrames.Count;
			var resultStruct = new YaOptBurst.FacialAnimationFrameStruct();
			var accumFrameStructs = new NativeArray<YaOptBurst.FacialAnimationFrameStruct>(frameCount, Allocator.Temp);
			for (var i = 0; i < accumFrames.Count; i++)
			{
				var frame = accumFrames[i];
				accumFrameStructs[i] = new YaOptBurst.FacialAnimationFrameStruct()
				{
					HeadOffset = frame.headOffset,
					BrowOffset = frame.browOffset,
					LidOffset = frame.lidOffset,
					EyeballOffset = frame.eyeballOffset,
					EyeballOffsetL = frame.eyeballOffsetL,
					EyeballOffsetR = frame.eyeballOffsetR,
					MouthOffset = frame.mouthOffset
				};
			}
			YaOptBurst.AccumResultFrames(accumFrameStructs, frameCount, ref resultStruct);
			animationFrame.headOffset = resultStruct.HeadOffset;
			animationFrame.browOffset = resultStruct.BrowOffset;
			animationFrame.lidOffset = resultStruct.LidOffset;
			animationFrame.eyeballOffset = resultStruct.EyeballOffset;
			animationFrame.eyeballOffsetL = resultStruct.EyeballOffsetL;
			animationFrame.eyeballOffsetR = resultStruct.EyeballOffsetR;
			animationFrame.mouthOffset = resultStruct.MouthOffset;
			accumFrameStructs.Dispose();
		}

		private static void AccumResultFramesNoBurst(AnimationFrame animationFrame,
			List<AnimationFrame> accumFrames)
		{
			var accumHeadOffset = new Vector3();
			var accumBrowOffset = new Vector3();
			var accumLidOffset = new Vector3();
			var accumEyeballOffset = new Vector3();
			var accumEyeballOffsetL = new Vector3();
			var accumEyeballOffsetR = new Vector3();
			var accumMouthOffset = new Vector3();
			foreach (var frame in accumFrames)
			{
				accumHeadOffset += frame.headOffset;
				accumBrowOffset += frame.browOffset;
				accumLidOffset += frame.lidOffset;
				accumEyeballOffset += frame.eyeballOffset;
				accumEyeballOffsetL += frame.eyeballOffsetL;
				accumEyeballOffsetR += frame.eyeballOffsetR;
				accumMouthOffset += frame.mouthOffset;
			}
			float frameCount = accumFrames.Count + accumHeadOffset.y;
			animationFrame.headOffset = (frameCount != 0 ?
				new Vector3(accumHeadOffset.x / frameCount, 0f, accumHeadOffset.z / frameCount) : new Vector3());
			frameCount = accumFrames.Count + accumBrowOffset.y;
			animationFrame.browOffset = (frameCount != 0 ?
				new Vector3(accumBrowOffset.x / frameCount, 0f, accumBrowOffset.z / frameCount) : new Vector3());
			frameCount = accumFrames.Count + accumLidOffset.y;
			animationFrame.lidOffset = (frameCount != 0 ?
				new Vector3(accumLidOffset.x / frameCount, 0f, accumLidOffset.z / frameCount) : new Vector3());
			frameCount = accumFrames.Count + accumEyeballOffset.y;
			animationFrame.eyeballOffset = (frameCount != 0 ?
				new Vector3(accumEyeballOffset.x / frameCount, 0f, accumEyeballOffset.z / frameCount) : new Vector3());
			frameCount = accumFrames.Count + accumEyeballOffsetL.y;
			animationFrame.eyeballOffsetL = (frameCount != 0 ?
				new Vector3(accumEyeballOffsetL.x / frameCount, 0f, accumEyeballOffsetL.z / frameCount) : new Vector3());
			frameCount = accumFrames.Count + accumEyeballOffsetR.y;
			animationFrame.eyeballOffsetR = (frameCount != 0 ?
				new Vector3(accumEyeballOffsetR.x / frameCount, 0f, accumEyeballOffsetR.z / frameCount) : new Vector3());
			frameCount = accumFrames.Count + accumMouthOffset.y;
			animationFrame.mouthOffset = (frameCount != 0 ?
				new Vector3(accumMouthOffset.x / frameCount, 0f, accumMouthOffset.z / frameCount) : new Vector3());
		}
	}
}