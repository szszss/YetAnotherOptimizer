using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.Compatibility.LWMDeepStorage
{
	[HarmonyPatch]
	internal static class MultiTargets_Add
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			// Just one method for now
			yield return AccessTools.Method(
				AccessTools.TypeByName("LWM.DeepStorage.Patch_Building_DeSpawn_For_Building_Storage"),
				"Prefix");
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptFastListerRemove.Enabled &&
			       YaOptGlobal.HasMod("lwm.deepstorage");
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var modifyNextAdd = false;
			foreach (var instruction in instructions)
			{
				if (instruction.LoadsConstant((int)ThingRequestGroup.HasGUIOverlay))
				{
					modifyNextAdd = true;
				}
				if (modifyNextAdd && instruction.Calls("Add"))
				{
					modifyNextAdd = false;
					yield return CodeInstruction.LoadArgument(0); // Building
					yield return CodeInstruction.Call(
						typeof(MultiTargets_Add),
						nameof(Add));
					continue;

				}
				yield return instruction;
			}
		}

		private static void Add(List<Thing> list, Thing thing, Building building)
		{
			var lister = building.Map.listerThings;
			var indexType = (int)ThingRequestGroup.HasGUIOverlay;
			var indexer = ListerThingsIndexer.GetListerThingsIndex(lister);
			var record = indexer.GetThingRecord(thing, ListerThingsUse.Global);
			if (record.GroupIndex[indexType] >= 0)
				return;
			ListerThingsHelper.AddToThingList(list, thing, record, ListerThingsUse.Global, indexType);
		}
	}
}