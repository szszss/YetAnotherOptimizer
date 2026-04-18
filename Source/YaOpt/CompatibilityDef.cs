using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;
using YaOpt.Defines;

namespace YaOpt
{
	public class CompatibilityDef : Def
	{
		public List<string> BannedOptimizations
		{
			get => bannedOptimizations; set => bannedOptimizations = value;
		}

		public List<string> IgnoredToggleTabCaching
		{
			get => ignoredToggleTabCaching; set => ignoredToggleTabCaching = value;
		}

		public List<JobDef> IgnoredJobFailurePredicting
		{
			get => ignoredJobFailurePredicting; set => ignoredJobFailurePredicting = value;
		}

		public List<WorkGiverCompatibility> WorkGiverCompatibilities
		{
			get => workGiverCompatibilities; set => workGiverCompatibilities = value;
		}

		public List<ThreadLocalPatch> ThreadLocalPatches
		{
			get => threadLocalPatches; set => threadLocalPatches = value;
		}

		public List<LockPatch> LockPatches
		{
			get => lockPatches; set => lockPatches = value;
		}

		[NoTranslate]
		private List<string> bannedOptimizations;

		[NoTranslate]
		private List<string> ignoredToggleTabCaching;

		[NoTranslate]
		private List<JobDef> ignoredJobFailurePredicting;

		private List<WorkGiverCompatibility> workGiverCompatibilities;

		private List<ThreadLocalPatch> threadLocalPatches;

		private List<LockPatch> lockPatches;

		public void Cache()
		{
			var mod = modContentPack.Name;

			if (BannedOptimizations != null)
			{
				foreach (var bannedOptimization in BannedOptimizations)
				{
					if (YaOptGlobal.Settings.AllOptimizations.All(
							opt => opt.SettingId != bannedOptimization))
					{
						YaOptMod.Error($"CompatibilityDef {defName} from {mod} " +
									   $"couldn't find optimization {bannedOptimization}");
						continue;
					}
					YaOptMod.Debug($"Optimization {bannedOptimization} is banned by {mod}.");
					CompatibilityDefines.CachedBannedOptimizations.Add(bannedOptimization);
					CompatibilityDefines.CachedBannedBy[bannedOptimization] = mod;
				}
			}

			if (IgnoredToggleTabCaching != null)
			{
				foreach (var toggleTabTypeName in IgnoredToggleTabCaching)
				{
					var type = AccessTools.TypeByName(toggleTabTypeName);
					if (type == null)
					{
						YaOptMod.Error($"CompatibilityDef {defName} from {mod} " +
									   $"tried to ignore an inexistent toggle tab {toggleTabTypeName}.");
						continue;
					}
					YaOptMod.Debug($"Toggle tab {toggleTabTypeName} will not be cached because of {mod}.");
					CompatibilityDefines.CachedIgnoredToggleTabCaching.Add(type);
				}
			}

			if (IgnoredJobFailurePredicting != null)
			{
				CompatibilityDefines.CachedIgnoredJobFailurePredicting.AddRange(IgnoredJobFailurePredicting);
			}
		}
	}
}