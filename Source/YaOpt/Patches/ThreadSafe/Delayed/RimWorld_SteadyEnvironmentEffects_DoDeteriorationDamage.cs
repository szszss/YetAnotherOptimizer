using HarmonyLib;
using RimWorld;
using System.Collections.Concurrent;
using Verse;

namespace YaOpt.Patches.ThreadSafe.Delayed
{
	[HarmonyPatch(typeof(SteadyEnvironmentEffects))]
	[HarmonyPatch(nameof(SteadyEnvironmentEffects.DoDeteriorationDamage))]
	[HarmonyPriority(Priority.VeryHigh)]
	internal static class RimWorld_SteadyEnvironmentEffects_DoDeteriorationDamage
	{
		private static readonly ConcurrentQueue<(Thing, IntVec3, Map, bool)> _delayedDeteriorationDamages =
			new ConcurrentQueue<(Thing, IntVec3, Map, bool)>();

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelPostMapTick.Enabled;
		}

		static bool Prefix(Thing t, IntVec3 pos, Map map, bool sendMessage)
		{
			if (YaOptGlobal.IsInMainThread)
				return true;
			_delayedDeteriorationDamages.Enqueue((t, pos, map, sendMessage));
			return false;
		}

		public static void Playback()
		{
			if (!_delayedDeteriorationDamages.IsEmpty)
			{
				while (_delayedDeteriorationDamages.TryDequeue(out var result))
				{
					SteadyEnvironmentEffects.DoDeteriorationDamage(
						result.Item1, result.Item2, result.Item3, result.Item4);
				}
			}
		}

		public static void Clear()
		{
			_delayedDeteriorationDamages.Clear();
		}
	}
}