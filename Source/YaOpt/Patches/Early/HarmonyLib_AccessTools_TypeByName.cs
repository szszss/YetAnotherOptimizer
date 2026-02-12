using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using YaOpt.Helpers;

namespace YaOpt.Patches.Early
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptRuntimeInfoCache"/>
	/// </summary>
	[HarmonyPatch(typeof(AccessTools))]
	[HarmonyPatch(nameof(AccessTools.TypeByName))]
	[EarlyPatch]
	internal static class HarmonyLib_AccessTools_TypeByName
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptRuntimeInfoCache.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			const int LOCAL_TYPE = 3;
			var skip = false;
			var label = generator.DefineLabel();
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldstr)
				{
					skip = false;
					// var type = RuntimeInfoCache.GetTypeByName(name);
					yield return CodeInstruction.LoadArgument(0);
					yield return CodeInstruction.Call(
						typeof(RuntimeInfoCache),
						nameof(RuntimeInfoCache.GetTypeByName));
					yield return CodeInstruction.StoreLocal(LOCAL_TYPE);
					// if (type != null) {
					yield return CodeInstruction.LoadLocal(LOCAL_TYPE);
					yield return new CodeInstruction(OpCodes.Brfalse_S, label);
					//     return type;
					yield return CodeInstruction.LoadLocal(LOCAL_TYPE);
					yield return new CodeInstruction(OpCodes.Ret);
					//  }
					yield return instruction.WithLabels(label);
					continue;
				}
				if (skip)
				{
					if (instruction.labels.Count > 0)
					{
						yield return new CodeInstruction(OpCodes.Nop).WithLabels(instruction.labels);
					}
				}
				else
					yield return instruction;
				if (instruction.opcode == OpCodes.Endfinally)
				{
					skip = true;
				}
			}
		}
	}
}