using System;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using YaOpt.Helpers;

namespace YaOpt.Patches.ThreadSafe.Delayed
{
	/// <summary>
	/// Remove DoSteadyEffects from SteadyEnvironmentEffects that affect cell.
	/// They will be playbacked in the main thread later.
	/// </summary>
	[HarmonyPatch(typeof(SteadyEnvironmentEffects))]
	[HarmonyPatch("DoCellSteadyEffects")]
	[HarmonyPriority(Priority.VeryLow)]
	internal static class RimWorld_SteadyEnvironmentEffects_DoCellSteadyEffects
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelPostMapTick.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var list = instructions.ToList();

			// Remove GasUtility.DoSteadyEffects(c, this.map);
			var targetIndex = list.FindLastIndex(inst =>
				inst.opcode == OpCodes.Call && inst.Calls("DoSteadyEffects"));
			if (targetIndex > 0)
			{
				if (list[targetIndex - 1].opcode == OpCodes.Ldfld &&
				    list[targetIndex - 2].opcode == OpCodes.Ldarg_0 &&
				    list[targetIndex - 3].opcode == OpCodes.Ldarg_1)
				{
					list.RemoveRange(targetIndex - 3, 4);
				}
				else
				{
					list[targetIndex] = new CodeInstruction(OpCodes.Pop);
					list.Insert(targetIndex, new CodeInstruction(OpCodes.Pop));
				}
			}
			else
			{
				YaOptMod.Warning("Cannot find GasUtility.DoSteadyEffects. " +
								 "This could be a bug, or it could be that another patch has removed it.");
			}

			// Remove this.map.gameConditionManager.DoSteadyEffects(c, this.map);
			targetIndex = list.FindLastIndex(inst =>
				inst.opcode == OpCodes.Callvirt && inst.Calls("DoSteadyEffects"));
			if (targetIndex > 0)
			{
				if (list[targetIndex - 1].opcode == OpCodes.Ldfld &&
				    list[targetIndex - 2].opcode == OpCodes.Ldarg_0 &&
				    list[targetIndex - 3].opcode == OpCodes.Ldarg_1 &&
				    list[targetIndex - 4].opcode == OpCodes.Ldfld &&
				    list[targetIndex - 5].opcode == OpCodes.Ldfld &&
				    list[targetIndex - 6].opcode == OpCodes.Ldarg_0)
				{
					list.RemoveRange(targetIndex - 5, 6);
					list[targetIndex - 6].opcode = OpCodes.Nop;
				}
				else
				{
					list[targetIndex] = new CodeInstruction(OpCodes.Pop);
					list.Insert(targetIndex, new CodeInstruction(OpCodes.Pop));
					list.Insert(targetIndex, new CodeInstruction(OpCodes.Pop));
				}
			}
			else
			{
				YaOptMod.Warning("Cannot find GameConditionManager.DoSteadyEffects. " +
				                 "This could be a bug, or it could be that another patch has removed it.");
			}

			return list;
		}
	}
}