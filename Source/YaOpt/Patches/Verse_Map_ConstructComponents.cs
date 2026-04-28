using HarmonyLib;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// </summary>
	/// <seealso cref="YaOptSettings.OptFastListerRemove"/>
	[HarmonyPatch(typeof(Map))]
	[HarmonyPatch(nameof(Map.ConstructComponents))]
	[HarmonyPriority(Priority.HigherThanNormal)]
	internal static class Verse_Map_ConstructComponents
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptFastListerRemove.Enabled;
		}

		static void Postfix(Map __instance)
		{
			ListerThingsIndexer.Create(__instance.listerThings);
		}
	}
}