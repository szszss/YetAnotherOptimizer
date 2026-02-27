using HarmonyLib;
using RimWorld;
using System.Collections.Concurrent;
using Unity.Jobs.LowLevel.Unsafe;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.ThreadSafe.Delayed
{
	[HarmonyPatch(typeof(FireUtility))]
	[HarmonyPatch(nameof(FireUtility.TryStartFireIn))]
	[HarmonyPriority(Priority.VeryHigh)]
	internal static class RimWorld_FireUtility_TryStartFireIn
	{
		private static readonly ConcurrentQueue<(IntVec3, Map, float, Thing, SimpleCurve)> _delayedStartFire =
			new ConcurrentQueue<(IntVec3, Map, float, Thing, SimpleCurve)>();

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelPostMapTick.Enabled;
		}

		static bool Prefix(IntVec3 c, Map map, float fireSize, Thing instigator, SimpleCurve flammabilityChanceCurve)
		{
			if (!ParallelMapTickManager.ShouldCheckThread || !JobsUtility.IsExecutingJob)
				return true;
			_delayedStartFire.Enqueue((c, map, fireSize, instigator, flammabilityChanceCurve));
			return false;
		}

		public static void Playback()
		{
			if (!_delayedStartFire.IsEmpty)
			{
				while (_delayedStartFire.TryDequeue(out var result))
				{
					FireUtility.TryStartFireIn(result.Item1, result.Item2, result.Item3, result.Item4, result.Item5);
				}
			}
		}

		public static void Clear()
		{
			_delayedStartFire.Clear();
		}
	}
}