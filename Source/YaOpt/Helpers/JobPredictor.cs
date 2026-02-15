using JetBrains.Annotations;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Verse;
using Verse.AI;
using Exception = System.Exception;

namespace YaOpt.Helpers
{
	public static class JobPredictor
	{
		private static readonly ConcurrentDictionary<Pawn, JobPrediction> _jobPredictionMap =
			new ConcurrentDictionary<Pawn, JobPrediction>();

		[Flags]
		public enum ExpectedTargetStatus : byte
		{
			None          = 0b0000_0000,
			Spawned       = 0b0000_0001,
			Alive         = 0b0000_0011,
			Awake         = 0b0000_0111,
			InMentalState = 0b0000_1000,
			Forbidden     = 0b0001_0000,
		}

		public struct TargetStatusValidation : IEquatable<TargetStatusValidation>
		{
			public ExpectedTargetStatus ExpectedActorStatus;
			public ExpectedTargetStatus ExpectedTargetAStatus;
			public ExpectedTargetStatus ExpectedTargetBStatus;
			public ExpectedTargetStatus ExpectedTargetCStatus;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static ExpectedTargetStatus GetExpectedTargetStatus(Thing thing)
			{
				var status = ExpectedTargetStatus.None;
				if (thing == null)
					return status;
				if (!thing.Destroyed)
				{
					status |= ExpectedTargetStatus.Spawned;
					if (thing is Pawn pawn)
					{
						var health = pawn.health;
						if (!health.Dead)
						{
							status |= ExpectedTargetStatus.Alive;
							if (!health.Downed)
							{
								status |= ExpectedTargetStatus.Awake;
								if (pawn.InMentalState)
								{
									status |= ExpectedTargetStatus.InMentalState;
								}
							}
						}
					}
					if (thing is ThingWithComps thingWithComps && thingWithComps.compForbiddable?.Forbidden == true)
					{
						status |= ExpectedTargetStatus.Forbidden;
					}
				}

				return status;
			}

			public static TargetStatusValidation CreateValidation(Pawn actor, Job job)
			{
				if (actor == null || job == null)
					return default;

				return new TargetStatusValidation()
				{
					ExpectedActorStatus = GetExpectedTargetStatus(actor),
					ExpectedTargetAStatus = GetExpectedTargetStatus(job.targetA.Thing),
					ExpectedTargetBStatus = GetExpectedTargetStatus(job.targetB.Thing),
					ExpectedTargetCStatus = GetExpectedTargetStatus(job.targetC.Thing),
				};
			}

			public bool Equals(TargetStatusValidation other)
			{
				return ExpectedActorStatus == other.ExpectedActorStatus &&
					   ExpectedTargetAStatus == other.ExpectedTargetAStatus &&
					   ExpectedTargetBStatus == other.ExpectedTargetBStatus &&
					   ExpectedTargetCStatus == other.ExpectedTargetCStatus;
			}

			public override bool Equals(object obj)
			{
				return obj is TargetStatusValidation other && Equals(other);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					var hashCode = (int)ExpectedActorStatus;
					hashCode = (hashCode * 397) ^ (int)ExpectedTargetAStatus;
					hashCode = (hashCode * 397) ^ (int)ExpectedTargetBStatus;
					hashCode = (hashCode * 397) ^ (int)ExpectedTargetCStatus;
					return hashCode;
				}
			}

			public static bool operator ==(TargetStatusValidation left, TargetStatusValidation right)
			{
				return left.Equals(right);
			}

			public static bool operator !=(TargetStatusValidation left, TargetStatusValidation right)
			{
				return !left.Equals(right);
			}
		}

		public struct TargetDistanceValidation
		{
			public const int TOLERANCE = 2;
			public const int NEG_TOLERANCE = -TOLERANCE;
			public ushort ManhattanDistanceToTargetA;
			public ushort ManhattanDistanceToTargetB;
			public ushort ManhattanDistanceToTargetC;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static ushort GetManhattanDistance([NotNull] Thing a, [CanBeNull] Thing b)
			{
				if (b == null)
					return 0;

				var delta = a.Position - b.Position;
				return (ushort)(delta.x + delta.z);
			}

			public static TargetDistanceValidation CreateValidation(Pawn actor, Job job)
			{
				if (actor == null || job == null)
					return default;

				return new TargetDistanceValidation()
				{
					ManhattanDistanceToTargetA = GetManhattanDistance(actor, job.targetA.Thing),
					ManhattanDistanceToTargetB = GetManhattanDistance(actor, job.targetB.Thing),
					ManhattanDistanceToTargetC = GetManhattanDistance(actor, job.targetC.Thing),
				};
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private static bool DistanceAlmostEquals(ushort distance1, ushort distance2)
			{
				var delta = (int)distance1 - (int)distance2;
				return delta >= NEG_TOLERANCE && delta <= TOLERANCE;
			}

			public bool AlmostEquals(TargetDistanceValidation other)
			{
				return DistanceAlmostEquals(ManhattanDistanceToTargetA, other.ManhattanDistanceToTargetA) &&
					   DistanceAlmostEquals(ManhattanDistanceToTargetB, other.ManhattanDistanceToTargetB) &&
					   DistanceAlmostEquals(ManhattanDistanceToTargetC, other.ManhattanDistanceToTargetC);
			}
		}

		[StructLayout(LayoutKind.Auto, Size = 64)] // Fill a cache line to avoid false sharing
		public sealed class JobPrediction
		{
			public int UpdateTick = -1;
			public bool MayFail = false;
			public bool ShouldDoConstantJob = false;
			public TargetDistanceValidation DistanceValidation;
			public TargetStatusValidation StatusValidation;
		}

