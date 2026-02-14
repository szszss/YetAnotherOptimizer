using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Unity.Jobs;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptEarlyRenderPrepare"/>
	/// </summary>
	[HarmonyPatch(typeof(DynamicDrawManager))]
	[HarmonyPatch("ComputeCulledThings")]
	internal static class Verse_DynamicDrawManager_ComputeCulledThings
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptEarlyRenderPrepare.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo methodInfo &&
					methodInfo.Name == "Complete")
				{
					yield return new CodeInstruction(OpCodes.Pop);
					yield return CodeInstruction.LoadLocal(7);
					yield return CodeInstruction.StoreField(
						typeof(ParallelPreDrawHelper),
						nameof(ParallelPreDrawHelper.CullJobHandle));
					yield return CodeInstruction.Call(typeof(JobHandle), nameof(JobHandle.ScheduleBatchedJobs));
					continue;
				}
				yield return instruction;
			}
		}
	}
}