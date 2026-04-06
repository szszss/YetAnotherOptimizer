using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch(typeof(ThoughtWorker_Precept_AltarSharing))]
	[HarmonyPatch("SharedAltar")]
	internal static class RimWorld_ThoughtWorker_Precept_AltarSharing_SharedAltar
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelPawnTick.Enabled &&
				   YaOptGlobal.Settings.ParallelPawnMoodUpdate;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			instructions = ThreadLocalHelper.ThreadLocalTranspiler(instructions, generator, "tmpPawnIdeoBuildingRooms");
			return ThreadLocalHelper.ThreadLocalTranspiler(instructions, generator, "tmpDifferentIdeoStyleRooms");
		}
	}
}