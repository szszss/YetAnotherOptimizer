using HarmonyLib;
using RimWorld;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptIdeoCheck"/>
	/// </summary>
	[HarmonyPatch(typeof(Ideo))]
	[HarmonyPatch(nameof(Ideo.MemberWillingToDo))]
	internal static class RimWorld_Ideo_MemberWillingToDo
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptIdeoCheck.Enabled;
		}

		[HarmonyPriority(Priority.VeryLow)]
		static bool Prefix(Ideo __instance, ref bool __result, HistoryEvent ev)
		{
			__result = true;
			var cache = IdeoHelper.GetCache(__instance);
			if (cache == null || cache.Version != __instance.currentCacheId)
				return true;
			foreach (var preceptComp in cache.CompsWithMemberWillingToDo)
			{
				if (!preceptComp.MemberWillingToDo(ev))
				{
					__result = false;
					return false;
				}
			}
			return false;
		}
	}
}