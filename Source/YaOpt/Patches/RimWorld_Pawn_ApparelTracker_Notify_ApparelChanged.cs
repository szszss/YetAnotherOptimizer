using HarmonyLib;
using RimWorld;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptStatCache"/>
	/// </summary>
	[HarmonyPatch(typeof(Pawn_ApparelTracker))]
	[HarmonyPatch(nameof(Pawn_ApparelTracker.Notify_ApparelChanged))]
	internal static class RimWorld_Pawn_ApparelTracker_Notify_ApparelChanged
	{

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptStatCache.Enabled;
		}

		static void Postfix(Pawn_ApparelTracker __instance)
		{
			var pawn = __instance.pawn;
			StatDefOf.ComfyTemperatureMin.Worker.ClearCacheForThing(pawn);
			StatDefOf.ComfyTemperatureMax.Worker.ClearCacheForThing(pawn);
		}
	}
}