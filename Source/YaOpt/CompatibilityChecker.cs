using HarmonyLib;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace YaOpt
{
	public static class CompatibilityChecker
	{
		private const int MAX_CHECK_TIMES = 2;

		private static int _checkTimes = 0;

		public static bool HasProblem;

		private static readonly MethodInfo _settingsWrite = AccessTools.Method(
			typeof(ModSettings), nameof(ModSettings.Write));

		/// <summary>
		/// Check for compatibility with other optimizer mods.
		/// </summary>
		/// <param name="canForceDo">If true, allows changing incompatible options for other mods.</param>
		/// <param name="canSave">If true, allows saving other mod settings after changing.</param>
		/// <param name="silent">Don't print error message.</param>
		/// <param name="ignoreMaxTimes">Ignore the max check times.</param>
		public static void Check(bool canForceDo, bool canSave = false, bool silent = false, bool ignoreMaxTimes = false)
		{
			// Compatibility checks are quite heavy, so we only perform them twice at most
			// (usually once when starting the game and once when loading a save for the first time).
			if (!ignoreMaxTimes)
			{
				if (_checkTimes >= MAX_CHECK_TIMES)
					return;
				_checkTimes++;
			}

			var noProblem = true;
			noProblem &= CheckButterPlusPlus(canForceDo, canSave, silent);
			CheckPerformanceOptimizer(silent);
			noProblem &= CheckPerformanceFish(canForceDo & canSave, canSave, silent);
			HasProblem = !noProblem;
		}

		private static bool CheckButterPlusPlus(bool canForceDo, bool canSave, bool silent)
		{
			if (!YaOptGlobal.HasMod("olli.butterplusplus"))
				return true;
			try
			{
				var typeMod = AccessTools.TypeByName("ButterPlusPlus.ButterPlusPlusMod");
				var typeSettings = AccessTools.TypeByName("ButterPlusPlus.Settings");
				var getterSettings = AccessTools.PropertyGetter(typeMod, "Settings");
				var fieldCompatibilityMode = AccessTools.Field(typeSettings, "compatibilityMode");

				var objSettings = getterSettings.Invoke(null, Array.Empty<object>());
				var cm = (bool)fieldCompatibilityMode.GetValue(objSettings);
				if (!cm)
				{
					string msg = null;
					if (canForceDo)
					{
						fieldCompatibilityMode.SetValue(objSettings, true);
						if (canSave)
							_settingsWrite.Invoke(objSettings, null);
						msg = "YaOpt.Message.ButterCompatibilityForceEnable";
					}
					else
					{
						msg = "YaOpt.Message.ButterCompatibilityNotEnabled";
					}
					if (!silent)
					{
						var str = msg.Translate().ToString();
						Messages.Message(str, null, MessageTypeDefOf.CautionInput, false);
						YaOptMod.Error(str);
					}
					return false;
				}
			}
			catch (Exception e)
			{
				YaOptMod.Error("Butter++ was detected but failed to get the compatibility info: " +
							   e.ToStringSafe());
			}
			return true;
		}

		private static void CheckPerformanceOptimizer(bool silent)
		{
			if (silent)
				return;
			if (!YaOptGlobal.HasMod("Taranchuk.PerformanceOptimizer"))
				return;
			if (!YaOptGlobal.NeedThreadSafe)
				return;
			try
			{
				var typeMod = AccessTools.TypeByName("PerformanceOptimizer.PerformanceOptimizerMod");
				var typePerformPatchesPerFrames = AccessTools.TypeByName("PerformanceOptimizer.PerformPatchesPerFrames");
				var typeFasterGetCompReplacement = AccessTools.TypeByName("PerformanceOptimizer.Optimization_FasterGetCompReplacement");
				var typeOptimization = AccessTools.TypeByName("PerformanceOptimizer.Optimization");
				var fieldPerformPatchesPerFrames = AccessTools.Field(typeMod, "performPatchesPerFrames");
				var fieldOptimization = AccessTools.Field(typePerformPatchesPerFrames, "optimization");
				var getterEnabled = AccessTools.PropertyGetter(typeOptimization, "IsEnabled");

				var objPerformPatchesPerFrames = fieldPerformPatchesPerFrames.GetValue(null);
				var objOptimization = fieldOptimization.GetValue(objPerformPatchesPerFrames);
				if (objOptimization == null)
					return;
				var enabled = getterEnabled.Invoke(objOptimization, null);

				if (enabled is bool b && b)
				{
					YaOptMod.Warning("YaOpt.Message.PerformanceOptimizerGetComp".Translate());
				}
			}
			catch (Exception e)
			{
				YaOptMod.Error("Performance Optimizer was detected but failed to get the compatibility info: " +
							   e.ToStringSafe());
			}
		}

		private static bool CheckPerformanceFish(bool canForceDo, bool canSave, bool silent)
		{
			if (!YaOptGlobal.HasMod("bs.performance"))
				return true;
			try
			{
				var typeMod = AccessTools.TypeByName("PerformanceFish.PerformanceFishMod");
				var getterAllPatchClasses = AccessTools.PropertyGetter(typeMod, "AllPatchClasses");
				var getterAllPrepatchClasses = AccessTools.PropertyGetter(typeMod, "AllPrepatchClasses");
				var objAllPatchClasses = getterAllPatchClasses.Invoke(null, null);
				var objAllPrepatchClasses = getterAllPrepatchClasses.Invoke(null, null);

				var getterIHasFishPatch = AccessTools.PropertyGetter(
					AccessTools.TypeByName("PerformanceFish.Patching.IHasFishPatch"),
					"Patches");
				var getterClassWithFishPrepatches = AccessTools.PropertyGetter(
					AccessTools.TypeByName("PerformanceFish.Prepatching.ClassWithFishPrepatches"),
					"Patches");
				var getterIHasFishPatchAll = AccessTools.PropertyGetter(
					AccessTools.TypeByName("PerformanceFish.Patching.FishPatchHolder"),
					"All");
				var getterClassWithFishPrepatchesAll = AccessTools.PropertyGetter(
					AccessTools.TypeByName("PerformanceFish.Prepatching.FishPrepatchHolder"),
					"All");
				var getterFishPatchEnabled = AccessTools.PropertyGetter(
					AccessTools.TypeByName("PerformanceFish.Patching.FishPatch"),
					"Enabled");
				var getterFishPrepatchBaseEnabled = AccessTools.PropertyGetter(
					AccessTools.TypeByName("PerformanceFish.Prepatching.FishPrepatchBase"),
					"Enabled");
				var setterFishPatchEnabled = AccessTools.PropertySetter(
					AccessTools.TypeByName("PerformanceFish.Patching.FishPatch"),
					"Enabled");
				var setterFishPrepatchBaseEnabled = AccessTools.PropertySetter(
					AccessTools.TypeByName("PerformanceFish.Prepatching.FishPrepatchBase"),
					"Enabled");

				var errors = new List<string>();
				if (objAllPatchClasses is Array array1)
				{
					foreach (var o in array1)
					{
						var holder = getterIHasFishPatch.Invoke(o, null);
						var patches = getterIHasFishPatchAll.Invoke(holder, null) as IDictionary;
						foreach (var patch in patches.Values)
						{
							if (!CheckPerformanceFishHarmonyPatch(patch, out var error) &&
								getterFishPatchEnabled.Invoke(patch, null) is bool b && b)
							{
								if (canForceDo)
								{
									if (patch.GetType().FullName ==
										"PerformanceFish.JobSystem.Toils_BedOptimization+FailOnBedNoLongerUsable_Patch")
									{
										YaOptGlobal.Settings.OptBedThrottle.Enabled = false;
									}
									else
									{
										setterFishPatchEnabled.Invoke(patch, new object[] { false });
									}
								}
								if (!errors.Contains(error))
									errors.Add(error);
							}
						}
					}
				}
				if (objAllPrepatchClasses is Array array2)
				{
					foreach (var o in array2)
					{
						var holder = getterClassWithFishPrepatches.Invoke(o, null);
						var patches = getterClassWithFishPrepatchesAll.Invoke(holder, null) as IDictionary;
						foreach (var patch in patches.Values)
						{
							if (!CheckPerformanceFishPrepatch(patch, out var error) &&
								getterFishPrepatchBaseEnabled.Invoke(patch, null) is bool b && b)
							{
								if (canForceDo)
									setterFishPrepatchBaseEnabled.Invoke(patch, new object[] { false });
								if (!errors.Contains(error))
									errors.Add(error);
							}
						}
					}
				}

				if (errors.Count > 0)
				{
					if (canSave)
					{
						var getterSettings = AccessTools.PropertyGetter(typeMod, "Settings");
						var objSettings = getterSettings.Invoke(null, null);
						_settingsWrite.Invoke(objSettings, null);
						YaOptGlobal.Settings.Write();
					}
					if (!silent)
					{
						Messages.Message("YaOpt.Message.PerformanceFishSeeConsole".Translate(), null, MessageTypeDefOf.CautionInput, false);
						YaOptMod.Panic("YaOpt.Message.PerformanceFishIncompatibility1".Translate());
						YaOptMod.Error("YaOpt.Message.PerformanceFishIncompatibility2".Translate());
						foreach (var error in errors)
						{
							YaOptMod.Error(error);
						}
						YaOptMod.Error("YaOpt.Message.PerformanceFishIncompatibility3".Translate());
					}
					return false;
				}
			}
			catch (Exception e)
			{
				YaOptMod.Error("Performance Fish was detected but failed to get the compatibility info: " +
							   e.ToStringSafe());
			}
			return true;
		}

		private static bool CheckPerformanceFishHarmonyPatch(object patchObj, out string error)
		{
			var type = patchObj.GetType();
			var name = type.FullName;
			if (name == "PerformanceFish.AccessToolsCaching+AllTypes" &&
				YaOptGlobal.Settings.OptRuntimeInfoCache.Enabled)
			{
				error = "YaOpt.Message.PerformanceFish.RuntimeInfoCache".Translate();
				return false;
			}
			if (name == "PerformanceFish.MiscOptimizations+WindManager_WindManagerTick" &&
				YaOptGlobal.Settings.OptWindUpdate.Enabled)
			{
				error = "YaOpt.Message.PerformanceFish.WindUpdate".Translate();
				return false;
			}
			if (name == "PerformanceFish.JobSystem.Toils_BedOptimization+FailOnBedNoLongerUsable_Patch" &&
				YaOptGlobal.Settings.OptBedThrottle.Enabled)
			{
				error = "YaOpt.Message.PerformanceFish.BedThrottle".Translate();
				return false;
			}
			if (name == "PerformanceFish.Hauling.StorageSettingsPatches+AllowedToAcceptPatch" &&
				YaOptGlobal.Settings.OptParallelJobGiver.Enabled)
			{
				error = "YaOpt.Message.PerformanceFish.ParallelJobGiver".Translate();
				return false;
			}
			error = null;
			return true;
		}

		private static bool CheckPerformanceFishPrepatch(object patchObj, out string error)
		{
			var type = patchObj.GetType();
			var name = type.FullName;
			if (name == "PerformanceFish.GetCompCaching+ThingCompPatch" &&
				YaOptGlobal.Settings.OptThingGetComp.Enabled)
			{
				error = "YaOpt.Message.PerformanceFish.ThingGetComp".Translate();
				return false;
			}
			if (name == "PerformanceFish.Rendering.ContentFinderCaching+Get_Patch" &&
				YaOptGlobal.Settings.OptLazyTextureLoad.Enabled)
			{
				error = "YaOpt.Message.PerformanceFish.LazyTextureLoad".Translate();
				return false;
			}
			if (name == "PerformanceFish.Rendering.DynamicDrawManagerPatches+DrawDynamicThingsPatch" &&
				YaOptGlobal.Settings.OptEarlyRenderPrepare.Enabled)
			{
				error = "YaOpt.Message.PerformanceFish.EarlyRenderPrepare".Translate();
				return false;
			}
			if (name == "PerformanceFish.Rendering.PawnRenderingOptimizations+PawnRenderTreeComputeMatrixPatch" &&
				YaOptGlobal.Settings.OptComputeMatrixBurst.Enabled)
			{
				error = "YaOpt.Message.PerformanceFish.ComputeMatrixBurst".Translate();
				return false;
			}
			if ((name == "PerformanceFish.Listers.ThingsPrepatches+GetThingsOfTypeIntoListPatch" ||
				 name == "PerformanceFish.Listers.ThingsPrepatches+AddPatch" ||
				 name == "PerformanceFish.Listers.ThingsPrepatches+ContainsPatch" ||
				 name == "PerformanceFish.Listers.ThingsPrepatches+GetThingsOfTypePatch" ||
				 name == "PerformanceFish.Listers.ThingsPrepatches+RemovePatch" ||
				 name == "PerformanceFish.Listers.ThingsPrepatches+ClearPatch") &&
				YaOptGlobal.Settings.OptFastListerRemove.Enabled)
			{
				error = "YaOpt.Message.PerformanceFish.FastListerRemove".Translate();
				return false;
			}
			error = null;
			return true;
		}
	}
}