using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Verse;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch]
	internal static class MultiTargets_BodyDef
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(BodyDef), nameof(BodyDef.ClearCachedData));
			yield return AccessTools.Method(typeof(BodyDef), nameof(BodyDef.GetPartsWithDef));
			yield return AccessTools.Method(typeof(BodyDef), nameof(BodyDef.GetPartsWithTag));
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		static void Prefix(BodyDef __instance, out bool __state)
		{
			__state = false;
			Monitor.Enter(__instance, ref __state);
		}

		static void Finalizer(BodyDef __instance, bool __state)
		{
			if (__state)
				Monitor.Exit(__instance);
		}
	}
}