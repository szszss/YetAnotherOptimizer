using Gilzoide.ManagedJobs;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Verse;
using Verse.AI;
using YaOpt.Defines;
using YaOpt.Helpers.ThreadLocal;
using static YaOpt.Defines.WorkGiverCompatibility.Parallelism;

namespace YaOpt.Helpers
{
	internal static class ParallelJobGiver
	{
		public static bool Running;

		private static /*volatile*/ int workingFence;

		private static int jobIssueErrorAfter;

		private static readonly ConcurrentBag<JobResult> jobResults = new ConcurrentBag<JobResult>();

		private static readonly ConcurrentQueue<int> jobIndexQueue = new ConcurrentQueue<int>();

		private static readonly List<JobResult> tmpList = new List<JobResult>();

		private static readonly Queue<(WorkGiver, int)> workGiversProcessedByMainThread =
			new Queue<(WorkGiver, int)>();

		private static int workGiverMainThreadBarrier;

		private static JobHandle jobHandle = default;

		private static Stopwatch debugStopwatch;

		private delegate bool PawnCanUseWorkGiverDelegate(JobGiver_Work instance, Pawn pawn, WorkGiver giver);

		private static readonly PawnCanUseWorkGiverDelegate pawnCanUseWorkGiver;

		private struct JobResult
		{
			public Job ResultJob;
			public WorkGiver Giver;
			public Exception CaughtException;
			public string ErrorInfo;
			public int ErrorOnceHash;
			public int Index;
			public JobTag Tag;
			public bool Successful;
			public bool IsScanner;

			public void Dispose(Pawn pawn)
			{
				if (ResultJob != null)
					JobMaker.ReturnToPool(ResultJob);

				if (ErrorInfo != null)
				{
					if (ErrorOnceHash != 0)
						Log.ErrorOnce(ErrorInfo, ErrorOnceHash);
					else
						Log.Error(ErrorInfo);
				}

				if (CaughtException != null)
					Log.Error(
						$"{pawn.ToStringSafe()} threw exception in WorkGiver " +
						$"{Giver}: {CaughtException}");
			}
		}

		static ParallelJobGiver()
		{
			pawnCanUseWorkGiver = AccessTools.MethodDelegate<PawnCanUseWorkGiverDelegate>(
					AccessTools.Method(typeof(JobGiver_Work), "PawnCanUseWorkGiver"), null, false, null);
		}

