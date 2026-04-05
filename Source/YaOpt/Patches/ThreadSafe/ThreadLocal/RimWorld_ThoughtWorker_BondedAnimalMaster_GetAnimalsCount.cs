using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch(typeof(ThoughtWorker_BondedAnimalMaster))]
	[HarmonyPatch(nameof(ThoughtWorker_BondedAnimalMaster.GetAnimalsCount))]
	internal static class RimWorld_ThoughtWorker_BondedAnimalMaster_GetAnimalsCount
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