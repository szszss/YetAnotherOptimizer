using HarmonyLib;
using System.Collections.Generic;
using Verse;
using YaOpt.Helpers;

namespace YaOpt
{
	public static class YaOptGlobal
	{
		public static bool IsDebug => Settings?.DebugLogging == true;

		public static bool IsMultiplay { get; internal set; }

		public static bool IsWindows { get; internal set; }

		public static bool IsBurstAvailable { get; internal set; }

		public static bool IsLibraryLoaded { get; internal set; }

		public static bool IsTrampolineAvailable => AsmHelper.IsAvailable;

		public static bool IsParallelMaterialUpdateEnabled { get; internal set; }

		/// <summary>
		/// <seealso cref="YaOpt.Patches.ThreadSafe"/>
		/// </summary>
		public static bool NeedThreadSafe => YaOptMod.Instance.Settings.OptParallelPawnTick.Enabled ||
		                                     YaOptMod.Instance.Settings.OptParallelJobGiver.Enabled;

		public static YaOptMod Mod => YaOptMod.Instance;

		public static YaOptSettings Settings => YaOptMod.Instance.Settings;

		public static Harmony Harmony => YaOptMod.Instance.Harmony;

		public static List<YaOptSubMod> SubMods => Mod.SubMods;

		private static readonly Dictionary<string, bool> modLookup = new Dictionary<string, bool>();

		private static readonly Dictionary<string, bool> typeLookup = new Dictionary<string, bool>();

		private static readonly Dictionary<YaOptSettings.OptimizationOption, bool> optionSnapshot = 
			new Dictionary<YaOptSettings.OptimizationOption, bool>();

		public static bool HasType(string typeFullName)
		{
			if (!typeLookup.TryGetValue(typeFullName, out var result))
			{
				result = AccessTools.TypeByName(typeFullName) != null;
				typeLookup[typeFullName] = result;
			}
			return result;
		}

		public static bool HasMod(string modId)
		{
			if (!modLookup.TryGetValue(modId, out var result))
			{
				result = ModLister.GetActiveModWithIdentifier(modId) != null;
				modLookup[modId] = result;
			}
			return result;
		}

		public static void CreateOptionSnapshot()
		{
			foreach (var option in Settings.AllOptimizations)
			{
				if ((option.Flags & YaOptSettings.OptimizationFlag.NoSnapshot) == 0)
				{
					optionSnapshot[option] = option._enabled;
				}
			}
			IsParallelMaterialUpdateEnabled = Settings.OptParallelMaterialUpdate.Enabled;
		}

		public static bool AnyOptionChanged()
		{
			foreach (var pair in optionSnapshot)
			{
				if (pair.Key._enabled != pair.Value)
					return true;
			}
			return false;
		}
	}
}