		public static ThinkResult ParellellyIssueJobPackage(JobGiver_Work jgw, Pawn pawn, List<WorkGiver> jobList)
		{
			if (Running)
				throw new Exception("Cannot recursively call ParellellyIssueJobPackage");
			ThreadLocalMapPawns.PushPooledListsStack();
			Running = true;
			try
			{
				workingFence = int.MaxValue;
				jobIssueErrorAfter = int.MaxValue;
				workGiverMainThreadBarrier = int.MaxValue;
				var thinkResult = ThinkResult.NoJob;
				var JOBDEBUG = pawn.jobs.debugLog;

				if (pawn == null)
					throw new ArgumentNullException(nameof(pawn));

				if (jobList == null)
					throw new ArgumentNullException(nameof(jobList));

				if (JOBDEBUG)
				{
					pawn.jobs.DebugLogEvent($"Try to parellelly issue job for pawn {pawn.ToStringSafe()}.");
					if (debugStopwatch == null)
						debugStopwatch = Stopwatch.StartNew();
					else
						debugStopwatch.Restart();
				}

				try
				{
					// Rebuilding regions would break too many things
					// and would never run safely in a multi-threaded environment.
					// Therefore, if there are dirty regions, we will rebuild them here.
					pawn.Map?.regionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms();

					// Also, ensure factions lists init in main thread.
					pawn.Map?.mapPawns.SpawnedPawnsInFaction(null);

					// Almost all WorkGiver_Scanners use the ComfyTemperatureMin/Max stat.
					// So we cache them here to avoid lock in worker threads.
					pawn.ComfortableTemperatureRange();
				}
				catch (Exception ex)
				{
					Log.Error(ex.ToString());
				}

				var serialTaskCount = 0;
				var parallelTaskCount = 0;
				for (var i = 0; i < jobList.Count; i++)
				{
					var workGiver = jobList[i];
					if (pawnCanUseWorkGiver(jgw, pawn, workGiver))
					{
						if (CompatibilityDefines.CachedWorkGiverParallelism
							.TryGetValue(workGiver.def.defName, out var parallelism) && parallelism != Full)
						{
							serialTaskCount++;
							workGiversProcessedByMainThread.Enqueue((workGiver, i));
							if (parallelism == MainThreadedDelayed && workGiverMainThreadBarrier > i)
							{
								workGiverMainThreadBarrier = i;
							}
						}
						else
						{
							parallelTaskCount++;
							jobIndexQueue.Enqueue(i);
						}
					}
				}

				var mainThreadHasJobToDone = workGiversProcessedByMainThread.Count > 0;
				if (JOBDEBUG)
				{
					pawn.jobs.DebugLogEvent($"{parallelTaskCount} of {jobList?.Count} WorkGivers are being processed by " +
											$"{JobsUtility.JobWorkerMaximumCount} worker threads.");
					if (mainThreadHasJobToDone)
					{
						pawn.jobs.DebugLogEvent(
							$"There are also {serialTaskCount} WorkGivers are being processed by main thread.");
					}
				}

				YaOptGlobal.IsParallelRunningInTick = true;
				jobHandle = new ManagedJobFor(
						new IssueJobPackageJob(pawn, jobList))
					.ScheduleParallel(parallelTaskCount, 1);
				JobHandle.ScheduleBatchedJobs();
				while (!jobHandle.IsCompleted)
				{
					if (mainThreadHasJobToDone)
						mainThreadHasJobToDone = MainThreadProcess(pawn, false);
					if (workingFence < int.MaxValue) // If there is any WorkGiver issued job
					{
						if (JOBDEBUG)
						{
							pawn.jobs.DebugLogEvent($"A job (index={workingFence}) was issued. " +
													$"Time passed: {debugStopwatch.Elapsed.TotalMilliseconds}ms. " +
													"Stopping worker threads...");
						}

						jobHandle.CompleteWithSpinWait(); // Make sure it's finished
					}
				}
				YaOptGlobal.IsParallelRunningInTick = false;
				if (mainThreadHasJobToDone)
				{
					MainThreadProcess(pawn, true);
				}

				if (JOBDEBUG)
				{
					pawn.jobs.DebugLogEvent("All worker threads had stopped. " +
											$"Total time passed: {debugStopwatch.Elapsed.TotalMilliseconds}ms.");
					pawn.jobs.DebugLogEvent(PrintJobResults());
				}

				var bestIndex = int.MaxValue;
				JobResult bestResult = default;
				while (jobResults.TryTake(out var result))
				{
					if (result.Successful &&
						result.Index < bestIndex &&
						result.Index < jobIssueErrorAfter)
					{
						bestIndex = result.Index;
						bestResult = result;
					}
					tmpList.Add(result);
				}

				if (bestIndex < int.MaxValue)
				{
					if (JOBDEBUG)
					{
						if (bestResult.IsScanner)
						{
							pawn.jobs.DebugLogEvent(
								$"JobGiver_Work parellelly produced scan Job {bestResult.ResultJob.ToStringSafe()} " +
								$"from {bestResult.Giver}");
						}
						else
						{
							pawn.jobs.DebugLogEvent(
								$"JobGiver_Work parellelly produced non-scan Job {bestResult.ResultJob.ToStringSafe()} " +
								$"from {bestResult.Giver}");
						}
					}

					for (var i = 0; i < tmpList.Count; i++)
					{
						if (tmpList[i].Index == bestIndex)
						{
							tmpList.RemoveAt(i);
							break;
						}
					}

					thinkResult = new ThinkResult(bestResult.ResultJob, jgw, bestResult.Tag);
				}
				else if (JOBDEBUG)
				{
					pawn.jobs.DebugLogEvent("No job was issued. This could be either an error, " +
											"or maybe pawn really has nothing to do.");
				}

				if (JOBDEBUG)
				{
					pawn.jobs.DebugLogEvent("Returning ThinkResult. Total time passed: " +
											$"{debugStopwatch.Elapsed.TotalMilliseconds}ms");
					debugStopwatch.Stop();
				}

				return thinkResult;
			}
			finally
			{
				Running = false;
				ThreadLocalMapPawns.PopPooledListsStack();
				while (jobResults.TryTake(out var result))
				{
					result.Dispose(pawn);
				}

				foreach (var result in tmpList)
				{
					result.Dispose(pawn);
				}
				tmpList.Clear();

				while (jobIndexQueue.TryDequeue(out _))
				{
				}

				workGiversProcessedByMainThread.Clear();
			}
		}

