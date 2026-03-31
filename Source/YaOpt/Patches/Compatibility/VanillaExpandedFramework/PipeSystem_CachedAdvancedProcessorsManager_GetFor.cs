using HarmonyLib;
using System.Threading;

namespace YaOpt.Patches.Compatibility.VanillaExpandedFramework
{
	[HarmonyPatch("PipeSystem.CachedAdvancedProcessorsManager", "GetFor")]
	internal static class PipeSystem_CachedAdvancedProcessorsManager_GetFor
	{
		private static SpinLock _spinLock = new SpinLock();

		static bool Prepare()
		{
			return (YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelJobGiver.Enabled) &&
				   YaOptGlobal.HasMod("OskarPotocki.VanillaFactionsExpanded.Core");
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