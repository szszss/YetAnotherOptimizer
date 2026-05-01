using HarmonyLib;
using System.Reflection;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.Compatibility.AdaptiveStorageFramework
{
	[HarmonyPatch]
	internal static class AdaptiveStorage_Utility_ItemRenderControls_RestoreItemGUIOverlay
	{
		static MethodBase TargetMethod()
		{
			return AccessTools.Method(
				AccessTools.TypeByName("AdaptiveStorage.Utility.ItemRenderControls"),
				"RestoreItemGUIOverlay");
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptFastListerRemove.Enabled &&
			       YaOptGlobal.HasMod("adaptive.storage.framework");
		}

		static bool Prefix(Thing item, Map map)
		{
			var lister = map.listerThings;
			var indexType = (int)ThingRequestGroup.HasGUIOverlay;
			var list = ListerThingsHelper.GetListsByGroup(lister)[indexType];
			var indexer = ListerThingsIndexer.GetListerThingsIndex(lister);
			var record = indexer.GetThingRecord(item, ListerThingsUse.Global);
			if (record.GroupIndex[indexType] >= 0)
				return false;
			ListerThingsHelper.AddToThingList(list, item, record, ListerThingsUse.Global, indexType);
			return false;
		}
	}
}