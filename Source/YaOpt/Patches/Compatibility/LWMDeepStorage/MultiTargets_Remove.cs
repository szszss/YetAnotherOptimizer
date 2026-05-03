using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.Compatibility.LWMDeepStorage
{
	[HarmonyPatch]
	internal static class MultiTargets_Remove
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(
				AccessTools.TypeByName("LWM.DeepStorage.PatchDisplay_SpawnSetup"),
				"Postfix");
			yield return AccessTools.Method(
				AccessTools.TypeByName("LWM.DeepStorage.PatchDisplay_Notify_ReceivedThing"),
				"Postfix");
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptFastListerRemove.Enabled &&
				   YaOptGlobal.HasMod("lwm.deepstorage");
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var modifyNextRemove = false;
			foreach (var instruction in instructions)
			{
				if (instruction.LoadsConstant((int)ThingRequestGroup.HasGUIOverlay))
				{
					modifyNextRemove = true;
				}
				if (modifyNextRemove && instruction.Calls("Remove"))
				{
					modifyNextRemove = false;
					yield return CodeInstruction.LoadArgument(0); // Building_Storage
					yield return CodeInstruction.Call(
						typeof(MultiTargets_Remove),
						nameof(Remove));
					continue;

				}
				yield return instruction;
			}
		}

		private static bool Remove(List<Thing> list, Thing thing, Building_Storage building)
		{
			var lister = building.Map.listerThings;
			var indexType = (int)ThingRequestGroup.HasGUIOverlay;
			var indexer = ListerThingsIndexer.GetListerThingsIndex(lister);
			var record = indexer.GetThingRecord(thing, ListerThingsUse.Global);
			if (record.GroupIndex[indexType] < 0)
				return false;
			ListerThingsHelper.RemoveFromThingList(list, thing, indexer, record, ListerThingsUse.Global, indexType);
			return true;
		}
	}
}