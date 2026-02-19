using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch(typeof(Building_Bed))]
	[HarmonyPatch(nameof(Building_Bed.FindPreferredInteractionCell))]
	internal static class RimWorld_Building_Bed_FindPreferredInteractionCell
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var local = generator.DeclareLocal(typeof(List<IntVec3>));
			yield return CodeInstruction.Call(
				typeof(ThreadLocalTmpList<Building_Bed, IntVec3>),
				nameof(ThreadLocalTmpList<Building_Bed, IntVec3>.Get));
			yield return CodeInstruction.StoreLocal(local.LocalIndex);
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldsfld && instruction.operand is FieldInfo fieldInfo &&
					fieldInfo.Name == "tmpOrderedInteractionCells")
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