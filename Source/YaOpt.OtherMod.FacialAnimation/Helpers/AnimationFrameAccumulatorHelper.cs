using FacialAnimation;
using System;
using Unity.Collections;
using YaOpt.Unity;

namespace YaOpt.OtherMod.FacialAnimation.Helpers
{
	//[StaticConstructorOnStartup]
	[Obsolete]
	public static class AnimationFrameAccumulatorHelper
	{
		private static NativeArray<YaOptBurst.FacialAnimationFrameStruct> accumFrames;
		private static int current = 0;
		private static int maxCount;
		private static HeadShapeDef lastHeadShapeDef;
		private static BrowShapeDef lastBrowrowShapeDef;
		private static LidShapeDef lastLidShapeDef;
		private static LidOptionShapeDef lastLidOptionShapeDef;
		private static EyeballShapeDef lastEyeballShapeDef;
		private static MouthShapeDef lastMouthShapeDef;
		private static EmotionShapeDef lastEmotionShapeDef;

		public static NativeArray<YaOptBurst.FacialAnimationFrameStruct> AccumFrames => accumFrames;

		public static int FrameCount => current;

		static AnimationFrameAccumulatorHelper()
		{
			maxCount = 10;
			accumFrames = new NativeArray<YaOptBurst.FacialAnimationFrameStruct>(maxCount, Allocator.Persistent);
		}

		public static void Clear()
		{
			current = 0;
			lastHeadShapeDef = null;
			lastBrowrowShapeDef = null;
			lastLidShapeDef = null;
			lastLidOptionShapeDef = null;
			lastEyeballShapeDef = null;
			lastMouthShapeDef = null;
			lastEmotionShapeDef = null;
		}

		public static void Add(FaceAnimationDef.AnimationFrame animationFrame)
		{
			if (animationFrame.headShapeDef != null)
				lastHeadShapeDef = animationFrame.headShapeDef;
			if (animationFrame.browShapeDef != null)
				lastBrowrowShapeDef = animationFrame.browShapeDef;
			if (animationFrame.lidShapeDef != null)
				lastLidShapeDef = animationFrame.lidShapeDef;
			if (animationFrame.lidOptionShapeDef != null)
				lastLidOptionShapeDef = animationFrame.lidOptionShapeDef;
			if (animationFrame.eyeballShapeDef != null)
				lastEyeballShapeDef = animationFrame.eyeballShapeDef;
			if (animationFrame.mouthShapeDef != null)
				lastMouthShapeDef = animationFrame.mouthShapeDef;
			if (animationFrame.emotionShapeDef != null)
				lastEmotionShapeDef = animationFrame.emotionShapeDef;

			AddFrameStruct(new YaOptBurst.FacialAnimationFrameStruct()
			{
				HeadOffset = animationFrame.headOffset,
				BrowOffset = animationFrame.browOffset,
				LidOffset = animationFrame.lidOffset,
				EyeballOffset = animationFrame.eyeballOffset,
				EyeballOffsetL = animationFrame.eyeballOffsetL,
				EyeballOffsetR = animationFrame.eyeballOffsetR,
				MouthOffset = animationFrame.mouthOffset
			});
		}

		private static void AddFrameStruct(in YaOptBurst.FacialAnimationFrameStruct frame)
		{
			while (current >= maxCount)
			{
				var newCount = maxCount + 8;
				var newArray = new NativeArray<YaOptBurst.FacialAnimationFrameStruct>(newCount, Allocator.Persistent);
				NativeArray<YaOptBurst.FacialAnimationFrameStruct>.Copy(accumFrames, 0, newArray, 0, maxCount);
				accumFrames.Dispose();
				accumFrames = newArray;
				maxCount = newCount;
			}

			accumFrames[current++] = frame;
		}

		public static void ApplyShapeDefs(FaceAnimationDef.AnimationFrame resultFrame)
		{
			resultFrame.headShapeDef = lastHeadShapeDef;
			resultFrame.browShapeDef = lastBrowrowShapeDef;
			resultFrame.lidShapeDef = lastLidShapeDef;
			resultFrame.lidOptionShapeDef = lastLidOptionShapeDef;
			resultFrame.eyeballShapeDef = lastEyeballShapeDef;
			resultFrame.mouthShapeDef = lastMouthShapeDef;
			resultFrame.emotionShapeDef = lastEmotionShapeDef;
		}
	}
}