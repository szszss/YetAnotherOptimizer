using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Verse;
using YaOpt.Helpers;
using YaOpt.Helpers.ThreadSafe;

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
			new Dictionary<MethodBase, List<string>>();

		public static readonly Dictionary<MethodBase, LockPatchManager.PatchRequest> LockPatches =
			new Dictionary<MethodBase, LockPatchManager.PatchRequest>();

		private static readonly BitArray _ignoredJobFailurePredictingBloomFilter =
			new BitArray(4096);

		private const int BLOOMFILTER_MASK = 0xFFF;

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

				if (def.LockPatches != null)
				{
					foreach (var lockPatch in def.LockPatches)
					{
						try
						{
							var result = lockPatch.Read(owner);
							if (LockPatches.TryGetValue(result.TargetMethod, out var existValue))
							{
								if (existValue == result)
								{
									YaOptMod.Warning(
										$"Duplicated defined the lock for {result.TargetMethod.FullName()}.");
									continue;
								}
								YaOptMod.Error(
									$"Duplicated defined the lock for {result.TargetMethod.FullName()} " +
									$"with different parameters. Exist:{existValue} New:{result}");
							}
							LockPatches[result.TargetMethod] = result;
						}
						catch (Exception e)
						{
							YaOptMod.Error("Error when parsing LockPatches of CompatibilityDef " +
										   $"{def.defName} from {owner}. {e.ToStringSafe()}");
						}
					}
				}
			}

			foreach (var jobDef in CachedIgnoredJobFailurePredicting)
			{
				var mask = jobDef.shortHash & BLOOMFILTER_MASK;
				_ignoredJobFailurePredictingBloomFilter[mask] = true;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsJobFailurePredictingIgnored(JobDef jobDef)
		{
			var mask = jobDef.shortHash & BLOOMFILTER_MASK;
			if (_ignoredJobFailurePredictingBloomFilter[mask])
			{
				return CachedIgnoredJobFailurePredicting.Contains(jobDef);
			}
			return false;
		}
	}
}