using HarmonyLib;
using RimWorld;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	[HarmonyPatch(typeof(Toils_Bed))]
	[HarmonyPatch(nameof(Toils_Bed.BedNoLongerUsable))]
	internal static class RimWorld_Toils_Bed_BedNoLongerUsable
	{
		private const int INTERVAL = 15;

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptIdleThrottle.Enabled;
		}

		static bool Prefix(Pawn actor, Thing bedThing, ref bool __result)
		{
			if (actor.IsHashIntervalTick(INTERVAL))
			{
				return true;
			}
			__result = !IdleHelper.CanUseBedNowLight(actor, bedThing);
			return false;
		}
	}
}