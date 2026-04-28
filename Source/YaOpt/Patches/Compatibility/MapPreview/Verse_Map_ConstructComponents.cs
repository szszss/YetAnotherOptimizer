using HarmonyLib;
using System.Reflection;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.Compatibility.MapPreview
{
	/// <summary>
	/// </summary>
	/// <seealso cref="YaOptSettings.OptFastListerRemove"/>
	[HarmonyPatch]
	internal static class Verse_MapPreviewGenerator_ConstructMinimalMapComponents
	{
		static MethodBase TargetMethod()
		{
			return AccessTools.Method(AccessTools.TypeByName("MapPreview.MapPreviewGenerator"),
				"ConstructMinimalMapComponents");
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptFastListerRemove.Enabled &&
				   YaOptGlobal.HasType("MapPreview.MapPreviewGenerator");
		}

		static void Postfix(Map __instance)
		{
			ListerThingsIndexer.Create(__instance.listerThings);
		}
	}
}