using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptStatCache"/>
	/// </summary>
	[HarmonyPatch]
	internal static class MultiTargets_ComfortableTemperature
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(GenTemperature),
				nameof(GenTemperature.ComfortableTemperatureRange),
				new[] { typeof(Pawn) });
			yield return AccessTools.Method(typeof(GenTemperature),
				nameof(GenTemperature.ComfortableTemperatureRange),
				new[] { typeof(Pawn), typeof(List<ThingStuffPair>) });
			yield return AccessTools.Method(typeof(Alert_NeedWarmClothes), "AnyColonistsNeedWarmClothes");
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptStatCache.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var list = instructions.ToList();
			for (var i = 0; i < list.Count; i++)
			{
				var instruction = list[i];
				if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo methodInfo &&
				    methodInfo.Name == "GetStatValue")
				{
					list[i - 1] = new CodeInstruction(OpCodes.Ldc_I4, 10);
				}
			}
			return list;
		}
	}
}