namespace YaOpt.Patches.Prepatch
{
	internal static class Prepatcher
	{
		public static void Init()
		{
			if (YaOptGlobal.IsPrepatcherAvailable)
			{
				Verse_ContentFinder_Get.Enabled = YaOptGlobal.Settings.OptLazyTextureLoad.Enabled;
				Verse_ContentFinder_GetAllInFolder.Enabled = YaOptGlobal.Settings.OptLazyTextureLoad.Enabled;
				Verse_ThingWithComps_GetComp.Enabled = YaOptGlobal.Settings.OptThingGetComp.Enabled;
				Verse_ThingOwnerUtility_GetAllThingsRecursively.Enabled = true; // Always enabled
			}
		}
	}
}