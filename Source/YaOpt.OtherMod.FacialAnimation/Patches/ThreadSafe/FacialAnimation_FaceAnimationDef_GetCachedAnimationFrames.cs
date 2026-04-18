using FacialAnimation;
using HarmonyLib;
using System.Reflection;
using YaOpt.Helpers.ThreadSafe;

namespace YaOpt.OtherMod.FacialAnimation.Patches.ThreadSafe
{
	/// <seealso cref="SubMod.OptFAParallelUpdate"/>
	[HarmonyPatch(typeof(FaceAnimationDef))]
	[HarmonyPatch("GetCachedAnimationFrames")]
	internal static class FacialAnimation_FaceAnimationDef_GetCachedAnimationFrames
	{
		// Use a method-scoped lock to prevent deadlock.
		private static GreedySpinLock _spinLock = new GreedySpinLock() { SupportRecursion = true };

		static bool Prepare(MethodBase original)
		{
			return SubMod.OptFAParallelUpdate.Enabled;
		}

		static void Prefix(FaceAnimationDef __instance, out bool __state)
		{
			__state = false;
			_spinLock.Enter(ref __state);
		}

		static void Finalizer(FaceAnimationDef __instance, bool __state)
		{
			if (__state)
				_spinLock.Exit();
		}
	}
}