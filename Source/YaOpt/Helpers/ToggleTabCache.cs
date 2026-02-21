using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using YaOpt.Defines;

namespace YaOpt.Helpers
{
	[StaticConstructorOnStartup]
	internal static class ToggleTabCache
	{
		private const int CHECK_INTERVAL = 500;

		private static int currentRealTime;

		private static int currentGameTick;

		private static int lastCheckMapId = int.MinValue;

		private static bool wasPaused;

		private static bool forceUpdateBecausePauseOrUnpause;

		private static readonly Dictionary<Type, CacheEntry> tabCache = new Dictionary<Type, CacheEntry>();

		public static List<Type> ToggleTabTypes { get; } = new List<Type>();

		private class CacheEntry
		{
			public int LastCheckRealTime;

			public int LastCheckGameTick;

			/// <summary>
			/// It's used to randomly distribute the check of multiple ToggleTabs across different frames.
			/// </summary>
			public int ExtraCheckInterval;

			public int ExtraCheckIntervalDefault;

			public bool LastCheckResult;
		}

		static ToggleTabCache()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
			UpdateCallbackHelper.RegisterPostRenderCallback(BeforeGui);
			TypeSearcher.RegisterSearchingType(typeof(MainButtonWorker_ToggleTab),
				MainButtonWorker_ToggleTab_Checker);
		}

		private static void ClearCache()
		{
			tabCache.Clear();
			currentGameTick = 0;
			lastCheckMapId = int.MinValue;
			wasPaused = false;
		}

		private static void MainButtonWorker_ToggleTab_Checker(Type type)
		{
			if (type == typeof(MainButtonWorker_ToggleTab))
				return;
			if (type == typeof(MainButtonWorker_ToggleMechTab))
				return;
			if (CompatibilityDefines.CachedIgnoredToggleTabCaching.Contains(type))
				return;

			if (type.GetProperty(nameof(MainButtonWorker.Disabled),
					BindingFlags.Instance | BindingFlags.Public)?.GetMethod?.IsOverriden() == true)
			{
				ToggleTabTypes.Add(type);
			}
		}

		private static void BeforeGui(int tick)
		{
			currentRealTime = Environment.TickCount;
			currentGameTick = GenTicks.TicksGame;
			var paused = Find.TickManager.Paused;
			int currentMapId = Find.CurrentMap?.uniqueID ?? int.MinValue;
			if (wasPaused != paused)
			{
				wasPaused = paused;
				forceUpdateBecausePauseOrUnpause = true;
			}
			else if (lastCheckMapId != currentMapId)
			{
				lastCheckMapId = currentMapId;
				forceUpdateBecausePauseOrUnpause = true;
			}
			else
			{
				forceUpdateBecausePauseOrUnpause = false;
			}
		}

		public static bool TryGetResult(Type tabClass, out bool result)
		{
			result = false;
			if (forceUpdateBecausePauseOrUnpause)
				return false;
			if (!tabCache.TryGetValue(tabClass, out var entry))
				return false;
			if (currentRealTime - entry.LastCheckRealTime + entry.ExtraCheckInterval >= CHECK_INTERVAL &&
				currentGameTick != entry.LastCheckGameTick)
				return false;
			result = entry.LastCheckResult;
			return true;
		}

		public static void UpdateCache(Type tabClass, bool disabled)
		{
			if (!tabCache.TryGetValue(tabClass, out var entry))
			{
				entry = new CacheEntry();
				tabCache[tabClass] = entry;
				entry.ExtraCheckIntervalDefault = Math.Abs(tabClass.GetHashCode()) % CHECK_INTERVAL;
			}
			entry.LastCheckRealTime = currentRealTime;
			entry.LastCheckGameTick = currentGameTick;
			entry.LastCheckResult = disabled;
			entry.ExtraCheckInterval = forceUpdateBecausePauseOrUnpause ? entry.ExtraCheckIntervalDefault : 0;
		}
	}
}