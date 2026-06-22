using HarmonyLib;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.Compatibility.Kyulen
{
	[HarmonyPatch]
	internal static class Ninetail_AmpBodyGuards_IsHumanlikeOrAmp
	{
		private static ConcurrentDictionary<ThingDef, bool> _defIsAmpCache =
			new ConcurrentDictionary<ThingDef, bool>();

		static MethodBase TargetMethod()
		{
			return AccessTools.Method(
				AccessTools.TypeByName("Ninetail.AmpBodyGuards"),
				"IsHumanlikeOrAmp");
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe &&
				   YaOptGlobal.HasMod("bichang.kyulen");
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.LoadsField("DefIsAmpCache", true) ||
					instruction.StoresField("DefIsAmpCache", true))
				{
					instruction.operand = AccessTools.Field(
						typeof(Ninetail_AmpBodyGuards_IsHumanlikeOrAmp),
						nameof(_defIsAmpCache));
				}
				else if (instruction.Calls("TryGetValue"))
				{
					instruction.operand = AccessTools.Method(
						typeof(ConcurrentDictionary<ThingDef, bool>),
						nameof(ConcurrentDictionary<ThingDef, bool>.TryGetValue));
				}
				else if (instruction.Calls("set_Item"))
				{
					instruction.operand = AccessTools.Method(
						typeof(ConcurrentDictionary<ThingDef, bool>),
						nameof(ConcurrentDictionary<ThingDef, bool>.TryAdd));
					yield return instruction;
					yield return new CodeInstruction(OpCodes.Pop);
					continue;
				}
				yield return instruction;
			}
		}
	}
}