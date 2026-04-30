using HarmonyLib;
using System.Reflection;
using System.Threading;
using Verse;

namespace YaOpt.Patches.Compatibility.VehicleFramework
{
	[HarmonyPatch]
	internal static class SmashTools_MapComponentCache_GetComponent
	{
		private static readonly object _lockObj = new object();

		static MethodBase TargetMethod()
		{
			return AccessTools.Method(
				AccessTools.TypeByName("SmashTools.MapComponentCache").MakeGenericType(typeof(MapComponent)),
				"GetComponent");
		}

		static bool Prepare()
		{
			return (YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelWorkGiver.Enabled) &&
				   YaOptGlobal.HasType("SmashTools.MapComponentCache");
		}

		static void Prefix(out bool __state)
		{
			__state = false;
			Monitor.Enter(_lockObj, ref __state);
		}

		static void Finalizer(bool __state, MapComponent __result)
		{
			if (__result != null)
				YaOptMod.Warning($"Return {__result.GetType()} - {__result}");
			if (__state)
				Monitor.Exit(_lockObj);
		}
	}
}