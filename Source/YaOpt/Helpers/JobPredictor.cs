using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Verse;
using Verse.AI;
using YaOpt.Defines;
using Exception = System.Exception;

namespace YaOpt.Helpers
{
	/// <summary>
	/// Predicts pawn job outcomes to enable parallel processing and skip redundant checks on the main thread.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This class is the core of the parallel pawn tick optimization. It runs on worker threads
	/// during the parallel tick phase and pre-calculates:
	/// <list type="bullet">
	/// <item>Whether a pawn's current job will fail (enabling the main thread to skip job failure checks).</item>
	/// <item>Whether a pawn needs to start a constant job (like reacting to threats).</item>
	/// <item>Target status and distance validation for quick comparison on the main thread.</item>
	/// </list>
	/// </para>
	/// </remarks>
	/// <seealso cref="ParallelPawnTickManager"/>
	/// <seealso cref="YaOptSettings.OptParallelPawnTick"/>
	public static class JobPredictor
	{
		/// <summary>
		/// Thread-safe map from pawn to its prediction data.
		/// </summary>
		private static readonly Dictionary<int, JobPrediction> _jobPredictionMap =
			new Dictionary<int, JobPrediction>();

		/// <summary>
		/// Flags representing the expected state of a target (actor or job target).
		/// </summary>
		[Flags]
		public enum ExpectedTargetStatus : byte
		{
			None          = 0b0000_0000,
			/// <summary>Target is spawned and not destroyed.</summary>
			Spawned       = 0b0000_0001,
			/// <summary>Target is a pawn that is not dead.</summary>
			Alive         = 0b0000_0011,
			/// <summary>Target is a pawn that is not downed.</summary>
			Awake         = 0b0000_0111,
			/// <summary>Target is a pawn that is in a mental state.</summary>
			InMentalState = 0b0000_1000,
			/// <summary>Target is a thing and is forbidden.</summary>
			Forbidden     = 0b0001_0000,
		}

		/// <summary>
		/// Stores the expected status of actor and job targets for quick comparison.
		/// </summary>
		/// <remarks>
		/// Used to detect when job targets have changed state, requiring the main thread to
		/// perform a full job failure check.
		/// </remarks>
		public struct TargetStatusValidation : IEquatable<TargetStatusValidation>
		{
			public ExpectedTargetStatus ExpectedActorStatus;
			public ExpectedTargetStatus ExpectedTargetAStatus;
			public ExpectedTargetStatus ExpectedTargetBStatus;
			public ExpectedTargetStatus ExpectedTargetCStatus;

			/// <summary>
			/// Computes the expected status flags for a target.
			/// </summary>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static ExpectedTargetStatus GetExpectedTargetStatus(Thing thing)
			{
				if (thing == null)
					return ExpectedTargetStatus.None;
				return GetExpectedTargetStatusDo(thing);
			}

			private static ExpectedTargetStatus GetExpectedTargetStatusDo(Thing thing)
			{
				var status = ExpectedTargetStatus.None;
				if (!thing.Destroyed)
				{
					status |= ExpectedTargetStatus.Spawned;
					if (thing is ThingWithComps thingWithComps)
					{
						if (thingWithComps.compForbiddable?.Forbidden == true)
						{
							status |= ExpectedTargetStatus.Forbidden;
						}
						if (thingWithComps is Pawn pawn)
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
					}
				}
				return status;
			}

			/// <summary>
			/// Creates a validation snapshot for the actor and all job targets.
			/// </summary>
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

		/// <summary>
		/// Stores the expected Manhattan distances to job targets for quick comparison.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Used to detect when a pawn has moved significantly relative to its targets,
		/// which may indicate that the job state has changed.
		/// </para>
		/// <para>
		/// Uses Manhattan distance instead of Euclidean for faster calculation.
		/// A tolerance of <see cref="TOLERANCE"/> cells allows for minor position changes
		/// without triggering a full check.
		/// </para>
		/// </remarks>
		public struct TargetDistanceValidation
		{
			/// <summary>Allowed deviation in cells before triggering a full check.</summary>
			public const int TOLERANCE = 2;
			/// <summary>Negative tolerance for comparison.</summary>
			public const int NEG_TOLERANCE = -TOLERANCE;
			public ushort ManhattanDistanceToTargetA;
			public ushort ManhattanDistanceToTargetB;
			public ushort ManhattanDistanceToTargetC;

