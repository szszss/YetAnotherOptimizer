using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch(typeof(Room))]
	[HarmonyPatch(nameof(Room.Regions), MethodType.Getter)]
	internal static class Verse_Room_Regions
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var local = generator.DeclareLocal(typeof(List<Region>));
			yield return CodeInstruction.Call(
				typeof(ThreadLocalTmpList<Room, Region>),
				nameof(ThreadLocalTmpList<Room, Region>.Get));
			yield return CodeInstruction.StoreLocal(local.LocalIndex);
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldfld && instruction.operand is FieldInfo fieldInfo &&
					fieldInfo.Name == "tmpRegions")
				{
					yield return new CodeInstruction(OpCodes.Pop);
					yield return CodeInstruction.LoadLocal(local.LocalIndex);
					continue;
				}
				yield return instruction;
			}
		}
	}
}