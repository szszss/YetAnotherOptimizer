using HarmonyLib;
using YaOpt.Helpers.ThreadSafe;

namespace YaOpt.OtherMod.VanillaExpandedFramework.Patches.ThreadSafe
{
	[HarmonyPatch("PipeSystem.CachedAdvancedProcessorsManager", "GetFor")]
	internal static class PipeSystem_CachedAdvancedProcessorsManager_GetFor
	{
		private static GreedySpinLock _spinLock = new GreedySpinLock();

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