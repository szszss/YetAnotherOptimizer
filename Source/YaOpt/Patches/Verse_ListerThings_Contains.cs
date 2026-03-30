using HarmonyLib;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace YaOpt.Patches
{
	/// <summary>
	/// </summary>
	/// <seealso cref="YaOptSettings.OptFastListerRemove"/>
	[HarmonyPatch(typeof(ListerThings))]
	[HarmonyPatch(nameof(ListerThings.Contains))]
	internal static class Verse_ListerThings_Contains
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptFastListerRemove.Enabled;
		}

		static bool Prefix(Thing t, ListerThingsUse ___use, Dictionary<ThingDef, List<Thing>> ___listsByDef,
			ref bool __result)
		{
			__result = Contains(t, ___use, ___listsByDef);
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static bool Contains(Thing thing, ListerThingsUse use, Dictionary<ThingDef, List<Thing>> listsByDef)
		{
			var def = thing.def;
			if (!ListerThings.EverListable(def, use))
				return false;
			if (!listsByDef.TryGetValue(def, out var list))
				return false;
			return list.LastIndexOf(thing) >= 0;
		}
	}
}