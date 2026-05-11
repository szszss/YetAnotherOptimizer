using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using YaOpt.Helpers;

namespace YaOpt.Patches.Compatibility.SmarterConstruction
{
	[HarmonyPatch(typeof(ParallelWorkGiver))]
	[HarmonyPatch(nameof(ParallelWorkGiver.SmarterConstructionIsConstructFinishFrames))]
	internal static class YaOpt_Helpers_ParallelWorkGiver_SmarterConstructionIsConstructFinishFrames
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelWorkGiver.Enabled &&
				   YaOptGlobal.HasMod("dhultgren.smarterconstruction");
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			// return workGiver is WorkGiver_ConstructFinishFrames;
			yield return CodeInstruction.LoadArgument(0);
			yield return new CodeInstruction(OpCodes.Isinst, typeof(WorkGiver_ConstructFinishFrames));
			yield return new CodeInstruction(OpCodes.Ldnull);
			yield return new CodeInstruction(OpCodes.Cgt_Un);
			yield return new CodeInstruction(OpCodes.Ret);
		}
	}
}