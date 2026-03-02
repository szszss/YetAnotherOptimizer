using AlienRace;
using HarmonyLib;
using RimWorld;
using System.Reflection;
using UnityEngine;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.OtherMod.HumanoidAlienRaces.Patches
{
	/// <summary>
	/// Replaces LINQ-based offset lookups with simple loops to avoid GC allocations.
	/// </summary>
	/// <seealso cref="SubMod.OptHARDeLinq"/>
	[HarmonyPatch(typeof(AlienPartGenerator.RotationOffset))]
	[HarmonyPatch(nameof(AlienPartGenerator.RotationOffset.GetOffset))]
	internal static class AlienRace_AlienPartGenerator_RotationOffset_GetOffset
	{
		static bool Prepare(MethodBase original)
		{
			if (original != null)
			{
				MiscHelper.CheckHash(original, Hash128.Parse("21956c96f2af4ce0844908eba0fcacf3"));
			}
			return SubMod.OptHARDeLinq.Enabled;
		}

		static bool Prefix(AlienPartGenerator.RotationOffset __instance, bool __0, BodyTypeDef __1, HeadTypeDef __2,
			ref Vector3 __result)
		{
			var portrait = __0;
			var bodyType = __1;
			var headType = __2;
			var bodyTypes = (portrait ? (__instance.portraitBodyTypes ?? __instance.bodyTypes) : __instance.bodyTypes);
			var bodyOffset = Vector2.zero;
			if (bodyTypes != null)
			{
				foreach (var type in bodyTypes)
				{
					if (type.bodyType == bodyType)
					{
						bodyOffset = type.offset;
						break;
					}
				}
			}
			var headTypes = (portrait ? (__instance.portraitHeadTypes ?? __instance.headTypes) : __instance.headTypes);
			var headOffset = Vector2.zero;
			if (headTypes != null)
			{
				foreach (var type in headTypes)
				{
					if (type.headType == headType)
					{
						headOffset = type.offset;
						break;
					}
				}
			}
			__result.x = __instance.offset.x + bodyOffset.x + headOffset.x;
			__result.y = __instance.layerOffset;
			__result.z = __instance.offset.y + bodyOffset.y + headOffset.y;
			return false;
		}
	}
}