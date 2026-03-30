using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <seealso cref="YaOptSettings.OptParallelJobGiver"/>
	[HarmonyPatch]
	internal static class MultiTargets_ClosestThingGlobal
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(GenClosest), nameof(GenClosest.ClosestThing_Global));
			yield return AccessTools.Method(typeof(GenClosest), nameof(GenClosest.ClosestThing_Global_Reachable));
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelJobGiver.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				yield return instruction;

				if (instruction.Calls("get_Count"))
				{
					yield return CodeInstruction.Call(
						typeof(ParallelJobGiver),
						nameof(ParallelJobGiver.ClosestThingGlobalFastEscape),
						new[] { typeof(int) });
				}
				else if (instruction.Calls("MoveNext"))
				{
					yield return CodeInstruction.Call(
						typeof(ParallelJobGiver),
						nameof(ParallelJobGiver.ClosestThingGlobalFastEscape),
						new[] { typeof(bool) });
				}
			}
		}
	}
}