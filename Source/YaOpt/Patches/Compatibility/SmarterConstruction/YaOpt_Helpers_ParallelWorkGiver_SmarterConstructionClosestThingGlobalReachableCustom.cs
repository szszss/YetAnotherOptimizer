using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using YaOpt.Helpers;

namespace YaOpt.Patches.Compatibility.SmarterConstruction
{
	[HarmonyPatch(typeof(ParallelWorkGiver))]
	[HarmonyPatch(nameof(ParallelWorkGiver.SmarterConstructionClosestThingGlobalReachableCustom))]
	internal static class YaOpt_Helpers_ParallelWorkGiver_SmarterConstructionClosestThingGlobalReachableCustom
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelWorkGiver.Enabled &&
				   YaOptGlobal.HasMod("dhultgren.smarterconstruction");
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			// return CustomGenClosest.ClosestThing_Global_Reachable_Custom(...);
			for (var i = 0; i < 9; i++)
			{
				yield return CodeInstruction.LoadArgument(i);
			}
			yield return CodeInstruction.Call(
				AccessTools.TypeByName("SmarterConstruction.Patches.CustomGenClosest"),
				"ClosestThing_Global_Reachable_Custom");
			yield return new CodeInstruction(OpCodes.Ret);
		}
	}
}