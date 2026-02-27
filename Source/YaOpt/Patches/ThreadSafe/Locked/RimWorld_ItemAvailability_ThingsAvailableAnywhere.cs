using HarmonyLib;
using RimWorld;
using System.Threading;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(ItemAvailability))]
	[HarmonyPatch(nameof(ItemAvailability.ThingsAvailableAnywhere))]
	internal static class RimWorld_ItemAvailability_ThingsAvailableAnywhere
	{
		private static SpinLock _spinLock = new SpinLock();

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		static void Prefix(out bool __state)
		{
			__state = false;
			_spinLock.Enter(ref __state);
		}

		static void Finalizer(bool __state)
		{
			if (__state)
				_spinLock.Exit();
		}
	}
}