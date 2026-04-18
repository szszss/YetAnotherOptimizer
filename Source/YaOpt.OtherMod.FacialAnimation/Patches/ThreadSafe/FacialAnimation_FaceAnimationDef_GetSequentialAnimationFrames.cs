using FacialAnimation;
using HarmonyLib;
using System.Reflection;
using YaOpt.Helpers.ThreadSafe;

namespace YaOpt.OtherMod.FacialAnimation.Patches.ThreadSafe
{
	/// <seealso cref="SubMod.OptFAParallelUpdate"/>
	[HarmonyPatch(typeof(FaceAnimationDef))]
	[HarmonyPatch(nameof(FaceAnimationDef.GetSequentialAnimationFrames))]
	internal static class FacialAnimation_FaceAnimationDef_GetSequentialAnimationFrames
	{
		static bool Prepare(MethodBase original)
		{
			return SubMod.OptFAParallelUpdate.Enabled;
		}

		static void Prefix(FaceAnimationDef __instance, out bool __state)
		{
			__state = false;
			GreedyMonitor.Enter(__instance, ref __state);
		}

		static void Finalizer(FaceAnimationDef __instance, bool __state)
		{
			if (__state)
				GreedyMonitor.Exit(__instance);
		}
	}
}