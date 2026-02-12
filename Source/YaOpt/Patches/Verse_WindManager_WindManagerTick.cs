using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptWindUpdate"/>
	/// </summary>
	[HarmonyPatch(typeof(WindManager))]
	[HarmonyPatch(nameof(WindManager.WindManagerTick))]
	internal static class Verse_WindManager_WindManagerTick
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptWindUpdate.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				// skip WindManager.plantMaterials[i].SetFloat(ShaderPropertyIDs.SwayHead, this.plantSwayHead);
				if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo methodInfo &&
				    methodInfo.Name == "get_CurrentMap")
				{
					instruction.opcode = OpCodes.Ret;
					instruction.operand = null;
					yield return instruction;
					break;
				}
				yield return instruction;
			}
		}

		static void Postfix(Map ___map, List<Material> ___plantMaterials, float ___plantSwayHead)
		{
			if (Find.CurrentMap == ___map)
			{
				WindHelper.CurrentWind = ___plantSwayHead;
				WindHelper.PlantMaterials = ___plantMaterials;
			}
		}
	}
}