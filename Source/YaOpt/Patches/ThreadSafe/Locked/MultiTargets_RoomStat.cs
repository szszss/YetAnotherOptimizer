using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Verse;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch]
	internal static class MultiTargets_RoomStat
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(Room),
				nameof(Room.GetRoomRoleIfBuildingPlaced));
			yield return AccessTools.Method(typeof(Room),
				nameof(Room.GetStat));
			yield return AccessTools.PropertyGetter(typeof(Room),
				nameof(Room.Role));
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelThoughtUpdate.Enabled || YaOptGlobal.NeedThreadSafe;
		}

		static void Prefix(Room __instance, out bool __state)
		{
			__state = false;
			Monitor.Enter(__instance, ref __state);
		}

		static void Finalizer(Room __instance, bool __state)
		{
			if (__state)
				Monitor.Exit(__instance);
		}
	}
}