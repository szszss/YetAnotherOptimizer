using FacialAnimation;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using YaOpt.Helpers;

namespace YaOpt.OtherMod.FacialAnimation.Patches
{
	/// <summary>
	/// Replaces LINQ-based animation updates with GC-friendly loops.
	/// </summary>
	/// <seealso cref="SubMod.OptFADeLinq"/>
	[HarmonyPatch(typeof(AnimationFrameAccumulator))]
	[HarmonyPatch(nameof(AnimationFrameAccumulator.UpdateAninmation))]
	internal static class FacialAnimation_AnimationFrameAccumulator_UpdateAninmation
	{
		static bool Prepare(MethodBase original)
		{
			if (original != null)
			{
				MiscHelper.CheckHash(original, Hash128.Parse("5f0339bcc56180bb826cb18bb9d05e65"));
			}
			return SubMod.OptFADeLinq.Enabled;
		}

		static bool Prefix(AnimationFrameAccumulator __instance,
			IEnumerable<FaceAnimation> currentJobAnimationList,
			int tickGame, bool isStanding,
			List<FaceAnimation> temporaryAnimationList,
			ref FaceAnimationDef.AnimationFrame __result, ref int ___lastTick,
			List<FaceAnimationDef.AnimationFrame> ___accumFrames)
		{
			var currentJobAnimationListReused = currentJobAnimationList as List<FaceAnimation>;
			if (___lastTick > tickGame)
			{
				if (currentJobAnimationListReused != null)
				{
					for (var i = 0; i < currentJobAnimationListReused.Count; i++)
					{
						currentJobAnimationListReused[i].Reset(tickGame);
					}
				}
				else
				{
					foreach (FaceAnimation faceAnimation in currentJobAnimationList)
					{
						faceAnimation.Reset(tickGame);
					}
				}
			}
			___lastTick = tickGame;
			if (currentJobAnimationListReused != null)
			{
				for (var i = 0; i < currentJobAnimationListReused.Count; i++)
				{
					var faceAnimation2 = currentJobAnimationListReused[i];
					if (faceAnimation2.IsFinished(tickGame))
					{
						faceAnimation2.Reset(tickGame);
					}
					if (!faceAnimation2.animationDef.applyWhenStandingOnly || isStanding)
					{
						var frame = faceAnimation2.GetCurrentFrame(tickGame);
						if (frame != null)
						{
							___accumFrames.Add(frame);
						}
					}
				}
			}
			else
			{
				foreach (FaceAnimation faceAnimation2 in currentJobAnimationList)
				{
					if (faceAnimation2.IsFinished(tickGame))
					{
						faceAnimation2.Reset(tickGame);
					}
					if (!faceAnimation2.animationDef.applyWhenStandingOnly || isStanding)
					{
						var frame = faceAnimation2.GetCurrentFrame(tickGame);
						if (frame != null)
						{
							___accumFrames.Add(frame);
						}
					}
				}
			}


			foreach (FaceAnimation faceAnimation3 in temporaryAnimationList)
			{
				if (!faceAnimation3.IsFinished(tickGame) && (!faceAnimation3.animationDef.applyWhenStandingOnly || isStanding))
				{
					var frame = faceAnimation3.GetCurrentFrame(tickGame);
					if (frame != null)
					{
						___accumFrames.Add(frame);
					}
				}
			}
			for (var i = temporaryAnimationList.Count - 1; i >= 0; i--)
			{
				if (temporaryAnimationList[i].IsFinished(tickGame))
					temporaryAnimationList.RemoveAt(i);
			}
			__result = __instance.AccumResultFrameAndClear();
			return false;
		}

		// from temporaryAnimationList.RemoveAll((FaceAnimation x) => x.IsFinished(tickGame));
		// to ClearTemporaryAnimationList(temporaryAnimationList, tickGame)
		/*
			from:
			ldarg.s	temporaryAnimationList (4)
			ldloc.0
			ldftn	FacialAnimation.AnimationFrameAccumulator blabla...
			newobj	System.Predicate`1 blabla...
			callvirt	List::RemoveAll blabla...
			pop
			
			to:
			ldarg.s	temporaryAnimationList (4)
			ldloc.0
			pop
			ldarg.2
			call ClearTemporaryAnimationList blabla...
			ldc_i4_0
			pop
		 */
		/*private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldftn)
					continue;
				if (instruction.opcode == OpCodes.Newobj && instruction.operand is MethodBase ctor &&
				    ctor.DeclaringType.Name.Contains("Predicate"))
					continue;
				if (instruction.opcode == OpCodes.Callvirt && instruction.operand is MethodInfo methodInfo &&
				    methodInfo.Name == "RemoveAll")
				{
					yield return new CodeInstruction(OpCodes.Pop); // Pop ldloc0
					yield return CodeInstruction.LoadArgument(2); // tickGame
					yield return CodeInstruction.Call(typeof(FacialAnimation_AnimationFrameAccumulator_UpdateAninmation),
						nameof(ClearTemporaryAnimationList));
					// Push a dummy value into the stack so that the next POP instruction can pop it out
					yield return new CodeInstruction(OpCodes.Ldc_I4_0);
					continue;
				}
				yield return instruction;
			}
		}

		static void ClearTemporaryAnimationList(List<FaceAnimation> temporaryAnimationList, int tickGame)
		{
			var tmpList = ThreadLocalTmpList<AnimationFrameAccumulator, FaceAnimation>.Get();
			foreach (var animation in temporaryAnimationList)
			{
				if (!animation.IsFinished(tickGame))
					tmpList.Add(animation);
			}
			temporaryAnimationList.Clear();
			temporaryAnimationList.AddRange(tmpList);
			tmpList.Clear();
		}*/
	}
}