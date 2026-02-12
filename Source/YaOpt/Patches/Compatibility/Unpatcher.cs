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
						typeof(Hediff),
						nameof(Hediff.TendableNow)),
					HarmonyPatchType.Transpiler, "PerformanceOptimizer.Main");

				YaOptGlobal.Harmony.Unpatch(AccessTools.Method(
						typeof(HediffUtility),
						nameof(HediffUtility.IsTended)),
					HarmonyPatchType.Transpiler, "PerformanceOptimizer.Main");

				YaOptGlobal.Harmony.Unpatch(AccessTools.Method(
						typeof(HediffUtility),
						nameof(HediffUtility.IsPermanent)),
					HarmonyPatchType.Transpiler, "PerformanceOptimizer.Main");

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
				YaOptGlobal.Harmony.Unpatch(AccessTools.Method(
						typeof(WorkGiver_Scanner),
						"HasJobOnThing"),
					HarmonyPatchType.Postfix, "CodeOptimist.WhileYoureUp");
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
								AccessTools.TypeByName("WhileYoureUp.Mod/WorkGiver_Scanner__HasJobOnThing_Patch"),
								"ClearTempDetour"
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