		public static void AddPawn(Pawn pawn)
		{
			if (!_jobPredictionMap.TryAdd(pawn, new JobPrediction()))
			{
				return; //todo: error
			}
		}

		public static void RemovePawn(Pawn pawn)
		{
			if (!_jobPredictionMap.TryRemove(pawn, out _))
			{
				return; //todo: error
			}
		}

		public static void ProcessPawn(Pawn pawn, int gameTick)
		{
			JobPrediction prediction = null;
			try
			{
				if (!_jobPredictionMap.TryGetValue(pawn, out prediction))
				{
					prediction = new JobPrediction();
					_jobPredictionMap[pawn] = prediction;
				}
				prediction.MayFail = PredictFail(pawn);
				if (!prediction.MayFail)
				{
					var job = pawn.CurJob;
					prediction.DistanceValidation = TargetDistanceValidation.CreateValidation(pawn, job);
					prediction.StatusValidation = TargetStatusValidation.CreateValidation(pawn, job);
				}
				if (ThingHelper.ShouldTickInterval(pawn, out var tickDeltaPlusOne))
				{
					prediction.ShouldDoConstantJob = PredictDoConstantJob(pawn, tickDeltaPlusOne);
				}
				else
				{
					prediction.ShouldDoConstantJob = true;
				}
				prediction.UpdateTick = gameTick;
			}
			catch (Exception ex)
			{
				YaOptMod.Error($"Error when predict if pawn {pawn.ToStringSafe()} job fails\n{ex}");
				if (prediction != null)
				{
					prediction.UpdateTick = -1;
				}
			}
		}

		public static bool PredictFail(Pawn pawn)
		{
			if (pawn.jobs == null || pawn.jobs.curDriver == null)
				return true;
			try
			{
				var job = pawn.CurJob;
				if (job != null)
				{
					if (CompatibilityDef.CachedIgnoredJobFailurePredicting.Contains(job.def))
						return true;

					// We can't validate target queue. So we will stop predicting if a job uses target queue.
					if (job.targetQueueA != null || job.targetQueueB != null)
					{
						return true;
					}
				}

				var jobDriver = pawn.jobs.curDriver;
				if (jobDriver.globalFailConditions != null)
				{
					foreach (var action in jobDriver.globalFailConditions)
					{
						if (action() != JobCondition.Ongoing)
						{
							if (pawn.jobs.debugLog)
							{
								pawn.jobs.DebugLogEvent(
									$"Predictor: {jobDriver.GetType().Name} ends current job " +
									$"{jobDriver.job.ToStringSafe()} because of globalFailConditions"
								);
							}
							return true;
						}
					}
				}

				var toil = ThingHelper.GetCurToil(jobDriver);
				if (toil?.endConditions != null)
				{
					foreach (var action in toil.endConditions)
					{
						if (action() != JobCondition.Ongoing)
						{
							if (pawn.jobs.debugLog)
							{
								pawn.jobs.DebugLogEvent(
									$"Predictor: {jobDriver.GetType().Name} ends current job " +
									$"{jobDriver.job.ToStringSafe()} because of endConditions"
								);
							}
							return true;
						}
					}
				}
			}
			catch (Exception ex)
			{
				YaOptMod.Error("Exception in JobPredictor.PredictFail for pawn " + pawn.ToStringSafe()
					+ "\n" + ex);
				return true;
			}
			return false;
		}

		public static bool PredictDoConstantJob(Pawn pawn, int delta)
		{
			if (!pawn.Spawned || pawn.jobs == null || !pawn.IsHashIntervalTick(30, delta))
				return false;
			var thinker = pawn.thinker;
			if (thinker?.ConstantThinkTree == null)
				return false;
			try
			{
				var thinkResult = thinker.ConstantThinkNodeRoot.TryIssueJobPackage(pawn, default);
				if (thinkResult.IsValid)
				{
					var result = ThingHelper.ShouldStartJobFromThinkTree(pawn.jobs, thinkResult);
					JobMaker.ReturnToPool(thinkResult.Job);
					return result;
				}
			}
			catch (Exception ex)
			{
				YaOptMod.Error("Exception in JobPredictor.PredictDoConstantJob for pawn " + pawn.ToStringSafe()
					+ "\n" + ex);
				return true;
			}
			return false;
		}

		public static void CleanCache()
		{
			_jobPredictionMap.Clear();
		}

		public static bool ShouldCheckJobFail(Pawn pawn)
		{
			if (_jobPredictionMap.TryGetValue(pawn, out var prediction))
			{
				if (Find.TickManager.TicksGame == prediction.UpdateTick)
				{
					if (!prediction.MayFail)
					{
						var job = pawn.CurJob;
						if (job != null)
						{
							var distanceValidation = TargetDistanceValidation.CreateValidation(pawn, job);
							if (!distanceValidation.AlmostEquals(prediction.DistanceValidation))
								return true;
							var statusValidation = TargetStatusValidation.CreateValidation(pawn, job);
							if (!statusValidation.Equals(prediction.StatusValidation))
								return true;
						}
						return false;
					}
				}
			}
			return true;
		}

		public static bool ShouldCheckConstantJob(Pawn pawn)
		{
			if (_jobPredictionMap.TryGetValue(pawn, out var prediction))
			{
				if (Find.TickManager.TicksGame == prediction.UpdateTick)
				{
					return prediction.ShouldDoConstantJob;
				}
			}
			return true;
		}
	}
}