			/// <summary>
			/// Computes the Manhattan distance between two things.
			/// </summary>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static ushort GetManhattanDistance([NotNull] Thing a, [CanBeNull] Thing b)
			{
				if (b == null)
					return 0;

				var delta = a.Position - b.Position;
				return (ushort)(Math.Abs(delta.x) + Math.Abs(delta.z));
			}

			/// <summary>
			/// Creates a distance validation snapshot for the actor and all job targets.
			/// </summary>
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

			/// <summary>
			/// Checks if two distances are within the tolerance.
			/// </summary>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private static bool DistanceAlmostEquals(ushort distance1, ushort distance2)
			{
				var delta = (int)distance1 - (int)distance2;
				return delta >= NEG_TOLERANCE && delta <= TOLERANCE;
			}

			/// <summary>
			/// Checks if all distances are within tolerance of the other validation.
			/// </summary>
			public bool AlmostEquals(TargetDistanceValidation other)
			{
				return DistanceAlmostEquals(ManhattanDistanceToTargetA, other.ManhattanDistanceToTargetA) &&
					   DistanceAlmostEquals(ManhattanDistanceToTargetB, other.ManhattanDistanceToTargetB) &&
					   DistanceAlmostEquals(ManhattanDistanceToTargetC, other.ManhattanDistanceToTargetC);
			}
		}

		/// <summary>
		/// Stores prediction results for a single pawn.
		/// </summary>
		/// <remarks>
		/// Structured with <see cref="StructLayoutAttribute"/> to fill a cache line (64 bytes)
		/// and avoid false sharing between threads when processing multiple pawns.
		/// </remarks>
		[StructLayout(LayoutKind.Auto, Size = 64)] // Fill a cache line to avoid false sharing
		public sealed class JobPrediction
		{
			/// <summary>
			/// The game tick when this prediction was last updated.
			/// </summary>
			/// <remarks>A value of -1 indicates invalid/stale data.</remarks>
			public int UpdateTick = -1;
			/// <summary>If <c>true</c>, the pawn's job may fail this tick and requires main thread verification.</summary>
			public bool MayFail = false;
			/// <summary>If <c>true</c>, the pawn may need to start a constant job this tick.</summary>
			public bool ShouldDoConstantJob = false;
			/// <summary>Cached distance validation for quick comparison.</summary>
			public TargetDistanceValidation DistanceValidation;
			/// <summary>Cached status validation for quick comparison.</summary>
			public TargetStatusValidation StatusValidation;
		}

		/// <summary>
		/// Registers a pawn for job prediction tracking.
		/// </summary>
		public static void AddPawn(Pawn pawn)
		{
			if (!_jobPredictionMap.TryAdd(pawn.thingIDNumber, new JobPrediction()))
			{
				return; //todo: error
			}
		}

		/// <summary>
		/// Unregisters a pawn from job prediction tracking.
		/// </summary>
		public static void RemovePawn(Pawn pawn)
		{
			if (!_jobPredictionMap.Remove(pawn.thingIDNumber, out _))
			{
				return; //todo: error
			}
		}

