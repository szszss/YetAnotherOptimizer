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

		public bool NoErrorLog { get => noErrorLog; set => noErrorLog = value; }

		public List<string> BannedOptimizations { get => bannedOptimizations;  set => bannedOptimizations = value; }

		public List<string> IgnoredToggleTabCaching { get => ignoredToggleTabCaching; set => ignoredToggleTabCaching = value; }

		public List<JobDef> IgnoredJobFailurePredicting { get => ignoredJobFailurePredicting; set => ignoredJobFailurePredicting = value; }

		public List<WorkGiverCompatibility> WorkGiverCompatibilities { get => workGiverCompatibilities; set => workGiverCompatibilities = value; }

		private bool noErrorLog;

		[NoTranslate]
		private List<string> bannedOptimizations;

		[NoTranslate]
		private List<string> ignoredToggleTabCaching;

		[NoTranslate]
		private List<JobDef> ignoredJobFailurePredicting;

		private List<WorkGiverCompatibility> workGiverCompatibilities;

		public class WorkGiverCompatibility
		{
			[NoTranslate]
			public string WorkGiverDefName;

			[NoTranslate]
			public string WorkGiverClass;

			public Parallelism WorkGiverParallelism;

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
					WorkGiverDefName = ParseHelper.FromString<string>(elem.InnerText);
				}
				if ((elem = xmlRoot.SelectSingleNode("workGiverClass")) != null)
				{
					WorkGiverClass = ParseHelper.FromString<string>(elem.InnerText);
				}
				if ((elem = xmlRoot.SelectSingleNode("parallelism")) != null)
				{
					if (!Enum.TryParse(ParseHelper.FromString<string>(elem.InnerText), true, out WorkGiverParallelism))
					{
						throw new XmlException(
							$"Wrong YaOpt.CompatibilityDef.WorkGiverCompatibility.Parallelism: {WorkGiverParallelism}");
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

				if (def.BannedOptimizations != null)
				{
					foreach (var bannedOptimization in def.BannedOptimizations)
					{
						var index = YaOptGlobal.Settings.AllOptimizations.FirstIndexOf(
							opt => opt.SettingId == bannedOptimization);
						if (index < 0)
						{
							if (def.NoErrorLog)
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

				if (def.IgnoredToggleTabCaching != null)
				{
					foreach (var toggleTabTypeName in def.IgnoredToggleTabCaching)
					{
						var type = AccessTools.TypeByName(toggleTabTypeName);
						if (type == null)
						{
							if (def.NoErrorLog)
								YaOptMod.Debug($"Toggle tab {toggleTabTypeName} not found.");
							else
								YaOptMod.Error($"{mod} tried to ignore an inexistent toggle tab {toggleTabTypeName} from {mod}.");
							continue;
						}
						YaOptMod.Debug($"Toggle tab {toggleTabTypeName} will not be cached because of {mod}.");
						CachedIgnoredToggleTabCaching.Add(type);
					}
				}

				if (def.IgnoredJobFailurePredicting != null)
				{
					CachedIgnoredJobFailurePredicting.AddRange(def.IgnoredJobFailurePredicting);
				}

				if (def.WorkGiverCompatibilities != null)
				{
					foreach (var compatibility in def.WorkGiverCompatibilities)
					{
						var hasClass = !string.IsNullOrWhiteSpace(compatibility.WorkGiverClass);
						var hasDefName = !string.IsNullOrWhiteSpace(compatibility.WorkGiverDefName);
						if (hasClass && hasDefName)
						{
							YaOptMod.Error($"{mod} defined workGiverClass {compatibility.WorkGiverClass} " +
										   $"and workGiverDefName {compatibility.WorkGiverDefName}. " +
										   "It's not possible to define both workGiverClass and " +
										   "workGiverDefName simultaneously.");
						}

						if (hasDefName)
						{
							var wg = workGivers.Find(wgd => wgd.defName == compatibility.WorkGiverDefName);
							if (wg == null)
							{
								if (def.NoErrorLog)
									YaOptMod.Debug($"WorkGiver {compatibility.WorkGiverDefName} not found.");
								else
									YaOptMod.Error($"{mod} couldn't find WorkGiver {compatibility.WorkGiverDefName}.");
								continue;
							}
							YaOptMod.Debug($"The parallelism of WorkGiver {wg.defName} now is {compatibility.WorkGiverParallelism}, " +
										   $"set by {mod}.");
							CachedWorkGiverParallelism[wg.defName] = compatibility.WorkGiverParallelism;
						}
						else if (hasClass)
						{
							var workGiverType = AccessTools.TypeByName(compatibility.WorkGiverClass);
							if (workGiverType == null)
							{
								if (def.NoErrorLog)
									YaOptMod.Debug($"WorkGiver class {compatibility.WorkGiverClass} not found.");
								else
									YaOptMod.Error($"{mod} couldn't find WorkGiver class {compatibility.WorkGiverClass}.");
								continue;
							}
							foreach (var wg in workGivers
										 .Where(wgd => workGiverType.IsAssignableFrom(wgd.giverClass)))
							{
								YaOptMod.Debug($"The parallelism of WorkGiver {wg.defName} now is {compatibility.WorkGiverParallelism}, " +
											   $"set by {mod}.");
								CachedWorkGiverParallelism[wg.defName] = compatibility.WorkGiverParallelism;
							}
						}
					}
				}
			}
		}
	}
}