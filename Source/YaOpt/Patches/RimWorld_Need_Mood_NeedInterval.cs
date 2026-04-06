using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// 
	/// </summary>
	/// <seealso cref="YaOptSettings.OptParallelPawnTick"/>
	[HarmonyPatch(typeof(SituationalThoughtHandler))]
	[HarmonyPatch("UpdateAllMoodThoughts")]
	[HarmonyPriority(Priority.VeryLow)]
	internal static class RimWorld_SituationalThoughtHandler_UpdateAllMoodThoughts
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelPawnTick.Enabled &&
				   YaOptGlobal.Settings.ParallelPawnMoodUpdate;
		}

		static bool Prefix(SituationalThoughtHandler __instance, bool __runOriginal,
			List<Thought_Situational> ___cachedThoughts, ref bool ___thoughtsDirty)
		{
			if (!__runOriginal)
				return false;
			___thoughtsDirty = false;
			ParallelThoughtUpdater.Update(__instance, ___cachedThoughts);
			return false;
		}
	}
}