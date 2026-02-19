using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch(typeof(ReservationManager))]
	[HarmonyPatch(nameof(ReservationManager.CanReserve))]
	internal static class Verse_AI_ReservationManager_CanReserve
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var local = generator.DeclareLocal(typeof(List<Pawn>));
			yield return CodeInstruction.Call(
				typeof(ThreadLocalTmpList<ReservationManager, Pawn>),
				nameof(ThreadLocalTmpList<ReservationManager, Pawn>.Get));
			yield return CodeInstruction.StoreLocal(local.LocalIndex);
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldfld && instruction.operand is FieldInfo fieldInfo &&
					fieldInfo.Name == "tmpReservers")
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