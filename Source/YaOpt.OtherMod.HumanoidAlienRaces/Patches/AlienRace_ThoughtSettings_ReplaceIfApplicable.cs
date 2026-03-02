using AlienRace;
using HarmonyLib;
using RimWorld;
using System.Reflection;
using UnityEngine;
using YaOpt.Helpers;

namespace YaOpt.OtherMod.HumanoidAlienRaces.Patches
{
	/// <summary>
	/// Replaces LINQ-based thought replacement lookups with simple loops.
	/// </summary>
	/// <seealso cref="SubMod.OptHARDeLinq"/>
	[HarmonyPatch(typeof(ThoughtSettings))]
	[HarmonyPatch(nameof(ThoughtSettings.ReplaceIfApplicable))]
	internal static class AlienRace_ThoughtSettings_ReplaceIfApplicable
	{
		static bool Prepare(MethodBase original)
		{
			if (original != null)
			{
				MiscHelper.CheckHash(original, Hash128.Parse("88827cef00e729ebfc95c3096db32561"));
			}
			return SubMod.OptHARDeLinq.Enabled;
		}

		static bool Prefix(ThoughtSettings __instance, ThoughtDef def, ref ThoughtDef __result)
		{
			if (__instance.replacerList != null)
			{
				foreach (var tr in __instance.replacerList)
				{
					if (tr.replacer == def)
					{
						__result = def;
						return false;
					}
				}
				foreach (var tr in __instance.replacerList)
				{
					if (tr.original == def)
					{
						__result = tr.replacer ?? def;
						return false;
					}
				}
			}
			__result = def;
			return false;
		}
	}
}