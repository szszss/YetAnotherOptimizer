using HarmonyLib;
using RimWorld;
using Verse;

namespace YaOpt.Patches.Debugging
{
	[HarmonyPatch(typeof(CompExplosive))]
	[HarmonyPatch("Detonate")]
	internal static class RimWorld_CompExplosive
	{
		static bool Prepare()
		{
#if DEBUG
			return true;
#endif
			return false;
		}

		static void Prefix(CompExplosive __instance)
		{
			var parent = __instance.parent;
			if (parent == null)
			{
				YaOptMod.Error("Found a CompExplosive which parent is null");
				return;
			}
			var pos = parent.Position;
			var heldPos = parent.PositionHeld;
			if (pos != IntVec3.Invalid)
				return;
			YaOptMod.Error($"{parent.ToStringSafe()} - Pos:{pos}, HeldPos:{heldPos}");
			if (parent.ParentHolder != null)
			{
				YaOptMod.Error($"{parent.ParentHolder.ToStringSafe()}");
				YaOptMod.Error($"{parent.ParentHolder.GetDirectlyHeldThings().ToStringSafe()}");
			}
		}
	}
}