using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Threading;
using Verse;
using YaOpt.Helpers.Trampolines;
using YaOpt.Settings;

namespace YaOpt
{
	public static class YaOptGlobal
	{
		public static bool IsDebug => Settings?.DebugLogging == true;

		public static bool IsMultiplayer { get; internal set; }

		public static bool IsNativeAvailable { get; internal set; }

		public static bool IsBurstAvailable { get; internal set; }

		public static bool IsLibraryLoaded { get; internal set; }

		public static bool IsTrampolineAvailable => TrampolineFactory.IsAvailable;

		public static bool IsParallelMaterialUpdateEnabled { get; internal set; }

		public static bool IsInMainThread => _isInMainThread;

		/// <summary>
		/// <seealso cref="YaOpt.Patches.ThreadSafe"/>
		/// </summary>
		public static bool NeedThreadSafe => YaOptMod.Instance.Settings.OptParallelPawnTick.Enabled ||
											 YaOptMod.Instance.Settings.OptParallelJobGiver.Enabled;

		public static YaOptMod Mod => YaOptMod.Instance;

		public static YaOptSettings Settings => YaOptMod.Instance.Settings;

		public static Harmony Harmony => YaOptMod.Instance.Harmony;

		public static List<YaOptSubMod> SubMods => Mod.SubMods;

		private static readonly Dictionary<string, bool> _modLookup = new Dictionary<string, bool>();

		private static readonly Dictionary<string, bool> _typeLookup = new Dictionary<string, bool>();

		private static readonly Dictionary<OptimizationOption, bool> _optionSnapshot =
			new Dictionary<OptimizationOption, bool>();

		[ThreadStatic]
		private static bool _isInMainThread;

		public static bool HasType(string typeFullName)
		{
			if (!_typeLookup.TryGetValue(typeFullName, out var result))
			{
				result = AccessTools.TypeByName(typeFullName) != null;
				_typeLookup[typeFullName] = result;
			}
			return result;
		}

		public static bool HasMod(string modId)
		{
			if (!_modLookup.TryGetValue(modId, out var result))
			{
				result = ModLister.GetActiveModWithIdentifier(modId) != null;
				_modLookup[modId] = result;
			}
			return result;
		}

		public static void CreateOptionSnapshot()
		{
			foreach (var option in Settings.AllOptimizations)
			{
				if ((option.Flags & OptimizationFlags.NoSnapshot) == 0)
				{
					_optionSnapshot[option] = option._enabled;
				}
			}
			IsParallelMaterialUpdateEnabled = Settings.OptParallelMaterialUpdate.Enabled;
		}

		public static bool AnyOptionChanged()
		{
			foreach (var pair in _optionSnapshot)
			{
				if (pair.Key._enabled != pair.Value)
					return true;
			}
			return false;
		}

		internal static void MarkAsMainThread()
		{
			if (!UnityData.IsInMainThread)
			{
				YaOptMod.Error($"Thread {Thread.CurrentThread.Name} is not the Unity main thread, " +
							   $"but MarkAsMainThread was called within it.");
			}
			_isInMainThread = true;
		}
	}
}