		private static string PrintJobResults()
		{
			var chosen = int.MaxValue;
			var list = jobResults.ToArray();
			Array.Sort(list, (a, b) => a.Index.CompareTo(b.Index));
			var sb = new StringBuilder("Issued JobResult count: ")
				.Append(list.Length).AppendLine(". Now print details.");
			foreach (var result in list)
			{
				if (result.Successful &&
					result.Index < chosen &&
					result.Index < jobIssueErrorAfter)
				{
					chosen = result.Index;
				}
			}
			foreach (var result in list)
			{
				sb.Append("Index: ").Append(result.Index);
				if (result.Index == chosen)
					sb.Append(" (SELECTED)");
				sb.Append(" WorkGiver: ").Append(result.Giver.ToStringSafe());
				if (result.ResultJob != null)
					sb.Append(" Job: ").Append(result.ResultJob);

				if (result.Index == chosen)
				{
					sb.AppendLine(" Succeed and will be selected.");
				}
				else
				{
					if (result.Successful)
					{
						sb.Append(" Succeed but will be discarded because ");
						if (result.Index >= jobIssueErrorAfter)
						{
							sb.AppendLine("a non-synchronized error was happen.");
						}
						else if (result.Index >= chosen)
						{
							sb.AppendLine("a higher-priority job (lesser index) has been selected.");
						}
						else
						{
							sb.AppendLine("unknown reason.");
						}
					}
					else
					{
						sb.AppendLine(" Failed because of an error. Details will be printed later.");
					}
				}
			}

			return sb.ToString();
		}

		private readonly struct ScanThingsClosure
		{
			private readonly WorkGiver_Scanner _scanner;
			private readonly Pawn _pawn;

			public ScanThingsClosure(WorkGiver_Scanner scanner, Pawn pawn)
			{
				_scanner = scanner;
				_pawn = pawn;
			}

			public bool Validate(Thing t)
			{
				if (!t.IsForbidden(_pawn))
				{
					return _scanner.HasJobOnThing(_pawn, t);
				}
				return false;
			}

			public float GetPriority(Thing t)
			{
				return _scanner.GetPriority(_pawn, t);
			}
		}

		private struct ScanCellsClosure
		{
			private readonly WorkGiver_Scanner _scanner;
			private readonly Pawn _pawn;
			private readonly IntVec3 _pawnPosition;
			private readonly Danger _maxPathDanger;
			private readonly bool _prioritized;
			private readonly bool _allowUnreachable;

			private float _closestDistSquared;
			private float _bestPriority;
			public TargetInfo BestTargetOfLastPriority;
			public WorkGiver_Scanner ScannerWhoProvidedTarget;

			public ScanCellsClosure(WorkGiver_Scanner scanner, Pawn pawn, IntVec3 pawnPosition,
				Danger maxPathDanger, bool prioritized, bool allowUnreachable) : this()
			{
				_scanner = scanner;
				_pawn = pawn;
				_pawnPosition = pawnPosition;
				_maxPathDanger = maxPathDanger;
				_prioritized = prioritized;
				_allowUnreachable = allowUnreachable;
				_closestDistSquared = 99999f;
				_bestPriority = float.MinValue;
				BestTargetOfLastPriority = TargetInfo.Invalid;
				ScannerWhoProvidedTarget = null;
			}

