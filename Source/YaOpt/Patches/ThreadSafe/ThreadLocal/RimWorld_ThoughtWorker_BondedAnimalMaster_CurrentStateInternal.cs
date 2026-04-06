using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch(typeof(ThoughtWorker_BondedAnimalMaster))]
	[HarmonyPatch("CurrentStateInternal")]
	internal static class RimWorld_ThoughtWorker_BondedAnimalMaster_CurrentStateInternal
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelPawnTick.Enabled &&
				   YaOptGlobal.Settings.ParallelPawnMoodUpdate;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			return ThreadLocalHelper.ThreadLocalTranspiler(instructions, generator, "tmpAnimals");
		}
	}
}