using AlienRace.ExtendedGraphics;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.OtherMod.HumanoidAlienRaces.Patches
{
	/// <summary>
	/// Replaces LINQ-based body part lookups with simple loops.
	/// </summary>
	/// <seealso cref="SubMod.OptHARDeLinq"/>
	[HarmonyPatch(typeof(ExtendedGraphicsPawnWrapper))]
	[HarmonyPatch(nameof(ExtendedGraphicsPawnWrapper.GetBodyPart))]
	internal static class AlienRace_ExtendedGraphics_ExtendedGraphicsPawnWrapper_GetBodyPart
	{
		static bool Prepare(MethodBase original)
		{
			if (original != null)
			{
				MiscHelper.CheckHash(original, Hash128.Parse("91226b7956a951c30b195ec893adb49a"));
			}
			return SubMod.OptHARDeLinq.Enabled;
		}

		static bool Prefix(ExtendedGraphicsPawnWrapper __instance, BodyPartDef __0, string __1,
			ref BodyPartRecord __result)
		{
			var notMissingParts = __instance.GetHediffSet().GetNotMissingParts();
			if (notMissingParts == null)
			{
				__result = null;
				return false;
			}
			foreach (var bpr in notMissingParts)
			{
				if (__instance.IsBodyPart(bpr, __0, __1))
				{
					__result = bpr;
					return false;
				}
			}
			__result = null;
			return false;
		}
	}
}