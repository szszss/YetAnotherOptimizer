using HarmonyLib;
using RimWorld;
using System.Threading;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(ListerArtificialBuildingsForMeditation))]
	[HarmonyPatch(nameof(ListerArtificialBuildingsForMeditation.GetForCell))]
	internal static class RimWorld_ListerArtificialBuildingsForMeditation_GetForCell
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