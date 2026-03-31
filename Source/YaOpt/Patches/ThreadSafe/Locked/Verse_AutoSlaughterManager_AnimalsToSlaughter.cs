using HarmonyLib;
using System.Threading;
using Verse;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[HarmonyPatch(typeof(AutoSlaughterManager))]
	[HarmonyPatch(nameof(AutoSlaughterManager.AnimalsToSlaughter), MethodType.Getter)]
	internal static class Verse_AutoSlaughterManager_AnimalsToSlaughter
	{
		private static SpinLock _spinLock = new SpinLock();

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelJobGiver.Enabled;
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