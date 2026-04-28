using HarmonyLib;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// </summary>
	/// <seealso cref="YaOptSettings.OptFastListerRemove"/>
	[HarmonyPatch(typeof(ListerThings))]
	[HarmonyPatch(nameof(ListerThings.Clear))]
	internal static class Verse_ListerThings_Clear
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptFastListerRemove.Enabled;
		}

		static void Prefix(ListerThings __instance)
		{
			ListerThingsIndexer.TryClear(__instance);
		}
	}
}
