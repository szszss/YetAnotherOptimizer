using HarmonyLib;
using System.Reflection;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.Compatibility.AdaptiveStorageFramework
{
	[HarmonyPatch]
	internal static class AdaptiveStorage_Utility_ItemRenderControls_DisableItemGUIOverlay
	{
		static MethodBase TargetMethod()
		{
			return AccessTools.Method(
				AccessTools.TypeByName("AdaptiveStorage.Utility.ItemRenderControls"),
				"DisableItemGUIOverlay");
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
			if (record.GroupIndex[indexType] < 0)
				return false;
			ListerThingsHelper.RemoveFromThingList(list, item, indexer, record, ListerThingsUse.Global, indexType);
			return false;
		}
	}
}