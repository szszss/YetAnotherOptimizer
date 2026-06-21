using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using YaOpt.Helpers;

namespace YaOpt.Patches.Compatibility.MoveInteractionCell
{
	internal static class MoveInteractionCell_GridsUtility_GetThingList_Postfix
	{
		[ThreadStatic]
		internal static bool InterceptThingList;

		static MethodBase TargetMethod()
		{
			return AccessTools.Method(
				AccessTools.TypeByName("MoveInteractionCell.GridsUtility_GetThingList"),
				"Postfix");
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe &&
				   YaOptGlobal.HasType("MoveInteractionCell.GridsUtility_GetThingList");
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.LoadsField("InterceptThingList", true))
					instruction.operand = AccessTools.Field(
						typeof(MoveInteractionCell_GridsUtility_GetThingList_Postfix),
						nameof(InterceptThingList));
				yield return instruction;
			}
		}
	}
}