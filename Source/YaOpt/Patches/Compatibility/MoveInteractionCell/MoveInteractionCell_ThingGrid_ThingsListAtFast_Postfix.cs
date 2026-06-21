using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using YaOpt.Helpers;

namespace YaOpt.Patches.Compatibility.MoveInteractionCell
{
	internal static class MoveInteractionCell_ThingGrid_ThingsListAtFast_Postfix
	{
		[ThreadStatic]
		internal static bool InterceptThingListFast;

		static MethodBase TargetMethod()
		{
			return AccessTools.Method(
				AccessTools.TypeByName("MoveInteractionCell.ThingGrid_ThingsListAtFast"),
				"Postfix");
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe &&
				   YaOptGlobal.HasType("MoveInteractionCell.ThingGrid_ThingsListAtFast");
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.LoadsField("InterceptThingListFast", true))
					instruction.operand = AccessTools.Field(
						typeof(MoveInteractionCell_ThingGrid_ThingsListAtFast_Postfix),
						nameof(InterceptThingListFast));
				yield return instruction;
			}
		}
	}
}