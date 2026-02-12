using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Verse;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(MapCellsInRandomOrder))]
	[HarmonyPatch("CreateListIfShould")]
	internal static class Verse_MapCellsInRandomOrder_CreateListIfShould
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelPostMapTick.Enabled;
		}

		static bool Prefix(MapCellsInRandomOrder __instance, List<IntVec3> ___randomizedCells, out bool __state)
		{
			__state = false;
			if (___randomizedCells != null)
				return false;
			Monitor.Enter(__instance, ref __state);
			return true;
		}

		static void Finalizer(MapCellsInRandomOrder __instance, bool __state)
		{
			if (__state)
				Monitor.Exit(__instance);
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var local = generator.DeclareLocal(typeof(List<IntVec3>));
			var nullCheckPassed = false;
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ret)
				{
					nullCheckPassed = true;
				}
				else if (instruction.opcode == OpCodes.Stfld && instruction.operand is FieldInfo fieldInfo1 &&
					fieldInfo1.Name == "randomizedCells")
				{
					yield return CodeInstruction.StoreLocal(local.LocalIndex);
					yield return new CodeInstruction(OpCodes.Pop);
					continue;
				}
				else if (instruction.opcode == OpCodes.Ldfld && instruction.operand is FieldInfo fieldInfo2 &&
						 fieldInfo2.Name == "randomizedCells" && nullCheckPassed)
				{
					yield return new CodeInstruction(OpCodes.Pop);
					yield return CodeInstruction.LoadLocal(local.LocalIndex);
					continue;
				}

				yield return instruction;

				if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo methodInfo &&
					methodInfo.Name == "PopState")
				{
					yield return CodeInstruction.LoadArgument(0);
					yield return CodeInstruction.LoadLocal(local.LocalIndex);
					yield return CodeInstruction.StoreField(typeof(MapCellsInRandomOrder), "randomizedCells");
				}
			}
		}
	}
}