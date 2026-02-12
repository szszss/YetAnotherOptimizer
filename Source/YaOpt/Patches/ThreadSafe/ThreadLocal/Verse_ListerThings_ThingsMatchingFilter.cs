using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch(typeof(ListerThings))]
	[HarmonyPatch(nameof(ListerThings.ThingsMatchingFilter))]
	internal static class Verse_ListerThings_ThingsMatchingFilter
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var local = generator.DeclareLocal(typeof(List<Thing>));
			yield return CodeInstruction.Call(
				typeof(ThreadLocalTmpList<ListerThings, Thing>),
				nameof(ThreadLocalTmpList<ListerThings, Thing>.Get));
			yield return CodeInstruction.StoreLocal(local.LocalIndex);
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldsfld && instruction.operand is FieldInfo fieldInfo &&
					fieldInfo.Name == "tmpThingsMatchingFilter")
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