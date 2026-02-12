using HarmonyLib;
using RimWorld;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptIdeoCheck"/>
	/// </summary>
	[HarmonyPatch(typeof(Ideo))]
	[HarmonyPatch(nameof(Ideo.RecachePrecepts))]
	internal static class RimWorld_Ideo_RecachePrecepts
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptIdeoCheck.Enabled;
		}

		static void Postfix(Ideo __instance)
		{
			IdeoHelper.UpdateCache(__instance, __instance.currentCacheId);
		}
	}
}