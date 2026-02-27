using HarmonyLib;
using System.Collections.Concurrent;
using Unity.Jobs.LowLevel.Unsafe;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.ThreadSafe.Delayed
{
	[HarmonyPatch(typeof(FleckManager))]
	[HarmonyPatch(nameof(FleckManager.CreateFleck))]
	[HarmonyPriority(Priority.VeryHigh)]
	internal static class Verse_FleckManager_CreateFleck
	{
		private static readonly ConcurrentQueue<(FleckManager, FleckCreationData)> _delayedFleckCreation =
			new ConcurrentQueue<(FleckManager, FleckCreationData)>();

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelPostMapTick.Enabled;
		}

		static bool Prefix(FleckManager __instance, in FleckCreationData __0)
		{
			if (!ParallelMapTickManager.ShouldCheckThread || !JobsUtility.IsExecutingJob)
				return true;
			_delayedFleckCreation.Enqueue((__instance, __0));
			return false;
		}

		public static void Playback()
		{
			if (!_delayedFleckCreation.IsEmpty)
			{
				while (_delayedFleckCreation.TryDequeue(out var result))
				{
					result.Item1.CreateFleck(result.Item2);
				}
			}
		}

		public static void Clear()
		{
			_delayedFleckCreation.Clear();
		}
	}
}