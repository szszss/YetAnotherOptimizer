using HarmonyLib;
using System;
using System.Collections.Generic;
using Verse;

namespace YaOpt.Patches
{
	/// <summary>
	/// </summary>
	/// <seealso cref="YaOptSettings.OptFastListerRemove"/>
	[HarmonyPatch(typeof(ThingDef))]
	[HarmonyPatch(nameof(ThingDef.HasAssignableCompFrom))]
	internal static class Verse_ThingDef_HasAssignableCompFrom
	{
		private const int TOO_FEW_COMPS = 3;

		private static readonly AccessTools.FieldRef<List<CompProperties>, int> _listVersionFieldRef =
			AccessTools.FieldRefAccess<int>(typeof(List<CompProperties>), "_version");

		private static readonly Dictionary<ThingDef, Cache> _caches = new Dictionary<ThingDef, Cache>();

		private class Cache
		{
			public int Version;

			public readonly Dictionary<Type, bool> Results = new Dictionary<Type, bool>();

			public bool GetResult(Type type, int version, out bool result)
			{
				result = false;
				if (version != Version)
				{
					Results.Clear();
					return false;
				}
				return Results.TryGetValue(type, out result);
			}
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptFastListerRemove.Enabled;
		}

		static bool Prefix(ThingDef __instance, Type compType, ref bool __result)
		{
			var comps = __instance.comps;
			if (comps.Count == 0)
			{
				__result = false;
				return false;
			}
			if (!YaOptGlobal.IsInMainThread || comps.Count < TOO_FEW_COMPS)
				return true;
			if (!_caches.TryGetValue(__instance, out var cache))
				return true;
			var version = _listVersionFieldRef(comps);
			return !cache.GetResult(compType, version, out __result);
		}

		static void Postfix(ThingDef __instance, Type compType, bool __runOriginal, ref bool __result)
		{
			var comps = __instance.comps;
			if (!__runOriginal || !YaOptGlobal.IsInMainThread || comps.Count < TOO_FEW_COMPS)
				return;
			var version = _listVersionFieldRef(comps);
			if (!_caches.TryGetValue(__instance, out var cache))
			{
				cache = new Cache();
				_caches[__instance] = cache;
			}
			cache.Version = version;
			cache.Results[compType] = __result;
		}
	}
}