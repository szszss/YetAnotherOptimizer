using HarmonyLib;
using RimWorld;
using System.Threading;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(ListerBuildingWithTagInProximity))]
	[HarmonyPatch(nameof(ListerBuildingWithTagInProximity.GetForCell))]
	internal static class RimWorld_ListerBuildingWithTagInProximity_GetForCell
	{
		private static SpinLock spinLock = new SpinLock();

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		static void Prefix(out bool __state)
		{
			__state = false;
			spinLock.Enter(ref __state);
		}

		static void Finalizer(bool __state)
		{
			if (__state)
				spinLock.Exit();
		}
	}
}