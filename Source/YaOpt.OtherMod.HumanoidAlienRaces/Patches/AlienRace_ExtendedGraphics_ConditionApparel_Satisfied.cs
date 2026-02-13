using AlienRace;
using AlienRace.ExtendedGraphics;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.OtherMod.HumanoidAlienRaces.Patches
{
	[HarmonyPatch(typeof(ConditionApparel))]
	[HarmonyPatch(nameof(ConditionApparel.Satisfied))]
	internal static class AlienRace_ExtendedGraphics_ConditionApparel_Satisfied
	{
		static bool Prepare(MethodBase original)
		{
			if (original != null)
			{
				MiscHelper.CheckHash(original, Hash128.Parse("6a497eef2269eb7d0330d4452833fced"));
			}
			return SubMod.OptHARDeLinq.Enabled;
		}

		static bool Prefix(ConditionApparel __instance, ExtendedGraphicsPawnWrapper __0, ref ResolveData __1,
			ref bool __result)
		{
			__result = false;
			if (!__1.head && !AlienRenderTreePatches.IsPortrait(__0.WrappedPawn) && !__0.VisibleInBed(true))
			{
				__result = true;
			}
			else if (__1.head && AlienRenderTreePatches.IsPortrait(__0.WrappedPawn) && Prefs.HatsOnlyOnMap)
			{
				__result = true;
			}
			else if (__instance.hiddenUnderApparelTag.NullOrEmpty() && __instance.hiddenUnderApparelFor.NullOrEmpty())
			{
				__result = true;
			}
			else
			{
				__result = true;
				foreach (var ap in __0.GetWornApparelProps())
				{
					foreach (var bpgd in ap.bodyPartGroups)
					{
						if (__instance.hiddenUnderApparelFor.Contains(bpgd))
						{
							__result = false;
							return false;
						}
					}

					foreach (var s in ap.tags)
					{
						if (__instance.hiddenUnderApparelTag.Contains(s))
						{
							__result = false;
							return false;
						}
					}
				}
			}
			return false;
		}
	}
}