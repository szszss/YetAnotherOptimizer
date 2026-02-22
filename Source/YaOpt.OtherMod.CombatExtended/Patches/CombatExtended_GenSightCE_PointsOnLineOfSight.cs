using CombatExtended;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using YaOpt.OtherMod.CombatExtended.Helpers;

namespace YaOpt.OtherMod.CombatExtended.Patches
{
	[HarmonyPatch(typeof(GenSightCE))]
	[HarmonyPatch(nameof(GenSightCE.PointsOnLineOfSight))]
	internal static class CombatExtended_GenSightCE_PointsOnLineOfSight
	{
		static bool Prepare()
		{
			return SubMod.OptCELineOfSightBurst.Enabled;
		}

		static bool Prefix(Vector3 startPos, Vector3 endPos, out IEnumerable<IntVec3> __result)
		{
			__result = PointsOnLineOfSightHelper.PointsOnLineOfSight(startPos, endPos);
			return false;
		}
	}
}