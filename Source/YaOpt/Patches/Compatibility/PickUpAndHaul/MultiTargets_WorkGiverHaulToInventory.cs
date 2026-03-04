using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.Compatibility.PickUpAndHaul
{
	[HarmonyPatch]
	internal static class MultiTargets_WorkGiverHaulToInventory
	{
		[ThreadStatic]
		public static HashSet<IntVec3> SkipCells;

		[ThreadStatic]
		public static HashSet<Thing> SkipThings;

		static IEnumerable<MethodBase> TargetMethods()
		{
			var type = AccessTools.TypeByName("PickUpAndHaul.WorkGiver_HaulToInventory");
			yield return AccessTools.Method(type, "JobOnThing");
			yield return AccessTools.Method(type, "TryFindBestBetterStoreCellFor");
			yield return AccessTools.Method(type, "TryFindBestBetterNonSlotGroupStorageFor");
		}

		static bool Prepare(MethodBase original)
		{
			if (original != null)
				return true;

			return YaOptGlobal.Settings.OptParallelJobGiver.Enabled &&
				   YaOptGlobal.HasType("PickUpAndHaul.WorkGiver_HaulToInventory") &&
				   // WorkGiver_HaulToInventory will be main-thread only when WhileYoureUp is present
				   !YaOptGlobal.HasType("WhileYoureUp.Mod");
		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.LoadsField("skipCells", true) || instruction.StoresField("skipCells", true))
				{
					instruction.operand = AccessTools.Field(
						typeof(MultiTargets_WorkGiverHaulToInventory),
						nameof(SkipCells));
				}
				else if (instruction.LoadsField("skipThings", true) || instruction.StoresField("skipThings", true))
				{
					instruction.operand = AccessTools.Field(
						typeof(MultiTargets_WorkGiverHaulToInventory),
						nameof(SkipThings));
				}
				yield return instruction;
			}
		}
	}
}