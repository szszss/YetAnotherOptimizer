using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FacialAnimation;
using HarmonyLib;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.OtherMod.FacialAnimation.Patches.Compatibility.BondageFurniture
{
	[HarmonyPatch("DDJY_BED.Patch.FacialAnimationControllerComp_Patch", "FilterAnimations")]
	internal static class DDJY_BED_Patch_FacialAnimationControllerComp_Patch_FilterAnimations
	{
		static bool Prepare(MethodBase original)
		{
			return SubMod.OptFAAnimCache.Enabled && YaOptGlobal.HasMod("ddjy.bondagefurniture");
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
