using HarmonyLib;
using UnityEngine;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptMaterialGetColor"/>
	/// </summary>
	[HarmonyPatch(typeof(Material))]
	[HarmonyPatch("color", MethodType.Getter)]
	internal static class UnityEngine_Material_GetColor
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptMaterialGetColor.Enabled;
		}

		static bool Prefix(Material __instance, ref Color __result)
		{
			return !MaterialColorCache.GetColor(__instance, out __result);
		}

		static void Postfix(Material __instance, ref Color __result, bool __runOriginal)
		{
			if (__runOriginal)
			{
				MaterialColorCache.SetColor(__instance, __result);
			}
		}
	}
}