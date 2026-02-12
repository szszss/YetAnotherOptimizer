using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptParallelPostMapTick"/>
	/// </summary>
	[HarmonyPatch(typeof(Map))]
	[HarmonyPatch(nameof(Map.MapPostTick))]
	internal static class Verse_Map_MapPostTick
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelPostMapTick.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Callvirt && instruction.operand is MethodInfo methodInfo)
				{
					if (methodInfo.Name == "SteadyEnvironmentEffectsTick")
					{
						yield return new CodeInstruction(OpCodes.Pop);
						continue;
					}
					/*if (methodInfo.Name == "Tick" && "TempTerrainManager" == methodInfo.DeclaringType?.Name)
					{
						yield return new CodeInstruction(OpCodes.Pop);
						continue;
					}*/
					if (methodInfo.Name == "Tick" && "GasGrid" == methodInfo.DeclaringType?.Name)
					{
						yield return new CodeInstruction(OpCodes.Pop);
						continue;
					}
				}
				yield return instruction;
			}
		}
	}
}