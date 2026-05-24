using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace YaOpt.Patches.Compatibility
{
	internal static class Unpatcher
	{
		private static bool unpatchedPo;

		private static bool unpatchedSm;

		private static bool unpatchedWyau;

		private static bool unpatchedVmf;

		private static bool shouldRecoverWyau;

		public static void Unpatch()
		{
			if ((YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelWorkGiver.Enabled) &&
				YaOptGlobal.HasMod("Taranchuk.PerformanceOptimizer"))
			{
				unpatchedPo = true;
				YaOptGlobal.Harmony.Unpatch(AccessTools.Method(
						typeof(JobGiver_ConfigurableHostilityResponse),
						"TryGiveJob"),
					HarmonyPatchType.Prefix, "PerformanceOptimizer.Main");

				YaOptGlobal.Harmony.Unpatch(AccessTools.Method(
						typeof(ForbidUtility),
						nameof(ForbidUtility.IsForbidden), new[] { typeof(Thing), typeof(Pawn) }),
					HarmonyPatchType.All, "PerformanceOptimizer.Main");
			}

			if (YaOptGlobal.Settings.OptParallelWorkGiver.Enabled &&
				YaOptGlobal.HasType("SmartMedicine.UseTempSleepSpot"))
			{
				unpatchedSm = true;
				YaOptGlobal.Harmony.Unpatch(AccessTools.Method(
						typeof(JobGiver_PatientGoToBed),
						"TryGiveJob"),
					HarmonyPatchType.Prefix, "uuugggg.rimworld.SmartMedicine.main");
			}

			shouldRecoverWyau = true;
			if (YaOptGlobal.Settings.OptParallelWorkGiver.Enabled && YaOptGlobal.HasType("WhileYoureUp.Mod"))
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
			}

			if (YaOptGlobal.NeedThreadSafe &&
				YaOptGlobal.HasMod("oels.vehiclemapframework"))
			{
				unpatchedVmf = true;
				YaOptGlobal.Harmony.Unpatch(AccessTools.PropertyGetter(
						typeof(PawnsFinder),
						nameof(PawnsFinder.AllMaps)),
					HarmonyPatchType.Transpiler, "OELS.VehicleMapFramework");
				YaOptGlobal.Harmony.Unpatch(AccessTools.PropertyGetter(
						typeof(PawnsFinder),
						nameof(PawnsFinder.AllMaps_Spawned)),
					HarmonyPatchType.Transpiler, "OELS.VehicleMapFramework");
			}
		}

		public static void PatchAgain()
		{
			try
			{
				// For now we can only recover WhileYoureUp unpatching
				if (unpatchedWyau && shouldRecoverWyau)
				{
					unpatchedWyau = false;
					shouldRecoverWyau = false;

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
				}
			}
			catch (Exception ex)
			{
				YaOptMod.Error(ex.ToString());
			}
		}
	}
}