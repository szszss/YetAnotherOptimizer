using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace YaOpt.Defines
{
	public static class CompatibilityDefines
	{
		public static readonly HashSet<string> CachedBannedOptimizations = new HashSet<string>();

		public static readonly Dictionary<string, string> CachedBannedBy = new Dictionary<string, string>();

		public static readonly HashSet<Type> CachedIgnoredToggleTabCaching = new HashSet<Type>();

		public static readonly HashSet<JobDef> CachedIgnoredJobFailurePredicting = new HashSet<JobDef>();

		public static readonly Dictionary<string, WorkGiverCompatibility.Parallelism> CachedWorkGiverParallelism =
			new Dictionary<string, WorkGiverCompatibility.Parallelism>();

		public static readonly Dictionary<MethodBase, List<string>> ThreadLocalPatches =
			new Dictionary<MethodBase,List<string>>();

		public static void Load()
		{
			var workGivers = DefDatabase<WorkGiverDef>.AllDefsListForReading;

			foreach (var def in DefDatabase<CompatibilityDef>.AllDefs)
			{
				var owner = def.modContentPack.Name;

				try
				{
					def.Cache();
				}
				catch (Exception e)
				{
					YaOptMod.Error($"Error when parsing CompatibilityDef {def.defName} from {owner}. " +
								   $"{e.ToStringSafe()}");
					continue;
				}

				if (def.WorkGiverCompatibilities != null)
				{
					foreach (var compatibility in def.WorkGiverCompatibilities)
					{
						try
						{
							foreach (var (jobName, parallelism) in
									 compatibility.Read(workGivers, owner))
							{
								CachedWorkGiverParallelism[jobName] = parallelism;
							}
						}
						catch (Exception e)
						{
							YaOptMod.Error("Error when parsing WorkGiverCompatibilities of CompatibilityDef " +
										   $"{def.defName} from {owner}. {e.ToStringSafe()}");
						}
					}
				}

				if (def.ThreadLocalPatches != null)
				{
					foreach (var threadLocalPatch in def.ThreadLocalPatches)
					{
						try
						{
							var result = threadLocalPatch.Read(owner);
							if (!ThreadLocalPatches.TryGetValue(result.Item1, out var list))
							{
								list = new List<string>(1);
								ThreadLocalPatches[result.Item1] = list;
							}
							list.Add(result.Item2);
						}
						catch (Exception e)
						{
							YaOptMod.Error("Error when parsing ThreadLocalPatches of CompatibilityDef " +
										   $"{def.defName} from {owner}. {e.ToStringSafe()}");
						}
					}
				}
			}
		}
	}
}