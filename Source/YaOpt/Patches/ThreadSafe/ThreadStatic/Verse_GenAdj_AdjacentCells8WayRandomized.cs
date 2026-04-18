using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace YaOpt.Patches.ThreadSafe.ThreadStatic
{
	[HarmonyPatch(typeof(GenAdj))]
	[HarmonyPatch(nameof(GenAdj.AdjacentCells8WayRandomized))]
	internal static class Verse_GenAdj_AdjacentCells8WayRandomized
	{
		[ThreadStatic]
		public static List<IntVec3> AdjRandomOrderList;

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if ((instruction.opcode == OpCodes.Ldsfld || instruction.opcode == OpCodes.Stsfld) &&
					instruction.operand is FieldInfo fieldInfo && fieldInfo.Name == "adjRandomOrderList")
				{
					instruction.operand = AccessTools.Field(
						typeof(Verse_GenAdj_AdjacentCells8WayRandomized),
						nameof(AdjRandomOrderList));
				}
				yield return instruction;
			}
		}
	}
}