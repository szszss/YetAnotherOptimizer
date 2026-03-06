using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace YaOpt.Patches.Compatibility.WhileYouAreUp
{
	internal static class MultiTargets_PUAHMainThreadOnly
	{
		[HarmonyPatch]
		private static class Patch1
		{
			static IEnumerable<MethodBase> TargetMethods()
			{
				var type = AccessTools.TypeByName("WhileYoureUp.Mod");
				yield return AccessTools.Method(type, "PushHtiMethod");
				yield return AccessTools.Method(type, "PopHtiMethod");
			}

			static bool Prepare()
			{
				return YaOptGlobal.Settings.OptParallelJobGiver.Enabled &&
					   YaOptGlobal.HasType("PickUpAndHaul.WorkGiver_HaulToInventory") &&
					   YaOptGlobal.HasType("WhileYoureUp.Mod");
			}

			static bool Prefix()
			{
				return YaOptGlobal.IsInMainThread;
			}
		}

		[HarmonyPatch]
		private static class Patch2
		{
			static MethodBase TargetMethod()
			{
				return AccessTools.Method(
					AccessTools.TypeByName("WhileYoureUp.Mod")
						.GetNestedType("StoreUtility__TryFindBestBetterStoreCellFor_Patch",
							BindingFlags.Static | BindingFlags.NonPublic),
					"DetourAware_TryFindStore");
			}

			static bool Prepare()
			{
				return YaOptGlobal.Settings.OptParallelJobGiver.Enabled &&
					   YaOptGlobal.HasType("PickUpAndHaul.WorkGiver_HaulToInventory") &&
					   YaOptGlobal.HasType("WhileYoureUp.Mod");
			}

			static bool Prefix(ref bool __result)
			{
				var isMain = YaOptGlobal.IsInMainThread;
				if (!isMain)
					__result = true;
				return isMain;
			}
		}

		[HarmonyPatch(typeof(ListerHaulables))]
		[HarmonyPatch(nameof(ListerHaulables.ThingsPotentiallyNeedingHauling))]
		private static class Patch3
		{
			private static AccessTools.FieldRef<List<Thing>> _thingsInReducedPriorityStoreFieldRef;

			private static List<Thing> _tmpHualingThings;

			static bool Prepare()
			{
				var shouldDo = YaOptGlobal.Settings.OptParallelJobGiver.Enabled &&
							   YaOptGlobal.HasType("PickUpAndHaul.WorkGiver_HaulToInventory") &&
							   YaOptGlobal.HasType("WhileYoureUp.Mod");
				if (shouldDo && _thingsInReducedPriorityStoreFieldRef == null)
				{
					_thingsInReducedPriorityStoreFieldRef =
						AccessTools.StaticFieldRefAccess<List<Thing>>(
							AccessTools.Field(
								AccessTools.TypeByName("WhileYoureUp.Mod"),
								"thingsInReducedPriorityStore"));
					_tmpHualingThings = new List<Thing>();
				}
				return shouldDo;
			}

			static void Postfix(ref ICollection<Thing> __result)
			{
				if (YaOptGlobal.IsInMainThread)
				{
					var thingsInReducedPriorityStore = _thingsInReducedPriorityStoreFieldRef();
					if (thingsInReducedPriorityStore?.Count > 0)
					{
						_tmpHualingThings.Clear();
						_tmpHualingThings.AddRangeFast(__result);
						_tmpHualingThings.AddRangeFast(thingsInReducedPriorityStore);
						__result = _tmpHualingThings;
					}
				}
			}
		}
	}
}