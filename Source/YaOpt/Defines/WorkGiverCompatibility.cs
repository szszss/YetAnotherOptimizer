using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;

namespace YaOpt.Defines
{
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

		public IEnumerable<(string, Parallelism)> Read(List<WorkGiverDef> workGivers, string owner)
		{
			var hasClass = !string.IsNullOrWhiteSpace(WorkGiverClass);
			var hasDefName = !string.IsNullOrWhiteSpace(WorkGiverDefName);
			if (hasClass && hasDefName)
			{
				YaOptMod.Error($"{owner} defined workGiverClass {WorkGiverClass} " +
							   $"and workGiverDefName {WorkGiverDefName}. " +
							   "It's not possible to define both workGiverClass and " +
							   "workGiverDefName simultaneously. " +
							   $"{WorkGiverClass} will be ignored.");
			}

			if (hasDefName)
			{
				var wg = workGivers.Find(wgd => wgd.defName == WorkGiverDefName);
				if (wg == null)
				{
					YaOptMod.Error($"{owner} couldn't find WorkGiver {WorkGiverDefName}.");
					yield break;
				}
				YaOptMod.Debug($"The parallelism of WorkGiver {wg.defName} now is {WorkGiverParallelism}, " +
							   $"set by {owner}.");
				yield return (wg.defName, WorkGiverParallelism);
			}
			else if (hasClass)
			{
				var workGiverType = AccessTools.TypeByName(WorkGiverClass);
				if (workGiverType == null)
				{
					YaOptMod.Error($"{owner} couldn't find WorkGiver class {WorkGiverClass}.");
					yield break;
				}
				foreach (var wg in workGivers
							 .Where(wgd => workGiverType.IsAssignableFrom(wgd.giverClass)))
				{
					YaOptMod.Debug($"The parallelism of WorkGiver {wg.defName} now is {WorkGiverParallelism}, " +
								   $"set by {owner}.");
					yield return (wg.defName, WorkGiverParallelism);
				}
			}
		}
	}
}