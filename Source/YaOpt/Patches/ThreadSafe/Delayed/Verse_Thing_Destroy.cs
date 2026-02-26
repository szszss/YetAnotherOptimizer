using HarmonyLib;
using System.Collections.Concurrent;
using Verse;

namespace YaOpt.Patches.ThreadSafe.Delayed
{
	[HarmonyPatch(typeof(Thing))]
	[HarmonyPatch(nameof(Thing.Destroy))]
	[HarmonyPriority(Priority.VeryHigh)]
	internal static class Verse_Thing_Destroy
	{
		private static readonly ConcurrentQueue<(Thing, DestroyMode, bool)> _delayedThingDestroy =
			new ConcurrentQueue<(Thing, DestroyMode, bool)>();

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelPostMapTick.Enabled;
		}

		static bool Prefix(Thing __instance, DestroyMode mode)
		{
			if (YaOptGlobal.IsInMainThread)
				return true;
			_delayedThingDestroy.Enqueue((__instance, mode, Thing.allowDestroyNonDestroyable));
			return false;
		}

		public static void Playback()
		{
			if (!_delayedThingDestroy.IsEmpty)
			{
				while (_delayedThingDestroy.TryDequeue(out var result))
				{
					var oldValue = Thing.allowDestroyNonDestroyable;
					Thing.allowDestroyNonDestroyable = result.Item3;
					result.Item1.Destroy(result.Item2);
					Thing.allowDestroyNonDestroyable = oldValue;
				}
			}
		}

		public static void Clear()
		{
			_delayedThingDestroy.Clear();
		}
	}
}