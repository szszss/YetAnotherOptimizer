using HarmonyLib;
using UnityEngine;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// Caches material color on setter to enable fast getter lookups.
	/// </summary>
	/// <seealso cref="YaOptSettings.OptMaterialGetColor"/>
	[HarmonyPatch(typeof(Material))]
	[HarmonyPatch("color", MethodType.Setter)]
	internal static class UnityEngine_Material_SetColor
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptMaterialGetColor.Enabled;
		}

		static bool Prefix(Material __instance, Color value)
		{
			return MaterialColorCache.SetColor(__instance, value);
		}
	}
}