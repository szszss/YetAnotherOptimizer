using HarmonyLib;
using Verse;

namespace YaOpt.Patches.ThreadSafe
{
	[HarmonyPatch(typeof(MapPawns))]
	[HarmonyPatch("AssertMainThread")]
	internal static class Verse_MapPawns_AssertMainThread
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelJobGiver.Enabled;
		}

		static bool Prefix()
		{
			return !YaOptGlobal.IsParallelRunningInTick;
		}
	}
}