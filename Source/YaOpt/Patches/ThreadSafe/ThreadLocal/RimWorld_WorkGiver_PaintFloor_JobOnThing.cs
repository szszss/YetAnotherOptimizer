using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch(typeof(WorkGiver_PaintFloor))]
	[HarmonyPatch(nameof(WorkGiver_PaintFloor.JobOnCell))]
	internal static class RimWorld_WorkGiver_PaintFloor_JobOnThing
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelJobGiver.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var local = generator.DeclareLocal(typeof(List<Thing>));
			yield return CodeInstruction.Call(
				typeof(ThreadLocalTmpList<WorkGiver_PaintFloor, Thing>),
				nameof(ThreadLocalTmpList<WorkGiver_PaintFloor, Thing>.Get));
			yield return CodeInstruction.StoreLocal(local.LocalIndex);
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Stsfld && instruction.operand is FieldInfo fieldInfo1 &&
				    fieldInfo1.Name == "tmpDye")
				{
					yield return CodeInstruction.StoreLocal(local.LocalIndex)
						.WithLabels(instruction.labels).WithBlocks(instruction.blocks);
					continue;
				}
				if (instruction.opcode == OpCodes.Ldsfld && instruction.operand is FieldInfo fieldInfo2 &&
				    fieldInfo2.Name == "tmpDye")
				{
					yield return CodeInstruction.LoadLocal(local.LocalIndex)
						.WithLabels(instruction.labels).WithBlocks(instruction.blocks);
					continue;
				}
				yield return instruction;
			}
		}
	}
}