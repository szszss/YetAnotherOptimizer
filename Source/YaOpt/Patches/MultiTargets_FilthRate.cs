using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace YaOpt.Patches
{
	/// <summary>
	/// Caches filth rate stat lookups with 60-tick intervals.
	/// </summary>
	/// <seealso cref="YaOptSettings.OptStatCache"/>
	[HarmonyPatch]
	internal static class MultiTargets_FilthRate
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(Pawn_FilthTracker),
				nameof(Pawn_FilthTracker.Notify_EnteredNewCell));
			yield return AccessTools.Method(typeof(Alert_AnimalFilth), "CalculateTargets");
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
					list[i - 1] = new CodeInstruction(OpCodes.Ldc_I4, 60);
				}
			}
			return list;
		}
	}
}