using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;
using static YaOpt.CompatibilityDef.WorkGiverCompatibility;

namespace YaOpt
{
	public class CompatibilityDef : Def
	{
		public static readonly HashSet<string> CachedBannedOptimizations = new HashSet<string>();

		public static readonly Dictionary<string, string> CachedBannedBy = new Dictionary<string, string>();

		public static readonly HashSet<Type> CachedIgnoredToggleTabCaching = new HashSet<Type>();

		public static readonly HashSet<JobDef> CachedIgnoredJobFailurePredicting = new HashSet<JobDef>();

		public static readonly Dictionary<string, Parallelism> CachedWorkGiverParallelism =
			new Dictionary<string, Parallelism>();

		public bool noErrorLog;

		[NoTranslate]
		public List<string> bannedOptimizations;

		[NoTranslate]
		public List<string> ignoredToggleTabCaching;

		[NoTranslate]
		public List<JobDef> ignoredJobFailurePredicting;

		public List<WorkGiverCompatibility> workGiverCompatibilities;

		public class WorkGiverCompatibility
		{
			[NoTranslate]
			public string workGiverDefName;

			[NoTranslate]
			public string workGiverClass;

			public Parallelism parallelism;

			public enum Parallelism
			{
				Full,
				MainThreaded,
				MainThreadedDelayed,
			}

			[UsedImplicitly]
			public void LoadDataFromXmlCustom(XmlNode xmlRoot)
			{
				XmlNode elem = null;
				if ((elem = xmlRoot.SelectSingleNode("workGiverDefName")) != null)
				{
					workGiverDefName = ParseHelper.FromString<string>(elem.InnerText);
				}
				if ((elem = xmlRoot.SelectSingleNode("workGiverClass")) != null)
				{
					workGiverClass = ParseHelper.FromString<string>(elem.InnerText);
				}
				if ((elem = xmlRoot.SelectSingleNode("parallelism")) != null)
				{
					if (!Enum.TryParse(ParseHelper.FromString<string>(elem.InnerText), true, out parallelism))
					{
						throw new XmlException(
							$"Wrong YaOpt.CompatibilityDef.WorkGiverCompatibility.Parallelism: {parallelism}");
					}
				}
			}
		}

		public static void Cache()
		{
			var workGivers = DefDatabase<WorkGiverDef>.AllDefsListForReading;

			foreach (var def in DefDatabase<CompatibilityDef>.AllDefs)
			{
				var mod = def.modContentPack.Name;

				if (def.bannedOptimizations != null)
				{
					foreach (var bannedOptimization in def.bannedOptimizations)
					{
						var index = YaOptGlobal.Settings.AllOptimizations.FirstIndexOf(
							opt => opt.SettingId == bannedOptimization);
						if (index < 0)
						{
							if (def.noErrorLog)
								YaOptMod.Debug($"Optimization {bannedOptimization} not found.");
							else
								YaOptMod.Error($"{mod} couldn't find optimization {bannedOptimization}");
							continue;
						}
						YaOptMod.Debug($"Optimization {bannedOptimization} is banned by {mod}.");
						CachedBannedOptimizations.Add(bannedOptimization);
						CachedBannedBy[bannedOptimization] = mod;
					}
				}

				if (def.ignoredToggleTabCaching != null)
				{
					foreach (var toggleTabTypeName in def.ignoredToggleTabCaching)
					{
						var type = AccessTools.TypeByName(toggleTabTypeName);
						if (type == null)
						{
							if (def.noErrorLog)
								YaOptMod.Debug($"Toggle tab {toggleTabTypeName} not found.");
							else
								YaOptMod.Error($"{mod} tried to ignore an inexistent toggle tab {toggleTabTypeName} from {mod}.");
							continue;
						}
						YaOptMod.Debug($"Toggle tab {toggleTabTypeName} will not be cached because of {mod}.");
						CachedIgnoredToggleTabCaching.Add(type);
					}
				}

				if (def.ignoredJobFailurePredicting != null)
				{
					CachedIgnoredJobFailurePredicting.AddRange(def.ignoredJobFailurePredicting);
				}

				if (def.workGiverCompatibilities != null)
				{
					foreach (var compatibility in def.workGiverCompatibilities)
					{
						var hasClass = !string.IsNullOrWhiteSpace(compatibility.workGiverClass);
						var hasDefName = !string.IsNullOrWhiteSpace(compatibility.workGiverDefName);
						if (hasClass && hasDefName)
						{
							YaOptMod.Error($"{mod} defined workGiverClass {compatibility.workGiverClass} " +
										   $"and workGiverDefName {compatibility.workGiverDefName}. " +
										   "It's not possible to define both workGiverClass and " +
										   "workGiverDefName simultaneously.");
						}

						if (hasDefName)
						{
							var wg = workGivers.Find(wgd => wgd.defName == compatibility.workGiverDefName);
							if (wg == null)
							{
								if (def.noErrorLog)
									YaOptMod.Debug($"WorkGiver {compatibility.workGiverDefName} not found.");
								else
									YaOptMod.Error($"{mod} couldn't find WorkGiver {compatibility.workGiverDefName}.");
								continue;
							}
							YaOptMod.Debug($"The parallelism of WorkGiver {wg.defName} now is {compatibility.parallelism}, " +
										   $"set by {mod}.");
							CachedWorkGiverParallelism[wg.defName] = compatibility.parallelism;
						}
						else if (hasClass)
						{
							var workGiverType = AccessTools.TypeByName(compatibility.workGiverClass);
							if (workGiverType == null)
							{
								if (def.noErrorLog)
									YaOptMod.Debug($"WorkGiver class {compatibility.workGiverClass} not found.");
								else
									YaOptMod.Error($"{mod} couldn't find WorkGiver class {compatibility.workGiverClass}.");
								continue;
							}
							foreach (var wg in workGivers
										 .Where(wgd => workGiverType.IsAssignableFrom(wgd.giverClass)))
							{
								YaOptMod.Debug($"The parallelism of WorkGiver {wg.defName} now is {compatibility.parallelism}, " +
											   $"set by {mod}.");
								CachedWorkGiverParallelism[wg.defName] = compatibility.parallelism;
							}
						}
					}
				}
			}
		}
	}
}