			[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
			public void ProcessCell(IntVec3 c)
			{
				bool flag2 = false;
				float num5 = (c - _pawnPosition).LengthHorizontalSquared;
				float num6 = 0f;
				if (_prioritized)
				{
					if (!c.IsForbidden(_pawn) && _scanner.HasJobOnCell(_pawn, c))
					{
						if (!_allowUnreachable && !_pawn.CanReach(c, _scanner.PathEndMode, _maxPathDanger))
						{
							return;
						}
						num6 = _scanner.GetPriority(_pawn, c);
						if (num6 > _bestPriority || (num6 == _bestPriority && num5 < _closestDistSquared))
						{
							flag2 = true;
						}
					}
				}
				else if (num5 < _closestDistSquared && !c.IsForbidden(_pawn) && _scanner.HasJobOnCell(_pawn, c))
				{
					if (!_allowUnreachable && !_pawn.CanReach(c, _scanner.PathEndMode, _maxPathDanger))
					{
						return;
					}
					flag2 = true;
				}
				if (flag2)
				{
					BestTargetOfLastPriority = new TargetInfo(c, _pawn.Map);
					ScannerWhoProvidedTarget = _scanner;
					_closestDistSquared = num5;
					_bestPriority = num6;
				}
			}
		}

		private static bool MainThreadProcess(Pawn pawn, bool canCrossBarrier)
		{
			while (workGiversProcessedByMainThread.Count > 0)
			{
				var pair = workGiversProcessedByMainThread.Peek();
				if (canCrossBarrier || pair.Item2 < workGiverMainThreadBarrier)
				{
					pair = workGiversProcessedByMainThread.Dequeue();
					if (ShouldStop(pair.Item2))
						continue;
					TryIssue(pawn, pair.Item1, pair.Item2);
				}
				else
				{
					return false;
				}
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool ShouldStop(int workGiverIndex)
		{
			return workGiverIndex > workingFence;
		}

		private static bool UpdateProgress(int workGiverIndex)
		{
			while (true)
			{
				var fence = workingFence;
				if (workGiverIndex > fence)
				{
					return false;
				}
				if (Interlocked.CompareExchange(ref workingFence, workGiverIndex, fence) == fence)
					return true;
			}
		}

		private static void TryIssue(Pawn pawn, WorkGiver workGiver, int workGiverIndex)
		{
			Job job = null;
			try
			{
				job = workGiver.NonScanJob(pawn);
				if (job != null)
				{
					UpdateProgress(workGiverIndex);
					jobResults.Add(new JobResult()
					{
						Successful = true,
						ResultJob = job,
						Giver = workGiver,
						Index = workGiverIndex,
						Tag = workGiver.def.tagToGive,
						IsScanner = false,
					});
					return;
				}

				if (workGiver is WorkGiver_Scanner scanner)
				{
					TargetInfo bestTargetOfLastPriority = TargetInfo.Invalid;
					WorkGiver_Scanner scannerWhoProvidedTarget = null;
					if (scanner.def.scanThings)
					{
						Thing thing;
						var potentialWorkThings = scanner.PotentialWorkThingsGlobal(pawn);
						var closure = new ScanThingsClosure(scanner, pawn);
						bool flag = pawn.carryTracker?.CarriedThing != null &&
									scanner.PotentialWorkThingRequest.Accepts(pawn.carryTracker.CarriedThing) &&
									closure.Validate(pawn.carryTracker.CarriedThing);
						if (ShouldStop(workGiverIndex))
							return;
						if (scanner.Prioritized)
						{
							var searchSet = potentialWorkThings ??
											pawn.Map.listerThings.ThingsMatching(
												scanner.PotentialWorkThingRequest);
							if (ShouldStop(workGiverIndex))
								return;
							if (scanner.AllowUnreachable)
							{
								thing = GenClosest.ClosestThing_Global(pawn.Position, searchSet, 99999f,
									closure.Validate, closure.GetPriority);
							}
							else
							{
								thing = GenClosest.ClosestThing_Global_Reachable(
									pawn.Position, pawn.Map, searchSet, scanner.PathEndMode,
									TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)), 9999f,
									closure.Validate, closure.GetPriority);
							}

							if (ShouldStop(workGiverIndex))
								return;
							if (flag)
							{
								if (thing != null)
								{
									var num2 = scanner.GetPriority(pawn, pawn.carryTracker.CarriedThing);
									var num3 = scanner.GetPriority(pawn, thing);
									if (num2 >= num3)
									{
										thing = pawn.carryTracker.CarriedThing;
									}
								}
								else
								{
									thing = pawn.carryTracker.CarriedThing;
								}
							}
						}
						else if (flag)
						{
							thing = pawn.carryTracker.CarriedThing;
						}
						else if (scanner.AllowUnreachable)
						{
							var searchSet2 = potentialWorkThings ??
											 pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
							if (ShouldStop(workGiverIndex))
								return;
							thing = GenClosest.ClosestThing_Global(pawn.Position, searchSet2, 99999f, closure.Validate);
						}
						else
						{
							thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
								scanner.PotentialWorkThingRequest, scanner.PathEndMode,
								TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)), 9999f, closure.Validate,
								potentialWorkThings, 0, scanner.MaxRegionsToScanBeforeGlobalSearch,
								potentialWorkThings != null);
						}
						if (thing != null)
						{
							bestTargetOfLastPriority = thing;
							scannerWhoProvidedTarget = scanner;
						}
					}