		/// <summary>
		/// Performs parallel job prediction for a single pawn.
		/// </summary>
		/// <remarks>
		/// Updates the following:
		/// <list type="bullet">
		/// <item><see cref="JobPrediction.MayFail"/> - Whether the job might fail.</item>
		/// <item><see cref="JobPrediction.DistanceValidation"/> - Target distances.</item>
		/// <item><see cref="JobPrediction.StatusValidation"/> - Target statuses.</item>
		/// <item><see cref="JobPrediction.ShouldDoConstantJob"/> - Whether constant job check is needed.</item>
		/// </list>
		/// </remarks>
		public static void ProcessPawn(Pawn pawn, int gameTick, int tickDeltaPlusOne,
			bool predictJobFailure, bool predictConstantJob)
		{
			JobPrediction prediction = null;
			try
			{
				if (!_jobPredictionMap.TryGetValue(pawn.thingIDNumber, out prediction))
				{
					return;
				}

				if (predictJobFailure)
				{
					prediction.MayFail = PredictFail(pawn);
					if (!prediction.MayFail)
					{
						var job = pawn.CurJob;
						prediction.DistanceValidation = TargetDistanceValidation.CreateValidation(pawn, job);
						prediction.StatusValidation = TargetStatusValidation.CreateValidation(pawn, job);
					}
				}
				else
				{
					prediction.MayFail = true;
				}

				if (predictConstantJob)
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

		/// <summary>
		/// Predicts whether a pawn's current job will fail this tick.
		/// </summary>
		/// <returns><c>true</c> if the job may fail; <c>false</c> if the job is stable.</returns>
		/// <remarks>
		/// <para>
		/// Checks:
		/// <list type="bullet">
		/// <item>Global job fail conditions (e.g., pawn died, target destroyed).</item>
		/// <item>Current toil end conditions.</item>
		/// <item>Job-specific ignore list for jobs that shouldn't be predicted.</item>
		/// </list>
		/// </para>
		/// <para>
		/// Jobs with target queues are not predicted because their validations are too complex.
		/// </para>
		/// </remarks>
		/// <seealso cref="Verse.AI.JobDriver.CheckCurrentToilEndOrFail"/>
		public static bool PredictFail(Pawn pawn)
		{
			var jobDriver = pawn.jobs?.curDriver;
			if (jobDriver == null)
				return true;
			try
			{
				var job = pawn.CurJob;
				if (job != null)
				{
					if (CompatibilityDefines.IsJobFailurePredictingIgnored(job.def))
						return true;

					// We can't validate target queue. So we will stop predicting if a job uses target queue.
					// Also, can't validate placedThings.
					if (job.targetQueueA?.Count > 0 || job.targetQueueB?.Count > 0 ||
						job.placedThings?.Count > 0)
					{
						return true;
					}
				}

				var globalFailConditions = jobDriver.globalFailConditions;
				if (globalFailConditions != null)
				{
					for (int index = 0, count = globalFailConditions.Count; index < count; index++)
					{
						var action = globalFailConditions[index];
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
				var endConditions = toil?.endConditions;
				if (endConditions != null)
				{
					for (int index = 0, count = endConditions.Count; index < count; index++)
					{
						var action = endConditions[index];
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

		/// <summary>
		/// Predicts whether a pawn needs to check for constant jobs (e.g., reacting to threats).
		/// </summary>
		/// <returns><c>true</c> if constant job check is needed; otherwise, <c>false</c>.</returns>
		/// <remarks>
		/// Runs the pawn's constant think tree to see if a higher-priority job should interrupt.
		/// </remarks>
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

		/// <summary>
		/// Clears all prediction data.
		/// </summary>
		public static void CleanCache()
		{
			_jobPredictionMap.Clear();
		}

		/// <summary>
		/// Checks if the main thread should perform a full job failure check for a pawn.
		/// </summary>
		/// <returns>
		/// <c>false</c> if the parallel prediction guarantees the job won't fail this tick;
		/// <c>true</c> if a full check is needed.
		/// </returns>
		/// <remarks>
		/// <para>
		/// Skips the full check when:
		/// <list type="bullet">
		/// <item>Prediction says the job won't fail (<see cref="JobPrediction.MayFail"/> is <c>false</c>).</item>
		/// <item>Target distances haven't changed significantly.</item>
		/// <item>Target statuses haven't changed.</item>
		/// </list>
		/// </para>
		/// </remarks>
		/// <seealso cref="Patches.Verse_AI_JobDriver_DriverTick"/>
		public static bool ShouldCheckJobFail(Pawn pawn)
		{
			if (_jobPredictionMap.TryGetValue(pawn.thingIDNumber, out var prediction))
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

		/// <summary>
		/// Checks if the main thread should check for constant jobs for a pawn.
		/// </summary>
		/// <seealso cref="Patches.Verse_AI_Pawn_JobTracker_JobTrackerTickInterval"/>
		public static bool ShouldCheckConstantJob(Pawn pawn)
		{
			if (_jobPredictionMap.TryGetValue(pawn.thingIDNumber, out var prediction))
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