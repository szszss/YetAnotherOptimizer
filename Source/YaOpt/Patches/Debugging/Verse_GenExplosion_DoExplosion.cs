using HarmonyLib;
using Verse;

namespace YaOpt.Patches.Debugging
{
	[HarmonyPatch(typeof(GenExplosion))]
	[HarmonyPatch(nameof(GenExplosion.DoExplosion))]
	internal static class Verse_GenExplosion_DoExplosion
	{
		static bool Prepare()
		{
#if DEBUG
			return true;
#endif
			return false;
		}

		static void Prefix(IntVec3 center, Thing instigator)
		{
			if (center != IntVec3.Invalid)
				return;

			if (instigator == null)
			{
				YaOptMod.Error("Found an explosion with invalid center but the instigator is null");
				return;
			}

			YaOptMod.Error("Found an explosion with invalid center.");
		}
	}
}