					if (scanner.def.scanCells)
					{
						if (ShouldStop(workGiverIndex))
							return;
						var closure = new ScanCellsClosure(scanner, pawn, pawn.Position,
							scanner.MaxPathDanger(pawn), scanner.Prioritized, scanner.AllowUnreachable);
						closure.BestTargetOfLastPriority = bestTargetOfLastPriority;
						closure.ScannerWhoProvidedTarget = scannerWhoProvidedTarget;
						var potentialWorkCells = scanner.PotentialWorkCellsGlobal(pawn);
						if (potentialWorkCells is IList<IntVec3> lipotentialWorkCellList)
						{
							for (int num4 = 0; num4 < lipotentialWorkCellList.Count; num4++)
							{
								closure.ProcessCell(lipotentialWorkCellList[num4]);
							}
						}
						else
						{
							foreach (IntVec3 item in potentialWorkCells)
							{
								closure.ProcessCell(item);
							}
						}
						bestTargetOfLastPriority = closure.BestTargetOfLastPriority;
						scannerWhoProvidedTarget = closure.ScannerWhoProvidedTarget;
					}

					if (bestTargetOfLastPriority.IsValid && scannerWhoProvidedTarget != null)
					{
						if (ShouldStop(workGiverIndex))
							return;
						job = bestTargetOfLastPriority.HasThing ?
							scannerWhoProvidedTarget.JobOnThing(pawn, bestTargetOfLastPriority.Thing) :
							scannerWhoProvidedTarget.JobOnCell(pawn, bestTargetOfLastPriority.Cell);
						if (job != null)
						{
							UpdateProgress(workGiverIndex);
							job.workGiverDef = scannerWhoProvidedTarget.def;
							jobResults.Add(new JobResult()
							{
								Successful = true,
								ResultJob = job,
								Giver = workGiver,
								Index = workGiverIndex,
								Tag = workGiver.def.tagToGive,
								IsScanner = true,
							});
							return;
						}

						if (UpdateProgress(workGiverIndex))
						{
							jobIssueErrorAfter = workGiverIndex;
							jobResults.Add(new JobResult()
							{
								Successful = false,
								ErrorInfo = $"{scannerWhoProvidedTarget.ToStringSafe()} " +
											$"provided target {bestTargetOfLastPriority} " +
											$"but yielded no actual job for pawn {pawn.ToStringSafe()}. " +
											"The CanGiveJob and JobOnX methods may not be synchronized.",
								ErrorOnceHash = workGiver.GetHashCode(),
								Giver = workGiver,
								Index = workGiverIndex
							});
						}
					}
				}
			}
			catch (Exception ex)
			{
				jobResults.Add(new JobResult()
				{
					Successful = false,
					ResultJob = job,
					CaughtException = ex,
					Giver = workGiver,
					Index = workGiverIndex,
				});
			}
		}

		private readonly struct IssueJobPackageJob : IJobFor
		{
			private readonly Pawn _pawn;

			private readonly List<WorkGiver> _jobList;

			public IssueJobPackageJob(Pawn pawn, List<WorkGiver> jobList)
			{
				_pawn = pawn;
				_jobList = jobList;
			}

			public void Execute(int _)
			{
				if (!jobIndexQueue.TryDequeue(out var workGiverIndex))
					return;

				if (ShouldStop(workGiverIndex))
					return;

				var workGiver = _jobList[workGiverIndex];
				TryIssue(_pawn, workGiver, workGiverIndex);
			}
		}
	}
}