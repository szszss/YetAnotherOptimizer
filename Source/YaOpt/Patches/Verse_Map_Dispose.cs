using HarmonyLib;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// </summary>
	/// <seealso cref="YaOptSettings.OptFastListerRemove"/>
	[HarmonyPatch(typeof(Map))]
	[HarmonyPatch(nameof(Map.Dispose))]
	[HarmonyPriority(Priority.LowerThanNormal)]
	internal static class Verse_Map_Dispose
	{
		static void Prefix(Map __instance)
		{
			if (__instance.Disposed)
				return;
			ListerThingsIndexer.Destroy(__instance.listerThings);
		}
	}
}