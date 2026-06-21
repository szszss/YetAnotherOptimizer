using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.Compatibility.DigitalStorage
{
	[HarmonyPatch]
	internal static class DigitalStorage_GhostLedgerIndex_AddToListerDirect
	{
		private static AccessTools.FieldRef<ListerThings, Dictionary<ThingDef, List<Thing>>> _listsByDefRef;

		static MethodBase TargetMethod()
		{
			return AccessTools.Method(
				AccessTools.TypeByName("DigitalStorage.Ghost.GhostLedgerIndex"),
				"AddToListerDirect");
		}

		static bool Prepare()
		{
			var shouldDo = YaOptGlobal.Settings.OptFastListerRemove.Enabled &&
						   YaOptGlobal.HasMod("cagier.digitalstorage");
			if (shouldDo && _listsByDefRef == null)
			{
				_listsByDefRef = AccessTools.FieldRefAccess<ListerThings, Dictionary<ThingDef, List<Thing>>>(
					AccessTools.Field(typeof(ListerThings), "listsByDef"));
			}
			return shouldDo;
		}

		static void Postfix(MapComponent __instance, Thing t)
		{
			var lister = __instance.map.listerThings;
			if (lister.use != ListerThingsUse.Global)
				return;

			var indexer = ListerThingsIndexer.GetListerThingsIndex(lister);
			if (indexer.TryGetThingRecord(t, lister.use) != null)
				return;

			var record = indexer.Add(t, lister.use);
			var listsByDef = _listsByDefRef(lister);
			if (listsByDef.TryGetValue(t.def, out var list) && list.Count > 0)
			{
				record.DefIndex = list.Count - 1;
			}
		}
	}
}