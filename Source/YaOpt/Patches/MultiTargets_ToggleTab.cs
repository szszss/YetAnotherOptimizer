using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptToggleTabCheck"/>
	/// </summary>
	[HarmonyPatch]
	internal static class MultiTargets_ToggleTab
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			foreach (var type in ToggleTabCache.ToggleTabTypes)
			{
				yield return AccessTools.PropertyGetter(type, nameof(MainButtonWorker.Disabled));
			}
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptToggleTabCheck.Enabled;
		}

		static bool Prefix(object __instance, ref bool __result)
		{
			return !ToggleTabCache.TryGetResult(__instance.GetType(), out __result);
		}

		static void Postfix(object __instance, ref bool __result, bool __runOriginal)
		{
			if (__runOriginal)
			{
				ToggleTabCache.UpdateCache(__instance.GetType(), __result);
			}
		}
	}
}