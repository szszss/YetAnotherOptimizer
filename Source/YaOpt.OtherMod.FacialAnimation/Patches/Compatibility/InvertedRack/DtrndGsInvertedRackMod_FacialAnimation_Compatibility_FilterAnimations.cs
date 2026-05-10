using FacialAnimation;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.OtherMod.FacialAnimation.Patches.Compatibility.InvertedRack
{
	[HarmonyPatch("DtrndGsInvertedRackMod.FacialAnimation_Compatibility", "FilterAnimations")]
	internal static class DtrndGsInvertedRackMod_FacialAnimation_Compatibility_FilterAnimations
	{
		static bool Prepare(MethodBase original)
		{
			return SubMod.OptFAParallelUpdate.Enabled && YaOptGlobal.HasMod("dtrndg.invertedrack");
		}

		static void Postfix(ref IEnumerable<object> __result, IEnumerable<object> animations)
		{
			if (__result != animations && animations is IList list)
			{
				var tmpList = ThreadLocalTmpList<FacialAnimationControllerComp, object>.Get();
				tmpList.Clear();
				foreach (var o in __result)
				{
					tmpList.Add(o);
				}
				list.Clear();
				foreach (var o in tmpList)
				{
					list.Add(o);
				}
				tmpList.Clear();
				__result = animations;
			}
		}
	}
}
