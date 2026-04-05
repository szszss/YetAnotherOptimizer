using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// Skip the regular mood update if it has already been completed.
	/// </summary>
	/// <seealso cref="YaOptSettings.OptParallelPawnTick"/>
	[HarmonyPatch(typeof(Need_Mood))]
	[HarmonyPatch(nameof(Need_Mood.NeedInterval))]
	[HarmonyPriority(Priority.VeryHigh)]
	internal static class RimWorld_Need_Mood_NeedInterval
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelPawnTick.Enabled &&
			       YaOptGlobal.Settings.ParallelPawnMoodUpdate;
		}

		static bool Prefix(Pawn ___pawn)
		{
			var id = ___pawn.thingIDNumber;
			lock (ParallelPawnTickManager.PawnsWhoShouldSkipMoodUpdate)
			{
				return !ParallelPawnTickManager.PawnsWhoShouldSkipMoodUpdate.Contains(id);
			}
		}
	}
}