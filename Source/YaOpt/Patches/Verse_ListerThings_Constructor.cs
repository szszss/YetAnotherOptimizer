using System;
using HarmonyLib;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// </summary>
	/// <seealso cref="YaOptSettings.OptFastListerRemove"/>
	[HarmonyPatch(typeof(ListerThings))]
	[HarmonyPatch(MethodType.Constructor)]
	[HarmonyPatch(new [] { typeof(ListerThingsUse), typeof(ThingListChangedCallbacks) })]
	internal static class Verse_ListerThings_Constructor
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptFastListerRemove.Enabled;
		}

		static void Postfix(ListerThings __instance)
		{
			ListerThingsIndexer.Create(__instance);
		}
	}
}
