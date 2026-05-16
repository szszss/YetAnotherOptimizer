using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using YaOpt.Helpers.ThreadSafe;

namespace YaOpt.Patches.Compatibility.VehicleMapFramework
{
	[HarmonyPatch]
	internal static class VehicleMapFramework_VehicleMapUtility_BaseMapAndVehicleMaps
	{
		private static GreedySpinLock _spinLock = new GreedySpinLock();

		static MethodBase TargetMethod()
		{
			VehicleMapFrameworkCompatibility.Init();
			return AccessTools.Method(
				AccessTools.TypeByName("VehicleMapFramework.VehicleMapUtility"),
				"BaseMapAndVehicleMaps", new[] { typeof(Map), typeof(bool) });
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe &&
				   YaOptGlobal.HasMod("oels.vehiclemapframework");
		}

		static void Prefix(out bool __state)
		{
			__state = false;
			_spinLock.Enter(ref __state);
		}

		static void Postfix(Map map, bool includeItself, ref HashSet<Map> __result)
		{
			__result = VehicleMapFrameworkCompatibility.GetThreadSafeBaseMapAndVehicleMaps(
				__result, map, includeItself);
		}

		static void Finalizer(bool __state)
		{
			if (__state)
				_spinLock.Exit();
		}
	}
}