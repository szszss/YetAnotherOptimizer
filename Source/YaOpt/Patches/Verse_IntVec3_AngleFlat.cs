using HarmonyLib;
using System;
using Verse;

namespace YaOpt.Patches
{
	/// <seealso cref="YaOptSettings.OptAttackTargetFinder"/>
	[HarmonyPatch(typeof(IntVec3))]
	[HarmonyPatch(nameof(IntVec3.AngleFlat), MethodType.Getter)]
	internal static class Verse_IntVec3_AngleFlat
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptAttackTargetFinder.Enabled && !YaOptGlobal.IsMultiplayer;
		}

		static bool Prefix(IntVec3 __instance, ref float __result)
		{
			if (__instance.x == 0 && __instance.z == 0)
			{
				__result = 0;
				return false;
			}
			// Fix the difference in coordinate systems
			int x = __instance.z;
			int y = __instance.x;
			float absX = Math.Abs(x);
			float absY = Math.Abs(y);
			float a = Math.Min(absX, absY) / Math.Max(absX, absY);
			float s = a * a;

			// Fast Atan2
			float r = ((-0.0464964749f * s + 0.15931422f) * s - 0.327622764f) * s * a + a;
			if (absY > absX) r = 1.57079637f - r;
			if (x < 0) r = 3.14159274f - r;
			if (y < 0) r = -r;

			float deg = r * 57.29578f;
			__result = deg < 0 ? deg + 360f : deg;
			return false;
		}
	}
}