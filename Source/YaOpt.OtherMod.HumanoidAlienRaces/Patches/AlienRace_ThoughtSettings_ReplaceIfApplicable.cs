using AlienRace;
using HarmonyLib;
using RimWorld;
using System.Reflection;
using UnityEngine;
using YaOpt.Helpers;

namespace YaOpt.OtherMod.HumanoidAlienRaces.Patches
{
	/// <summary>
	/// This is actually a TPS optimization.
	/// </summary>
	[HarmonyPatch(typeof(ThoughtSettings))]
	[HarmonyPatch(nameof(ThoughtSettings.ReplaceIfApplicable))]
	internal static class AlienRace_ThoughtSettings_ReplaceIfApplicable
	{
		static bool Prepare(MethodBase original)
		{
			if (original != null)
			{
				MiscHelper.CheckHash(original, Hash128.Parse("31a4b85d5b8c2aabea2703fb8dc0acf6"));
			}
			return SubMod.OptHARDeLinq.Enabled;
		}

		static bool Prefix(ThoughtSettings __instance, ThoughtDef __0, ref ThoughtDef __result)
		{
			if (__instance.replacerList != null)
			{
				foreach (var tr in __instance.replacerList)
				{
					if (tr.replacer == __0)
					{
						__result = __0;
						return false;
					}
				}
				foreach (var tr in __instance.replacerList)
				{
					if (tr.original == __0)
					{
						__result = tr.replacer ?? __0;
						return false;
					}
				}
			}
			__result = __0;
			return false;
		}
	}
}