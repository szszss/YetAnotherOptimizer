using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using YaOpt.Helpers;
using static YaOpt.Helpers.ListerThingsIndexer;

namespace YaOpt.Patches.Compatibility.DigitalStorage
{
	[HarmonyPatch]
	internal static class DigitalStorage_GhostLedgerIndex_RemoveFromListerDirect
	{
		private static AccessTools.FieldRef<ListerThings, Dictionary<ThingDef, List<Thing>>> _listsByDefRef;

		static MethodBase TargetMethod()
		{
			return AccessTools.Method(
				AccessTools.TypeByName("DigitalStorage.Ghost.GhostLedgerIndex"),
				"RemoveFromListerDirect");
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

		static bool Prefix(MapComponent __instance, Thing t)
		{
			var lister = __instance.map.listerThings;

			var listsByDef = _listsByDefRef(lister);
			if (!listsByDef.TryGetValue(t.def, out var list))
				return false;

			var indexer = GetListerThingsIndex(lister);
			var record = indexer.TryGetThingRecord(t, lister.use);

			if (record != null && record.DefIndex >= 0)
			{
				ListerThingsHelper.RemoveFromThingList(
					list, t, indexer, record, ListerThingsUse.Global,
					ListerThingsHelper.INDEX_TYPE_DEF);
			}
			else
			{
				list.Remove(t);
			}

			return false;
		}
	}
}
