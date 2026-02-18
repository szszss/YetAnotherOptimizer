using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace YaOpt.Patches.Compatibility.VanillaPsycastsExpanded
{
	[HarmonyPatch]
	internal static class MultiTargets_GenRadialCache
	{
		private static readonly object _objLock = new object();

		static IEnumerable<MethodBase> TargetMethods()
		{
			var type = AccessTools.TypeByName("VanillaPsycastsExpanded.GenRadialCached");
			yield return AccessTools.FirstMethod(type,
				method => method.Name == "RadialDistinctThingsAround" && method.GetParameters().Length == 2);
			yield return AccessTools.Method(type, "MeditationFociAround");
			yield return AccessTools.Method(type, "WealthAround");
		}

		static bool Prepare(MethodBase original)
		{
			if (original != null)
				return true;

			var shouldRun = (YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelJobGiver.Enabled) &&
							YaOptGlobal.HasMod("VanillaExpanded.VPsycastsE");

			return shouldRun;
		}

		static void Prefix(out bool __state)
		{
			__state = false;
			Monitor.Enter(_objLock, ref __state);
		}

		static void Finalizer(bool __state)
		{
			if (__state)
				Monitor.Exit(_objLock);
		}
	}
}