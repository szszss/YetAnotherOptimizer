using HarmonyLib;
using RimWorld;
using System;
using Verse;
using YaOpt.Patches.Compatibility.PickUpAndHaul;

namespace YaOpt.Patches.Compatibility
{
	internal static class Unpatcher
	{
		private static bool unpatchedPo;

		private static bool unpatchedSm;

		private static bool unpatchedWyau;

		private static bool shouldRecoverPo;

		private static bool shouldRecoverSm;

		private static bool shouldRecoverWyau;

		public static void Unpatch()
		{
			shouldRecoverPo = true;
			if ((YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelJobGiver.Enabled) &&
				YaOptGlobal.HasMod("Taranchuk.PerformanceOptimizer"))
			{
				unpatchedPo = true;
				shouldRecoverPo = false;
				YaOptGlobal.Harmony.Unpatch(AccessTools.Method(
						typeof(JobGiver_ConfigurableHostilityResponse),
						"TryGiveJob"),
					HarmonyPatchType.Prefix, "PerformanceOptimizer.Main");

				YaOptGlobal.Harmony.Unpatch(AccessTools.Method(
						typeof(ForbidUtility),
						nameof(ForbidUtility.IsForbidden), new[] { typeof(Thing), typeof(Pawn) }),
					HarmonyPatchType.All, "PerformanceOptimizer.Main");
			}

			shouldRecoverSm = true;
			if (YaOptGlobal.Settings.OptParallelJobGiver.Enabled &&
				YaOptGlobal.HasType("SmartMedicine.UseTempSleepSpot"))
			{
				unpatchedSm = true;
				shouldRecoverSm = false;
				YaOptGlobal.Harmony.Unpatch(AccessTools.Method(
						typeof(JobGiver_PatientGoToBed),
						"TryGiveJob"),
					HarmonyPatchType.Prefix, "uuugggg.rimworld.SmartMedicine.main");
			}

			shouldRecoverWyau = true;
			if (YaOptGlobal.Settings.OptParallelJobGiver.Enabled && YaOptGlobal.HasType("WhileYoureUp.Mod"))
			{
				unpatchedWyau = true;
				shouldRecoverWyau = false;

				// Replace this patch with RimWorld_WorkGiver_Haul_JobOnThing
				YaOptGlobal.Harmony.Unpatch(AccessTools.Method(
						typeof(WorkGiver_Scanner),
						"HasJobOnThing"),
					HarmonyPatchType.Postfix, "CodeOptimist.WhileYoureUp");

				// Replace this patch with MultiTargets_PUAHMainThreadOnly/Patch3
				YaOptGlobal.Harmony.Unpatch(AccessTools.Method(
						typeof(ListerHaulables),
						nameof(ListerHaulables.ThingsPotentiallyNeedingHauling)),
					HarmonyPatchType.Postfix, "CodeOptimist.WhileYoureUp");

				/*var fieldInfo = AccessTools.Field(
					AccessTools.TypeByName("WhileYoureUp.Mod"),
					"PuahField_WorkGiver_HaulToInventory_SkipCells");
				fieldInfo.SetValue(null, AccessTools.Field(
					typeof(MultiTargets_WorkGiverHaulToInventory),
					"SkipCells"));*/
			}
		}

		public static void PatchAgain()
		{
			try
			{
				// For now we can only recover WhileYoureUp unpatching
				if (unpatchedWyau && shouldRecoverWyau)
				{
					unpatchedPo = false;
					shouldRecoverPo = false;

					if (AccessTools.Field(
							AccessTools.TypeByName("WhileYoureUp.Mod"),
							"harmony").GetValue(null) is Harmony harmony)
					{
						harmony.Patch(AccessTools.Method(typeof(WorkGiver_Scanner), "HasJobOnThing"),
							null, new HarmonyMethod(AccessTools.Method(
								AccessTools.TypeByName("WhileYoureUp.Mod/" +
								                       "WorkGiver_Scanner__HasJobOnThing_Patch"),
								"ClearTempDetour"
							)));

						harmony.Patch(AccessTools.Method(typeof(ListerHaulables),
								nameof(ListerHaulables.ThingsPotentiallyNeedingHauling)),
							null, new HarmonyMethod(AccessTools.Method(
								AccessTools.TypeByName("WhileYoureUp.Mod/" +
								                       "Puah_ListerHaulables_ThingsPotentiallyNeedingHauling_Patch"),
								"IncludeThingsInReducedPriorityStore"
							)));
					}

					/*var fieldInfo = AccessTools.Field(
						AccessTools.TypeByName("WhileYoureUp.Mod"),
						"PuahField_WorkGiver_HaulToInventory_SkipCells");
					fieldInfo.SetValue(null, AccessTools.Field(
						AccessTools.TypeByName("PickUpAndHaul.WorkGiver_HaulToInventory"),
						"skipCells"));*/
				}
			}
			catch (Exception ex)
			{
				YaOptMod.Error(ex.ToString());
			}
		}
	}
}