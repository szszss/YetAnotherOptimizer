using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse.AI;
using YaOpt.Helpers;

namespace YaOpt.Patches.ThreadSafe
{
	//[HarmonyPatch(typeof(JobMaker))]
	//[HarmonyPatch(nameof(JobMaker.ReturnToPool))]
	[Obsolete]
	internal static class Verse_JobMaker_ReturnToPool
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo methodInfo)
				{
					// from SimplePool<Job>.FreeItemsCount
					// to ConcurrentPool<Job>.FreeItemsCount
					if (methodInfo.Name.Contains("FreeItemsCount"))
					{
						instruction.operand = AccessTools.PropertyGetter(
							typeof(ConcurrentPool<Job>), nameof(ConcurrentPool<Job>.FreeItemsCount));
					}
					// from SimplePool<Job>.Return(job)
					// to ConcurrentPool<Job>.Return(job)
					else if (methodInfo.Name.Contains("Return"))
					{
						instruction.operand = AccessTools.Method(
							typeof(ConcurrentPool<Job>), nameof(ConcurrentPool<Job>.Return));
					}
				}

				yield return instruction;
			}
		}
	}
}