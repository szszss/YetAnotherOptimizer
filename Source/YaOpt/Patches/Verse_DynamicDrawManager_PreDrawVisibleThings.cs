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
	/// Defers pre-draw job completion and reduces batch size for better work-stealing.
	/// </summary>
	/// <seealso cref="YaOptSettings.OptEarlyRenderPrepare"/>
	/// <seealso cref="YaOptSettings.OptPrepareBatchCount"/>
	[HarmonyPatch(typeof(DynamicDrawManager))]
	[HarmonyPatch("PreDrawVisibleThings")]
	internal static class Verse_DynamicDrawManager_PreDrawVisibleThings
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptEarlyRenderPrepare.Enabled || YaOptGlobal.Settings.OptPrepareBatchCount.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var prp = YaOptGlobal.Settings.OptEarlyRenderPrepare.Enabled;
			var pbc = YaOptGlobal.Settings.OptPrepareBatchCount.Enabled;
			foreach (var instruction in instructions)
			{
				/*if (instruction.opcode == OpCodes.Initobj && instruction.operand is MethodBase method &&
				    method.DeclaringType.Name.Contains("JobHandle"))
				{
					yield return new CodeInstruction(OpCodes.Pop);
					yield return CodeInstruction.LoadField(
						typeof(ParallelPreDrawHelper), 
						nameof(ParallelPreDrawHelper.CullJobHandle));
					yield return CodeInstruction.StoreLocal(2);
					continue;
				}*/
				if (prp && instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo methodInfo &&
					methodInfo.Name == "Complete")
				{
					yield return new CodeInstruction(OpCodes.Pop);
					yield return CodeInstruction.LoadLocal(2);
					yield return CodeInstruction.StoreField(
						typeof(ParallelPreDrawHelper),
						nameof(ParallelPreDrawHelper.PreDrawJobHandle));
					yield return CodeInstruction.Call(typeof(JobHandle), nameof(JobHandle.ScheduleBatchedJobs));
					continue;
				}
				if (pbc && instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo methodInfo2 &&
					methodInfo2.Name == "GetIdealBatchCount")
				{
					yield return new CodeInstruction(OpCodes.Pop);
					yield return new CodeInstruction(OpCodes.Ldc_I4, 4);
					continue;
				}
				yield return instruction;
			}
		}
	}
}